"use client";

import { useSearchParams } from "next/navigation";
import FirmaListesi from "@/components/FirmaListesi";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import SatisEkrani from "./SatisEkrani";

/** Satış ve müşteriler (?sekme=musteriler). */
export default function SatisSayfasi() {
  const firma = useSearchParams().get("sekme") === "musteriler";
  return (
    <>
      <SayfaSekmeleri
        etiket="Satış"
        sekmeler={[
          { adres: "/satis", ad: "Satış", aktif: !firma },
          { adres: "/satis?sekme=musteriler", ad: "Müşteriler", aktif: firma },
        ]}
      />
      {firma ? <FirmaListesi tur="musteri" kok="/satis?sekme=musteriler" /> : <SatisEkrani />}
    </>
  );
}
