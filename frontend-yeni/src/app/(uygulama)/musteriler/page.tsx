import type { Metadata } from "next";
import { Suspense } from "react";
import MusterilerSayfasi from "./MusterilerSayfasi";

export const metadata: Metadata = { title: "Müşteriler" };

export default function Sayfa() {
  return (
    <Suspense>
      <MusterilerSayfasi />
    </Suspense>
  );
}
