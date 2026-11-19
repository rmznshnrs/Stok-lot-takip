import type { Metadata } from "next";
import { Suspense } from "react";
import SiparislerSayfasi from "./SiparislerSayfasi";

export const metadata: Metadata = { title: "Siparişler" };

export default function Sayfa() {
  return (
    <Suspense>
      <SiparislerSayfasi />
    </Suspense>
  );
}
