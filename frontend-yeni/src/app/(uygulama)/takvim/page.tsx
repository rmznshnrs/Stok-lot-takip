import type { Metadata } from "next";
import { Suspense } from "react";
import TakvimSayfasi from "./TakvimSayfasi";

export const metadata: Metadata = { title: "Takvim" };

export default function Sayfa() {
  return (
    <Suspense>
      <TakvimSayfasi />
    </Suspense>
  );
}
