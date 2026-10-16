import type { Metadata } from "next";
import { Suspense } from "react";
import UretimSayfasi from "./UretimSayfasi";

export const metadata: Metadata = { title: "Üretim" };

export default function Sayfa() {
  return (
    <Suspense>
      <UretimSayfasi />
    </Suspense>
  );
}
