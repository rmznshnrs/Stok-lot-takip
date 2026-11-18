import type { Metadata } from "next";
import { Suspense } from "react";
import SevkiyatSayfasi from "./SevkiyatSayfasi";

export const metadata: Metadata = { title: "Sevkiyat" };

export default function Sayfa() {
  return (
    <Suspense>
      <SevkiyatSayfasi />
    </Suspense>
  );
}
