import type { Metadata } from "next";
import SifreFormu from "./SifreFormu";

export const metadata: Metadata = { title: "Şifre değiştir" };

export default function Sayfa() {
  return (
    <main className="icerik">
      <SifreFormu />
    </main>
  );
}
