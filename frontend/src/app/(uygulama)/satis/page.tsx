import type { Metadata } from "next";
import { Suspense } from "react";
import SatisSayfasi from "./SatisSayfasi";

export const metadata: Metadata = { title: "Satış" };

export default function Sayfa() {
  return (
    <Suspense>
      <SatisSayfasi />
    </Suspense>
  );
}
