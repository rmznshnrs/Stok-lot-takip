"use client";

import JsBarcode from "jsbarcode";
import { useEffect, useRef } from "react";

/** Code 128 barkod (SVG). Telefonun kamerası ve el okuyucuları bu türü okur. */
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
