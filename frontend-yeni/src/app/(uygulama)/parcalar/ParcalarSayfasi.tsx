"use client";

import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import ParcalarEkrani from "./ParcalarEkrani";

/** Parçalar. Tedarikçiler ayrı sayfada (/tedarikciler). */
export default function ParcalarSayfasi() {
  return (
    <>
      <SayfaSekmeleri etiket="Parçalar" sekmeler={[]} />
      <ParcalarEkrani />
    </>
  );
}
