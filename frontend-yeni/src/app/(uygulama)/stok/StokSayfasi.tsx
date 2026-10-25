"use client";

import { useSearchParams } from "next/navigation";
import LotSayfasi from "@/components/LotSayfasi";
import StokEkrani from "./StokEkrani";

/** Stok listesi; ?lot= verilirse o lotun sayfası. */
export default function StokSayfasi() {
  const lot = useSearchParams().get("lot")?.trim();
  return lot ? <LotSayfasi lotNo={lot} geri="/stok?gorunum=lot" /> : <StokEkrani />;
}
