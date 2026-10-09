import type { Metadata } from "next";
import { Suspense } from "react";
import UretimEkrani from "./UretimEkrani";

export const metadata: Metadata = { title: "Üretim" };

export default function Sayfa() {
  return (
    <Suspense>
      <UretimEkrani />
    </Suspense>
  );
}
