import type { Metadata } from "next";
import KayitFormu from "./KayitFormu";

export const metadata: Metadata = { title: "Kayıt ol" };

export default function KayitSayfasi() {
  return (
    <main className="giris-sayfa">
      <KayitFormu />
    </main>
  );
}
