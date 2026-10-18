"use client";

import { useSearchParams } from "next/navigation";
import FirmaListesi from "@/components/FirmaListesi";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import ParcalarEkrani from "./ParcalarEkrani";

/** Parçalar ve tedarikçiler (?sekme=tedarikciler). */
export default function ParcalarSayfasi() {
  const firma = useSearchParams().get("sekme") === "tedarikciler";
  return (
    <>
      <SayfaSekmeleri
        etiket="Parçalar"
        sekmeler={[
          { adres: "/parcalar", ad: "Parçalar", aktif: !firma },
          { adres: "/parcalar?sekme=tedarikciler", ad: "Tedarikçiler", aktif: firma },
        ]}
      />
      {firma ? <FirmaListesi tur="tedarikci" kok="/parcalar?sekme=tedarikciler" /> : <ParcalarEkrani />}
    </>
  );
}
