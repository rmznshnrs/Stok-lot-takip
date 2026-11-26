"use client";

import { useLayoutEffect, useRef } from "react";

export function useSatirSiniri<T extends HTMLElement>(satir: number, anahtar: unknown) {
  const kap = useRef<T>(null);
  useLayoutEffect(() => {
    const k = kap.current;
    if (!k) return;
    function ayarla() {
      if (!k) return;
      const satirlar = k.querySelectorAll<HTMLTableRowElement>("tbody tr");
      if (satirlar.length <= satir) {
        k.style.maxHeight = "none";
        return;
      }
      const ust = k.getBoundingClientRect().top - k.scrollTop;
      const son = satirlar[satir - 1].getBoundingClientRect().bottom;
      k.style.maxHeight = `${Math.ceil(son - ust + 6)}px`;
    }
    ayarla();
    window.addEventListener("resize", ayarla);
    return () => window.removeEventListener("resize", ayarla);
  }, [satir, anahtar]);
  return kap;
}
