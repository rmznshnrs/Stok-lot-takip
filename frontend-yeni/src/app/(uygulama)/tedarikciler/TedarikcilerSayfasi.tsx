"use client";

import Link from "next/link";
import FirmaListesi from "@/components/FirmaListesi";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { useSayfalama } from "@/components/Sayfalama";
import { stok } from "@/lib/api";
import { lotAdresi } from "@/lib/adres";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

export default function TedarikcilerSayfasi() {
  return (
    <>
      <SayfaSekmeleri etiket="Tedarikçiler" sekmeler={[]} />
      <FirmaListesi tur="tedarikci" kok="/tedarikciler" ek={(t) => <Alinanlar key={t.id} tedarikciAd={t.ad} />} />
    </>
  );
}

function Alinanlar({ tedarikciAd }: { tedarikciAd: string }) {
  const { veri: gruplar, hata } = useVeri(() => stok.durum({ bitenler: true }), []);
  const tum = gruplar
    ? gruplar.flatMap((g) => g.lotlar).filter((l) => l.tedarikciAd === tedarikciAd)
        .sort((a, b) => b.siparisTarihi.localeCompare(a.siparisTarihi) || b.id - a.id)
    : null;
  const { gorunen, cubuk } = useSayfalama(tum, tedarikciAd);

  return (
    <div className="form-dikey">
      <div className="bas-satir">
        <h3>Alınanlar {tum && <span className="rozet rozet--kucuk rozet--mavi">{tum.length}</span>}</h3>
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Sipariş Tarihi</th>
              <th>Parça</th>
              <th>Lot No</th>
              <th>Sipariş No</th>
              <th className="sag">Gelen</th>
              <th className="sag">Elde Kalan</th>
            </tr>
          </thead>
          <tbody>
            {tum === null && !hata && <tr className="bos"><td colSpan={6}>Yükleniyor…</td></tr>}
            {tum?.length === 0 && <tr className="bos"><td colSpan={6}>Bu tedarikçiden alım yok.</td></tr>}
            {gorunen?.map((l) => (
              <tr key={l.id} className="satir-tikla">
                <td>{tarih(l.siparisTarihi)}</td>
                <td>{l.parcaAd} <span className="soluk mono">{l.parcaKod}</span></td>
                <td className="mono"><Link href={lotAdresi(l.lotNo)} className="satir-bag">{l.lotNo}</Link></td>
                <td className="mono">{l.siparisNo}</td>
                <td className="sag">{l.girisAdet}</td>
                <td className={`sag${l.kalanAdet ? "" : " soluk"}`}>{l.kalanAdet}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {cubuk}
    </div>
  );
}
