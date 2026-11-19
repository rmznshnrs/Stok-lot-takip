"use client";

import { useSearchParams } from "next/navigation";
import LotSayfasi from "@/components/LotSayfasi";
import StokEkrani from "./StokEkrani";

export default function StokSayfasi() {
  const lot = useSearchParams().get("lot")?.trim();
  return lot ? <LotSayfasi lotNo={lot} /> : <StokEkrani />;
}
