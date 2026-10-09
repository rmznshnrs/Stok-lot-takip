import type { Metadata } from "next";
import { Suspense } from "react";
import StokEkrani from "./StokEkrani";

export const metadata: Metadata = { title: "Stok" };

export default function Sayfa() {
  return (
    <Suspense>
      <StokEkrani />
    </Suspense>
  );
}
