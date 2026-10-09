import type { Metadata } from "next";
import ParcalarEkrani from "./ParcalarEkrani";

export const metadata: Metadata = { title: "Parçalar" };

export default function Sayfa() {
  return <ParcalarEkrani />;
}
