import type { Metadata } from "next";
import { Suspense } from "react";
import GirisFormu from "./GirisFormu";

export const metadata: Metadata = { title: "Giriş" };

export default function GirisSayfasi() {
  return (
    <main className="giris-sayfa">
      <Suspense>
        <GirisFormu />
      </Suspense>
    </main>
  );
}
