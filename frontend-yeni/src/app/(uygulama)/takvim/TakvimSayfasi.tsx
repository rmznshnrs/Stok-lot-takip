"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { ozet, sira, type TakvimGunu } from "@/lib/api";
import { bugun, tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import GunPaneli from "./GunPaneli";

const GUNLER = ["Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz"];
const AYLAR = ["Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık"];

export function gunMetni(t: Date) {
  return `${t.getFullYear()}-${String(t.getMonth() + 1).padStart(2, "0")}-${String(t.getDate()).padStart(2, "0")}`;
}

function gunler(yil: number, ay: number) {
  const ilk = new Date(yil, ay, 1);
  const bas = new Date(yil, ay, 1 - ((ilk.getDay() + 6) % 7));
  const son = new Date(yil, ay + 1, 0);
  const bit = new Date(yil, ay, son.getDate() + 6 - ((son.getDay() + 6) % 7));
  const liste: Date[] = [];
  for (let d = new Date(bas); d <= bit; d.setDate(d.getDate() + 1)) liste.push(new Date(d));
  return liste;
}

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
  const [arama, setArama] = useState("");
  const aranan = arama.trim().toLocaleLowerCase("tr");
  const uyar = (metin: string) => !aranan || metin.toLocaleLowerCase("tr").includes(aranan);
  const [secili, setSecili] = useState<string | null>(params.get("gun"));
  const gunVerisi = new Map<string, TakvimGunu>((veri ?? []).map((g) => [g.tarih, {
    ...g,
    urunler: g.urunler.filter((u) => uyar(u.urunAd)),
    planlar: g.planlar.filter((p) => uyar(`${p.parca?.kod ?? ""} ${p.parca?.ad ?? ""} ${p.tedarikciAd ?? ""}`)),
  }]));
  const ayAdresi = (fark: number) => {
    const d = new Date(y, a + fark, 1);
    return `/takvim?ay=${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}`;
  };

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
            <div className="arama-satiri">
              <input type="search" className="girdi girdi--ara" aria-label="Takvimde ara" placeholder="Ürün ya da parça ara"
                value={arama} onChange={(e) => setArama(e.target.value)} />
            </div>
          </div>
          <div className="panel-ic">
            <div className="takvim-acikla">
              <span className="takvim-cubuk takvim-cubuk--plan">Planlanan üretim</span>
              <span className="takvim-cubuk takvim-cubuk--uret">Üretilen</span>
              <span className="takvim-cubuk takvim-cubuk--ist">Teslimi istenen sipariş</span>
              <span className="takvim-cubuk takvim-cubuk--alim">Parça alımı</span>
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

      </main>
    </>
  );
}
