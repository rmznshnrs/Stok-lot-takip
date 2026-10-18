import type { Metadata } from "next";
import { Suspense } from "react";
import YonetimEkrani from "./YonetimEkrani";

export const metadata: Metadata = { title: "Yönetim" };

export default function Sayfa() {
  return (
    <Suspense>
      <YonetimEkrani />
    </Suspense>
  );
}
