"use client";

import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import ParcalarEkrani from "./ParcalarEkrani";

export default function ParcalarSayfasi() {
  return (
    <>
      <SayfaSekmeleri etiket="Parçalar" sekmeler={[]} />
      <ParcalarEkrani />
    </>
  );
}
