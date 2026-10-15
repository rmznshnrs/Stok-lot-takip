import type { Metadata } from "next";
import { Suspense } from "react";
import EtiketEkrani from "./EtiketEkrani";

export const metadata: Metadata = { title: "Etiket yazdır" };

export default function Sayfa() {
  return (
    <Suspense>
      <EtiketEkrani />
    </Suspense>
  );
}
