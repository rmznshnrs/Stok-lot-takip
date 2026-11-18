"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { ozet, urunler, type TakvimGunu } from "@/lib/api";
import { bugun } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

const GUNLER = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
const AYLAR = ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

/** "YYYY-MM-DD" (yerel tarih, saat dilimi kaymadan). */
function metin(t: Date) {
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
 * Takvim: gün gün, ürün ürün planlanan üretim (sıradaki işler), üretilen ve teslimi istenen sipariş adedi.
 * Teslim tarihi verilmemiş siparişler sipariş gününde görünür. ?ay=YYYY-MM
 */
export default function TakvimSayfasi() {
  const params = useSearchParams();
  const bugunMetni = bugun();
  const [yil, ay] = (params.get("ay") ?? bugunMetni.slice(0, 7)).split("-").map(Number);
  const gecerli = yil > 2000 && ay >= 1 && ay <= 12;
  const y = gecerli ? yil : Number(bugunMetni.slice(0, 4));
  const a = gecerli ? ay - 1 : Number(bugunMetni.slice(5, 7)) - 1;
  const liste = gunler(y, a);
  const bas = metin(liste[0]);
  const bit = metin(liste.at(-1)!);

  const { veri, hata } = useVeri(() => ozet.takvim(bas, bit), [bas, bit]);
  const { veri: urunListesi } = useVeri(() => urunler.listele(), []);
  const [urunId, setUrunId] = useState("");
  const gunVerisi = new Map<string, TakvimGunu["urunler"]>(
    (veri ?? []).map((g) => [g.tarih, g.urunler.filter((u) => !urunId || String(u.urunId) === urunId)]),
  );
  const ayAdresi = (fark: number) => {
    const d = new Date(y, a + fark, 1);
    return `/takvim?ay=${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
  };

  // Ay toplamları (yalnız bu ayın günleri)
  const ayOnek = `${y}-${String(a + 1).padStart(2, "0")}`;
  const toplam = new Map<string, { plan: number; uret: number; ist: number }>();
  for (const [t, us] of gunVerisi) {
    if (!t.startsWith(ayOnek)) continue;
    for (const u of us) {
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
              <Link href="/takvim" className="btn-baglanti">Bugün</Link>
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
              <span className="takvim-cip takvim-cip--plan">Planlanan üretim</span>
              <span className="takvim-cip takvim-cip--uret">Üretilen</span>
              <span className="takvim-cip takvim-cip--ist">Teslimi istenen sipariş</span>
            </div>
            {hata && <div className="mesaj mesaj--error">{hata}</div>}
            <div className="takvim-kap">
              <div className="takvim" role="grid" aria-label={`${AYLAR[a]} ${y}`}>
                {GUNLER.map((g) => <div key={g} className="takvim-gun-adi" role="columnheader">{g}</div>)}
                {liste.map((d) => {
                  const t = metin(d);
                  const us = gunVerisi.get(t) ?? [];
                  const disarida = d.getMonth() !== a;
                  return (
                    <div key={t} role="gridcell"
                      className={`takvim-gun${disarida ? " takvim-gun--disarida" : ""}${t === bugunMetni ? " takvim-gun--bugun" : ""}${t < bugunMetni ? " takvim-gun--gecmis" : ""}`}>
                      <div className="takvim-gun-no">{d.getDate()}</div>
                      {us.map((u) => (
                        <div key={u.urunId} className={`takvim-urun${u.istenen > u.planlanan + u.uretilen ? " takvim-urun--eksik" : ""}`}>
                          <span className="takvim-urun-ad">{u.urunAd}</span>
                          <span className="takvim-sayilar">
                            {u.planlanan > 0 && <span className="takvim-cip takvim-cip--plan" title="Planlanan üretim">{u.planlanan}</span>}
                            {u.uretilen > 0 && <span className="takvim-cip takvim-cip--uret" title="Üretilen">{u.uretilen}</span>}
                            {u.istenen > 0 && <span className="takvim-cip takvim-cip--ist" title="Teslimi istenen sipariş">{u.istenen}</span>}
                          </span>
                        </div>
                      ))}
                    </div>
                  );
                })}
              </div>
            </div>
            {veri === null && !hata && <p className="yukleniyor">Yükleniyor…</p>}
          </div>
        </section>

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
