import type { Metadata } from "next";
import { Suspense } from "react";
import UrunlerEkrani from "./UrunlerEkrani";

export const metadata: Metadata = { title: "Ürünler" };

export default function Sayfa() {
  return (
    <Suspense>
      <UrunlerEkrani />
    </Suspense>
  );
}
