"use client";

import Link from "next/link";
import { useState } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import MalGirisiFormu from "../stok/MalGirisiFormu";
import FirmaListesi from "@/components/FirmaListesi";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { useSayfalama } from "@/components/Sayfalama";
import { parcalar, stok } from "@/lib/api";
import { lotAdresi } from "@/lib/adres";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import { useSiralama } from "@/lib/siralama";

export default function TedarikcilerSayfasi() {
  return (
    <>
      <SayfaSekmeleri etiket="Tedarikçiler" sekmeler={[]} />
      <FirmaListesi tur="tedarikci" kok="/tedarikciler" ek={(t) => <Alinanlar key={t.id} tedarikciAd={t.ad} />} />
    </>
  );
}

function Alinanlar({ tedarikciAd }: { tedarikciAd: string }) {
  const { veri: gruplar, hata, yenile } = useVeri(() => stok.durum({ bitenler: true }), []);
  const { veri: parcaListesi } = useVeri(() => parcalar.listele(), []);
  const [formAcik, setFormAcik] = useState(false);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const tum = gruplar
    ? gruplar.flatMap((g) => g.lotlar).filter((l) => l.tedarikciAd === tedarikciAd)
        .sort((a, b) => b.siparisTarihi.localeCompare(a.siparisTarihi) || b.id - a.id)
    : null;
  const { sirali, baslik, siralamaAnahtari } = useSiralama(tum, {
    tarih: (l) => l.siparisTarihi, parca: (l) => l.parcaAd, lot: (l) => l.lotNo, siparisNo: (l) => l.siparisNo,
    gelen: (l) => l.girisAdet, kalan: (l) => l.kalanAdet,
  });
  const { gorunen, cubuk } = useSayfalama(sirali, `${tedarikciAd}|${siralamaAnahtari}`);

  return (
    <div className="form-dikey">
      <div className="bas-satir">
        <h3>Alınanlar {tum && <span className="rozet rozet--kucuk rozet--mavi">{tum.length}</span>}</h3>
        <button type="button" className="btn btn--kucuk" aria-expanded={formAcik} onClick={() => setFormAcik(!formAcik)}>
          {formAcik ? "Formu Kapat" : "+ Parça Alımı"}
        </button>
      </div>
      {formAcik && (
        <MalGirisiFormu parcalar={parcaListesi ?? []} tedarikci={tedarikciAd} baslik={`${tedarikciAd} · Parça Alımı`}
          kaydedildi={(m) => { setMesaj({ tur: "success", metin: m }); yenile(); }} />
      )}
      {!formAcik && <Mesaj mesaj={mesaj} />}
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              {baslik("tarih", "Sipariş Tarihi")}
              {baslik("parca", "Parça")}
              {baslik("lot", "Lot No")}
              {baslik("siparisNo", "Sipariş No")}
              {baslik("gelen", "Gelen", "sag")}
              {baslik("kalan", "Elde Kalan", "sag")}
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
