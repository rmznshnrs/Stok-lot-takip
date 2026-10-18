import type { Metadata } from "next";
import { Suspense } from "react";
import ParcalarSayfasi from "./ParcalarSayfasi";

export const metadata: Metadata = { title: "Parçalar" };

export default function Sayfa() {
  return (
    <Suspense>
      <ParcalarSayfasi />
    </Suspense>
  );
}
