import type { Metadata } from "next";
import YonetimEkrani from "./YonetimEkrani";

export const metadata: Metadata = { title: "Yönetim" };

export default function Sayfa() {
  return <YonetimEkrani />;
}
