"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";

const SEKMELER = [
  { sekme: null, ad: "Üretim Sırası", adres: "/uretim" },
  { sekme: "uretimde", ad: "Üretimde", adres: "/uretim?sekme=uretimde" },
  { sekme: "gecmis", ad: "Üretildi", adres: "/uretim?sekme=gecmis" },
];

export default function UretimSekmeleri() {
  const simdiki = useSearchParams().get("sekme");
  return (
    <nav className="sekmeler" aria-label="Üretim">
      {SEKMELER.map((s) => {
        const aktif = simdiki === s.sekme;
        return (
          <Link key={s.adres} href={s.adres} className={`sekme${aktif ? " sekme--aktif" : ""}`} aria-current={aktif ? "page" : undefined}>
            {s.ad}
          </Link>
        );
      })}
    </nav>
  );
}
