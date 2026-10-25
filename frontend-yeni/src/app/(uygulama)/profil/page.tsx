import type { Metadata } from "next";
import ProfilBilgisi from "./ProfilBilgisi";
import SifreFormu from "../sifre/SifreFormu";

export const metadata: Metadata = { title: "Profil" };

export default function Sayfa() {
  return (
    <main className="icerik icerik--profil">
      <ProfilBilgisi />
      <SifreFormu />
    </main>
  );
}
