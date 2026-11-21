"use client";

import { useMemo, useState, type ReactNode } from "react";

type Deger = string | number | boolean | null | undefined;
type Yon = "artan" | "azalan";

function karsilastir(a: Deger, b: Deger) {
  const bosA = a === null || a === undefined || a === "";
  const bosB = b === null || b === undefined || b === "";
  if (bosA || bosB) return bosA === bosB ? 0 : bosA ? 1 : -1;
  if (typeof a === "number" && typeof b === "number") return a - b;
  if (typeof a === "boolean" && typeof b === "boolean") return Number(a) - Number(b);
  return String(a).localeCompare(String(b), "tr", { numeric: true, sensitivity: "base" });
}

export function useSiralama<T, A extends string>(liste: T[] | null | undefined, alanlar: Record<A, (x: T) => Deger>) {
  const [durum, setDurum] = useState<{ alan: A; yon: Yon } | null>(null);

  const sirali = useMemo(() => {
    if (!liste || !durum) return liste;
    const al = alanlar[durum.alan];
    const carpan = durum.yon === "artan" ? 1 : -1;
    return liste
      .map((x, i) => ({ x, i }))
      .sort((p, q) => {
        const da = al(p.x), db = al(q.x);
        const bosA = da === null || da === undefined || da === "";
        const bosB = db === null || db === undefined || db === "";
        if (bosA !== bosB) return bosA ? 1 : -1;
        return karsilastir(da, db) * carpan || p.i - q.i;
      })
      .map((p) => p.x);
  }, [liste, durum, alanlar]);

  function degistir(alan: A) {
    setDurum((d) => (!d || d.alan !== alan ? { alan, yon: "artan" } : d.yon === "artan" ? { alan, yon: "azalan" } : null));
  }

  function baslik(alan: A, etiket: ReactNode, className?: string) {
    const aktif = durum?.alan === alan ? durum.yon : null;
    return (
      <th className={className} aria-sort={aktif === "artan" ? "ascending" : aktif === "azalan" ? "descending" : undefined}>
        <button type="button" className={`sirala-dugme${aktif ? " sirala-dugme--aktif" : ""}`} onClick={() => degistir(alan)}
          title={aktif === "artan" ? "Azalana çevir" : aktif === "azalan" ? "Sıralamayı kaldır" : "Sırala"}>
          {etiket}
          <span className="sirala-ok" aria-hidden="true">{aktif === "artan" ? "▲" : aktif === "azalan" ? "▼" : "⇅"}</span>
        </button>
      </th>
    );
  }

  return { sirali, baslik, siralamaAnahtari: durum ? `${durum.alan}-${durum.yon}` : "" };
}
