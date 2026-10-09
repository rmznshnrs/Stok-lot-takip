import type { Metadata } from "next";
import AnaSayfaEkrani from "./AnaSayfaEkrani";

export const metadata: Metadata = { title: "Ana Sayfa" };

export default function Sayfa() {
  return <AnaSayfaEkrani />;
}
