import type { Metadata } from "next";
import { Suspense } from "react";
import TedarikcilerSayfasi from "./TedarikcilerSayfasi";

export const metadata: Metadata = { title: "Tedarikçiler" };

export default function Sayfa() {
  return (
    <Suspense>
      <TedarikcilerSayfasi />
    </Suspense>
  );
}
