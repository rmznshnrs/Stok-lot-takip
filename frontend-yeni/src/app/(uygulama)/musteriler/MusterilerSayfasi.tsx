"use client";

import { useSearchParams } from "next/navigation";
import FirmaListesi from "@/components/FirmaListesi";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import SeriSayfasi from "@/components/SeriSayfasi";
import AlinanUrunler from "./AlinanUrunler";
import MusteriSiparisleri from "./MusteriSiparisleri";
import SiparisDetay from "./SiparisDetay";
import SiparisFormu from "./SiparisFormu";

export default function MusterilerSayfasi() {
  const params = useSearchParams();
  const seri = params.get("seri")?.trim();
  const siparis = params.get("siparis");
  const siparisMusteri = Number(params.get("musteri")) || 0;
  return (
    <>
      <SayfaSekmeleri etiket="Müşteriler" sekmeler={[]} />
      {siparis === "yeni" && siparisMusteri ? (
        <SiparisFormu musteriId={siparisMusteri} />
      ) : siparis && Number(siparis) ? (
        <SiparisDetay key={siparis} id={Number(siparis)} />
      ) : seri ? (
        <SeriSayfasi key={seri} seriNo={seri} />
      ) : (
        <FirmaListesi tur="musteri" kok="/musteriler" ek={(m) => (
          <>
            <MusteriSiparisleri key={`s-${m.id}`} musteriId={m.id} />
            <AlinanUrunler key={m.id} musteriId={m.id} />
          </>
        )} />
      )}
    </>
  );
}
