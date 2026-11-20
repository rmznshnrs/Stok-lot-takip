"use client";

import Link from "next/link";
import { SIPARIS_DURUM_ADI, type GrafikVerisi, type SiparisDurumu } from "@/lib/api";
import { tarih } from "@/lib/bicim";

const DURUM_RENGI: Record<SiparisDurumu, string> = {
  bekliyor: "#94A3B8", uretimde: "#34709F", hazir: "#0D9488", kismen_gonderildi: "#CA8A04", tamamlandi: "#16A34A", iptal: "#DC2626",
};
const DURUM_SIRASI: SiparisDurumu[] = ["bekliyor", "uretimde", "hazir", "kismen_gonderildi", "tamamlandi", "iptal"];

function gunFarki(t: string) {
  const [y, a, g] = t.split("-").map(Number);
  const s = new Date();
  return Math.round((Date.UTC(s.getFullYear(), s.getMonth(), s.getDate()) - Date.UTC(y, a - 1, g)) / 86_400_000);
}

export function GecikenIsler({ veri }: { veri: GrafikVerisi | null }) {
  const liste = veri?.gecikenler ?? [];
  return (
    <section className={`panel${liste.length ? " panel--gecikme" : ""}`}>
      <div className="panel-bas">
        <h3>Geciken İşler</h3>
        {veri && (liste.length
          ? <span className="rozet rozet--hata">{liste.length} iş gecikti</span>
          : <span className="rozet rozet--basari">Geciken iş yok</span>)}
      </div>
      {liste.length > 0 && (
        <div className="tablo-kap tablo-kap--bes">
          <table className="tablo">
            <thead>
              <tr>
                <th>Tür</th>
                <th>Müşteri / İş</th>
                <th>Ürün</th>
                <th className="sag">Kalan Adet</th>
                <th>İstenen / Planlanan</th>
              </tr>
            </thead>
            <tbody>
              {liste.map((g) => (
                <tr key={`${g.tur}-${g.id}`} className="satir-tikla">
                  <td>{g.tur === "siparis" ? "Teslim" : "Üretim"}</td>
                  <td>
                    <Link href={g.tur === "siparis" ? `/siparisler?id=${g.id}` : `/uretim?eklendi=${g.id}`} className="satir-bag">{g.no}</Link>
                  </td>
                  <td>{g.ad}</td>
                  <td className="sag"><b>{g.kalan}</b></td>
                  <td>{tarih(g.tarih)} <span className="kalan-gun kalan-gun--gecikti">{gunFarki(g.tarih)} gün gecikti</span></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}

export function HaftalikUretim({ veri }: { veri: GrafikVerisi | null }) {
  const haftalar = veri?.haftalikUretim ?? [];
  const enCok = Math.max(1, ...haftalar.map((h) => h.adet));
  const toplam = haftalar.reduce((t, h) => t + h.adet, 0);
  return (
    <section className="panel">
      <div className="panel-bas">
        <h3>Haftalık Üretim</h3>
        {veri && <span className="ozet">Son {haftalar.length} hafta · {toplam} adet</span>}
      </div>
      <div className="panel-ic">
        <div className="hafta-grafigi" role="img" aria-label={haftalar.map((h) => `${tarih(h.haftaBasi)} haftası ${h.adet} adet`).join(", ")}>
          {haftalar.map((h, i) => {
            const son = i === haftalar.length - 1;
            return (
              <div key={h.haftaBasi} className="hafta-sutun">
                <span className="hafta-sayi">{h.adet}</span>
                <span className="hafta-yol">
                  <span className={`hafta-dolu${son ? " hafta-dolu--bu" : ""}`} style={{ height: `${Math.max(2, (h.adet / enCok) * 100)}%` }} />
                </span>
                <span className="hafta-ad">{son ? "Bu hafta" : tarih(h.haftaBasi).slice(0, 5)}</span>
              </div>
            );
          })}
        </div>
      </div>
    </section>
  );
}

export function SiparisDurumuGrafigi({ veri }: { veri: GrafikVerisi | null }) {
  const sayilar = DURUM_SIRASI.map((d) => ({ d, sayi: veri?.siparisDurumlari.find((x) => x.durum === d)?.sayi ?? 0 }));
  const toplam = sayilar.reduce((t, x) => t + x.sayi, 0);
  let kayma = 0;
  return (
    <section className="panel">
      <div className="panel-bas">
        <h3>Talep Durumu</h3>
        <Link href="/siparisler" className="btn btn--ikincil btn--kucuk">Talepler</Link>
      </div>
      <div className="panel-ic halka-duzeni">
        <svg viewBox="0 0 42 42" className="halka" role="img"
          aria-label={sayilar.map((x) => `${SIPARIS_DURUM_ADI[x.d]} ${x.sayi}`).join(", ")}>
          <circle cx="21" cy="21" r="15.9155" fill="none" stroke="var(--kenar-ince)" strokeWidth="6" />
          {toplam > 0 && sayilar.filter((x) => x.sayi).map((x) => {
            const pay = (x.sayi / toplam) * 100;
            const parca = (
              <circle key={x.d} cx="21" cy="21" r="15.9155" fill="none" stroke={DURUM_RENGI[x.d]} strokeWidth="6"
                strokeDasharray={`${pay} ${100 - pay}`} strokeDashoffset={25 - kayma} />
            );
            kayma += pay;
            return parca;
          })}
          <text x="21" y="20.5" textAnchor="middle" className="halka-sayi">{toplam}</text>
          <text x="21" y="26" textAnchor="middle" className="halka-alt">talep</text>
        </svg>
        <ul className="lejant">
          {sayilar.map((x) => (
            <li key={x.d} className={x.sayi ? undefined : "soluk"}>
              <span className="lejant-renk" style={{ background: DURUM_RENGI[x.d] }} />
              {SIPARIS_DURUM_ADI[x.d]}
              <b>{x.sayi}</b>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
