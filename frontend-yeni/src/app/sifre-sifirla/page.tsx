import type { Metadata } from "next";
import { Suspense } from "react";
import SifreSifirlaFormu from "./SifreSifirlaFormu";

export const metadata: Metadata = { title: "Şifre sıfırla" };

export default function SifreSifirlaSayfasi() {
  return (
    <main className="giris-sayfa">
      <Suspense>
        <SifreSifirlaFormu />
      </Suspense>
    </main>
  );
}
