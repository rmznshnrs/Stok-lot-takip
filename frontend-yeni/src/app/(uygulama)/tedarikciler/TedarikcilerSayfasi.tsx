"use client";

import Link from "next/link";
import { useState } from "react";
import FirmaListesi from "@/components/FirmaListesi";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { useSayfalama } from "@/components/Sayfalama";
import { stok } from "@/lib/api";
import { lotAdresi } from "@/lib/adres";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

/** Tedarikçiler ve onlardan alınan partiler (stok girişleri), her birinin elde kalan adediyle. */
export default function TedarikcilerSayfasi() {
  const [secili, setSecili] = useState("");
  const [eldekiler, setEldekiler] = useState(false);
  const { veri: gruplar, hata } = useVeri(() => stok.durum({ bitenler: true }), []);
  const tum = (gruplar ?? []).flatMap((g) => g.lotlar)
    .sort((a, b) => b.siparisTarihi.localeCompare(a.siparisTarihi) || b.id - a.id);
  const tedarikciler = [...new Set(tum.map((l) => l.tedarikciAd))].sort((a, b) => a.localeCompare(b, "tr"));
  const liste = gruplar ? tum.filter((l) => (!secili || l.tedarikciAd === secili) && (!eldekiler || l.kalanAdet > 0)) : null;
  const { gorunen, cubuk } = useSayfalama(liste, `${secili}|${eldekiler}`);

  return (
    <>
      <SayfaSekmeleri etiket="Tedarikçiler" sekmeler={[]} />
      <FirmaListesi tur="tedarikci" kok="/tedarikciler" />
      <div className="icerik icerik--dikey">
      <section className="panel">
        <div className="panel-bas">
          <h2>Alınanlar</h2>
          <div className="arama-satiri">
            <select className="girdi" aria-label="Tedarikçi" value={secili} onChange={(e) => setSecili(e.target.value)}>
              <option value="">Bütün Tedarikçiler</option>
              {tedarikciler.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
            <label className="onay-etiket">
              <input type="checkbox" checked={eldekiler} onChange={(e) => setEldekiler(e.target.checked)} /> Yalnız Elde Kalanlar
            </label>
          </div>
        </div>
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Sipariş Tarihi</th>
                <th>Tedarikçi</th>
                <th>Parça</th>
                <th>Stok Giriş No</th>
                <th>Sipariş No</th>
                <th className="sag">Gelen</th>
                <th className="sag">Elde Kalan</th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !hata && <tr className="bos"><td colSpan={7}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && <tr className="bos"><td colSpan={7}>Kayıt yok.</td></tr>}
              {gorunen?.map((l) => (
                <tr key={l.id}>
                  <td>{tarih(l.siparisTarihi)}</td>
                  <td>{l.tedarikciAd}</td>
                  <td>{l.parcaAd} <span className="soluk mono">{l.parcaKod}</span></td>
                  <td className="mono"><Link href={lotAdresi(l.lotNo)}>{l.lotNo}</Link></td>
                  <td className="mono">{l.siparisNo}</td>
                  <td className="sag">{l.girisAdet}</td>
                  <td className={`sag${l.kalanAdet ? "" : " soluk"}`}>{l.kalanAdet}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {cubuk}
      </section>
      </div>
    </>
  );
}
