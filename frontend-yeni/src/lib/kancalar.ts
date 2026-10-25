"use client";

import { useCallback, useEffect, useState } from "react";

/**
 * Veriyi yükler; bagimlilar değişince yeniden yükler. Son istek kazanır (eski yanıt
 * yeni yanıtın üstüne yazılmaz).
 */
export function useVeri<T>(yukle: () => Promise<T>, bagimlilar: unknown[]) {
  const [veri, setVeri] = useState<T | null>(null);
  const [hata, setHata] = useState("");
  const [sayac, setSayac] = useState(0);

  useEffect(() => {
    let gecerli = true;
    yukle()
      .then((v) => {
        if (gecerli) {
          setVeri(v);
          setHata("");
        }
      })
      .catch((e: Error) => {
        if (gecerli) setHata(e.message);
      });
    return () => {
      gecerli = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...bagimlilar, sayac]);

  const yenile = useCallback(() => setSayac((s) => s + 1), []);
  return { veri, hata, yenile };
}

/** Yazarken her harfte istek atmamak için değeri gecikmeli döndürür. */
export function useGecikmeli<T>(deger: T, ms = 250) {
  const [gecikmeli, setGecikmeli] = useState(deger);
  useEffect(() => {
    const z = setTimeout(() => setGecikmeli(deger), ms);
    return () => clearTimeout(z);
  }, [deger, ms]);
  return gecikmeli;
}
