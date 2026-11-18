"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { ozet, sira, urunler, type TakvimGunu } from "@/lib/api";
import { bugun, tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import GunPaneli from "./GunPaneli";

const GUNLER = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
const AYLAR = ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

/** "YYYY-MM-DD" (yerel tarih, saat dilimi kaymadan). */
export function gunMetni(t: Date) {
  return `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, "0")}-${String(t.getDate()).padStart(2, "0")}`;
}

/** Ayın görünen günleri: ayın ilk gününün haftasının pazartesisinden son gününün haftasının pazarına. */
function gunler(yil: number, ay: number) {
  const ilk = new Date(yil, ay, 1);
  const bas = new Date(yil, ay, 1 - ((ilk.getDay() + 6) % 7));
  const son = new Date(yil, ay + 1, 0);
  const bit = new Date(yil, ay, son.getDate() + 6 - ((son.getDay() + 6) % 7));
  const liste: Date[] = [];
  for (let d = new Date(bas); d <= bit; d.setDate(d.getDate() + 1)) liste.push(new Date(d));
  return liste;
}

/**
 * Takvim: gün gün, ürün ürün planlanan üretim (sıradaki işler), üretilen ve teslimi istenen sipariş adedi;
 * parça alımı hatırlatıcıları ve notlar. Bir güne basınca o günün ayrıntısı ve plan girme alanı açılır.
 * Teslim tarihi verilmemiş siparişler sipariş gününde görünür. ?ay=YYYY-MM&gun=YYYY-MM-DD
 */
export default function TakvimSayfasi() {
  const params = useSearchParams();
  const bugunMetni = bugun();
  const [yil, ay] = (params.get("ay") ?? bugunMetni.slice(0, 7)).split("-").map(Number);
  const gecerli = yil > 2000 && ay >= 1 && ay <= 12;
  const y = gecerli ? yil : Number(bugunMetni.slice(0, 4));
  const a = gecerli ? ay - 1 : Number(bugunMetni.slice(5, 7)) - 1;
  const liste = gunler(y, a);
  const bas = gunMetni(liste[0]);
  const bit = gunMetni(liste.at(-1)!);

  const { veri, hata, yenile } = useVeri(() => ozet.takvim(bas, bit), [bas, bit]);
  const { veri: bekleyen, yenile: siraYenile } = useVeri(() => sira.liste(), []);
  const { veri: urunListesi } = useVeri(() => urunler.listele(), []);
  const [urunId, setUrunId] = useState("");
  const [secili, setSecili] = useState<string | null>(params.get("gun"));
  const gunVerisi = new Map<string, TakvimGunu>((veri ?? []).map((g) => [g.tarih, {
    ...g, urunler: g.urunler.filter((u) => !urunId || String(u.urunId) === urunId),
  }]));
  const ayAdresi = (fark: number) => {
    const d = new Date(y, a + fark, 1);
    return `/takvim?ay=${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
  };

  // Ay toplamları (yalnız bu ayın günleri)
  const ayOnek = `${y}-${String(a + 1).padStart(2, "0")}`;
  const toplam = new Map<string, { plan: number; uret: number; ist: number }>();
  for (const [t, g] of gunVerisi) {
    if (!t.startsWith(ayOnek)) continue;
    for (const u of g.urunler) {
      const x = toplam.get(u.urunAd) ?? { plan: 0, uret: 0, ist: 0 };
      toplam.set(u.urunAd, { plan: x.plan + u.planlanan, uret: x.uret + u.uretilen, ist: x.ist + u.istenen });
    }
  }

  return (
    <>
      <SayfaSekmeleri etiket="Takvim" sekmeler={[]} />
      <main className="icerik icerik--dikey">
        <section className="panel">
          <div className="panel-bas">
            <div className="dugme-grup">
              <Link href={ayAdresi(-1)} className="btn btn--ikincil btn--kucuk" aria-label="Önceki ay">←</Link>
              <h2 className="takvim-baslik">{AYLAR[a]} {y}</h2>
              <Link href={ayAdresi(1)} className="btn btn--ikincil btn--kucuk" aria-label="Sonraki ay">→</Link>
            </div>
            <div className="dugme-grup">
              <select className="girdi girdi--kucuk" aria-label="Ürün" value={urunId} onChange={(e) => setUrunId(e.target.value)}>
                <option value="">Bütün Ürünler</option>
                {urunListesi?.map((u) => <option key={u.id} value={u.id}>{u.ad}</option>)}
              </select>
            </div>
          </div>
          <div className="panel-ic">
            <div className="takvim-acikla">
              <span className="takvim-cubuk takvim-cubuk--plan">Planlanan üretim</span>
              <span className="takvim-cubuk takvim-cubuk--uret">Üretilen</span>
              <span className="takvim-cubuk takvim-cubuk--ist">Teslimi istenen sipariş</span>
              <span className="takvim-cubuk takvim-cubuk--alim">Parça alımı / not</span>
              <span className="not">Plan girmek ya da ayrıntıyı görmek için bir güne bas.</span>
            </div>
            {hata && <div className="mesaj mesaj--error">{hata}</div>}
            <div className="takvim-kap">
              <div className="takvim" role="grid" aria-label={`${AYLAR[a]} ${y}`}>
                {GUNLER.map((g) => <div key={g} className="takvim-gun-adi" role="columnheader">{g}</div>)}
                {liste.map((d) => {
                  const t = gunMetni(d);
                  const g = gunVerisi.get(t);
                  const sinif = [
                    "takvim-gun",
                    d.getMonth() !== a && "takvim-gun--disarida",
                    t === bugunMetni && "takvim-gun--bugun",
                    t < bugunMetni && "takvim-gun--gecmis",
                    t === secili && "takvim-gun--secili",
                  ].filter(Boolean).join(" ");
                  return (
                    <button key={t} type="button" role="gridcell" className={sinif} aria-selected={t === secili}
                      aria-label={`${tarih(t)} ayrıntısı`} onClick={() => setSecili(t === secili ? null : t)}>
                      <span className="takvim-gun-no">{d.getDate()}</span>
                      {g?.urunler.map((u) => (
                        <span key={u.urunId} className="takvim-urun">
                          {u.planlanan > 0 && <span className="takvim-cubuk takvim-cubuk--plan">{u.urunAd} · {u.planlanan} plan</span>}
                          {u.uretilen > 0 && <span className="takvim-cubuk takvim-cubuk--uret">{u.urunAd} · {u.uretilen} üretildi</span>}
                          {u.istenen > 0 && (
                            <span className={`takvim-cubuk takvim-cubuk--ist${u.istenen > u.planlanan + u.uretilen ? " takvim-cubuk--eksik" : ""}`}>
                              {u.urunAd} · {u.istenen} teslim
                            </span>
                          )}
                        </span>
                      ))}
                      {g?.planlar.map((p) => (
                        <span key={p.id} className={`takvim-cubuk takvim-cubuk--alim${p.tamamlandi ? " takvim-cubuk--tamam" : ""}`}>
                          {p.parca ? `${p.parca.kod}${p.adet ? ` × ${p.adet}` : ""} alım` : p.aciklama}
                        </span>
                      ))}
                    </button>
                  );
                })}
              </div>
            </div>
            {veri === null && !hata && <p className="yukleniyor">Yükleniyor…</p>}
          </div>
        </section>

        {secili && (
          <GunPaneli
            key={secili}
            gun={secili}
            veri={gunVerisi.get(secili)}
            isler={bekleyen?.filter((e) => e.planlananTarih === secili) ?? []}
            kapat={() => setSecili(null)}
            degisti={() => { yenile(); siraYenile(); }}
          />
        )}

        {toplam.size > 0 && (
          <section className="panel">
            <div className="panel-bas"><h3>{AYLAR[a]} Toplamı</h3></div>
            <div className="tablo-kap">
              <table className="tablo">
                <thead>
                  <tr>
                    <th>Ürün</th>
                    <th className="sag">Planlanan Üretim</th>
                    <th className="sag">Üretilen</th>
                    <th className="sag">Teslimi İstenen</th>
                  </tr>
                </thead>
                <tbody>
                  {[...toplam].sort(([x], [z]) => x.localeCompare(z, "tr")).map(([ad, t]) => (
                    <tr key={ad} className={t.ist > t.plan + t.uret ? "uyari-satir" : undefined}>
                      <td>{ad}</td>
                      <td className="sag">{t.plan}</td>
                      <td className="sag">{t.uret}</td>
                      <td className="sag">{t.ist}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}
      </main>
    </>
  );
}
