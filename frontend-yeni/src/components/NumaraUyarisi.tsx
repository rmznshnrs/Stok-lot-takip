"use client";

import { useEffect, useState } from "react";
import { kontrol, type NumaraTuru2 } from "@/lib/api";
import { useGecikmeli } from "@/lib/kancalar";

export function useNumaraKontrol(tur: NumaraTuru2, deger: string, haric?: number) {
  const aranan = useGecikmeli(deger.trim(), 350);
  const [sonuc, setSonuc] = useState<{ deger: string; mesaj: string } | null>(null);

  useEffect(() => {
    if (!aranan) return;
    let gecerli = true;
    kontrol.numara(tur, aranan, haric)
      .then((s) => { if (gecerli) setSonuc({ deger: aranan, mesaj: s.kullaniliyor ? s.mesaj : "" }); })
      .catch(() => {});
    return () => { gecerli = false; };
  }, [tur, aranan, haric]);

  return sonuc && sonuc.deger === deger.trim() && deger.trim() ? sonuc.mesaj : "";
}

export default function NumaraUyarisi({ tur, deger, haric }: { tur: NumaraTuru2; deger: string; haric?: number }) {
  const mesaj = useNumaraKontrol(tur, deger, haric);
  if (!mesaj) return null;
  return <span className="numara-uyarisi" role="alert">{mesaj}</span>;
}
