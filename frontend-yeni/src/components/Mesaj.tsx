"use client";

import { useEffect, useState } from "react";

export type MesajTuru = "success" | "error" | "warning" | "info";
export interface MesajBilgisi { tur: MesajTuru; metin: string }

const SURE = 5000;

export default function Mesaj({ mesaj }: { mesaj: MesajBilgisi | null }) {
  const [gizlenen, setGizlenen] = useState<MesajBilgisi | null>(null);

  useEffect(() => {
    if (!mesaj || (mesaj.tur !== "success" && mesaj.tur !== "info")) return;
    const z = window.setTimeout(() => setGizlenen(mesaj), SURE);
    return () => window.clearTimeout(z);
  }, [mesaj]);

  if (!mesaj || gizlenen === mesaj) return null;
  return (
    <div className={`mesaj mesaj--${mesaj.tur}`} role={mesaj.tur === "error" ? "alert" : "status"}>
      {mesaj.metin}
    </div>
  );
}
