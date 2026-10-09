import type { Metadata } from "next";
import SatisEkrani from "./SatisEkrani";

export const metadata: Metadata = { title: "Satış" };

export default function Sayfa() {
  return <SatisEkrani />;
}
