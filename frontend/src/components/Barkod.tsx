"use client";

import bwipjs from "bwip-js/browser";
import JsBarcode from "jsbarcode";
import { useEffect, useMemo, useRef } from "react";
import { gs1Metni, type Gs1Alan } from "@/lib/gs1";

/** Çizgili barkod (Code 128). El okuyucularının hepsi ve telefon kamerası okur. */
export default function Barkod({ deger, yukseklik = 40 }: { deger: string; yukseklik?: number }) {
  const svg = useRef<SVGSVGElement>(null);

  useEffect(() => {
    if (!svg.current) return;
    JsBarcode(svg.current, deger, {
      format: "CODE128",
      height: yukseklik,
      width: 1.6,
      margin: 0,
      displayValue: true,
      font: "IBM Plex Mono, monospace",
      fontSize: 12,
      textMargin: 2,
      background: "transparent",
    });
  }, [deger, yukseklik]);

  return <svg ref={svg} className="barkod" role="img" aria-label={`Barkod: ${deger}`} />;
}

/**
 * GS1 karekod (DataMatrix, ilaç kutularındaki gibi). İçinde birden fazla bilgi taşır.
 * Çizilemezse (geçersiz veri) bir uyarı gösterilir.
 */
export function Karekod({ alanlar }: { alanlar: Gs1Alan[] }) {
  const metin = gs1Metni(alanlar);
  const sonuc = useMemo(() => {
    try {
      // Yalnız kare modüllerden oluşan bir SVG döner (metin içermez).
      // dontlint: GS1 normalde (240)/(21)/(10) ile birlikte bir GTIN (01) ister; firmanın GTIN'i yok.
      const secenek = { bcid: "gs1datamatrix", text: metin, scale: 3, paddingwidth: 0, paddingheight: 0, dontlint: true };
      return { svg: bwipjs.toSVG(secenek as Parameters<typeof bwipjs.toSVG>[0]) };
    } catch {
      return { hata: "Karekod çizilemedi." };
    }
  }, [metin]);

  if ("hata" in sonuc) return <div className="karekod-hata">{sonuc.hata}</div>;
  return <div className="karekod" role="img" aria-label={`Karekod: ${metin}`} dangerouslySetInnerHTML={{ __html: sonuc.svg }} />;
}
