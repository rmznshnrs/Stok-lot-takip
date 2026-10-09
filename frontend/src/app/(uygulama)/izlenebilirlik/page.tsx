import type { Metadata } from "next";
import { Suspense } from "react";
import IzlenebilirlikEkrani from "./IzlenebilirlikEkrani";

export const metadata: Metadata = { title: "İzlenebilirlik" };

export default function Sayfa() {
  return (
    <Suspense>
      <IzlenebilirlikEkrani />
    </Suspense>
  );
}
