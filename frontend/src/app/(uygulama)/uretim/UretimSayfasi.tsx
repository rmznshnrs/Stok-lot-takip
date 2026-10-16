"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import UretimEkrani from "./UretimEkrani";
import UretimGecmisi from "./UretimGecmisi";

/** Üretim sayfasının iki sekmesi: yeni üretim ve üretim geçmişi (?sekme=gecmis). */
export default function UretimSayfasi() {
  const gecmis = useSearchParams().get("sekme") === "gecmis";
  return (
    <>
      <div className="ust-sekme-satiri">
        <nav className="sekmeler sekmeler--buyuk" aria-label="Üretim">
          <Link href="/uretim" className={`sekme${gecmis ? "" : " sekme--aktif"}`} aria-current={gecmis ? undefined : "page"}>
            Yeni üretim
          </Link>
          <Link href="/uretim?sekme=gecmis" className={`sekme${gecmis ? " sekme--aktif" : ""}`} aria-current={gecmis ? "page" : undefined}>
            Üretim geçmişi
          </Link>
        </nav>
      </div>
      {gecmis ? <UretimGecmisi /> : <UretimEkrani />}
    </>
  );
}
