import type { Metadata } from "next";
import { Suspense } from "react";
import StokSayfasi from "./StokSayfasi";

export const metadata: Metadata = { title: "Stok" };

export default function Sayfa() {
  return (
    <Suspense>
      <StokSayfasi />
    </Suspense>
  );
}
