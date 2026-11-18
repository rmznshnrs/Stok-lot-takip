"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import SirayaEkle from "./SirayaEkle";
import UretimGecmisi from "./UretimGecmisi";
import UretimSirasi from "./UretimSirasi";

/**
 * Üretim: sıra (varsayılan; ?yeni=1 sıraya ekleme formu) ve üretim geçmişi (?sekme=gecmis).
 * Ürünler doğrudan üretilmez, önce sıraya girer. Geri çağırmalar Müşteriler'de.
 */
export default function UretimSayfasi() {
  const params = useSearchParams();
  const gecmis = params.get("sekme") === "gecmis";
  const yeni = params.get("yeni") === "1";
  const ara = params.get("ara")?.trim() ?? "";
  return (
    <>
      <div className="ust-sekme-satiri">
        <h1>Üretim</h1>
        <nav className="sekmeler sekmeler--buyuk" aria-label="Üretim">
          <Link href="/uretim" className={`sekme${gecmis ? "" : " sekme--aktif"}`} aria-current={gecmis ? undefined : "page"}>
            Üretim Sırası
          </Link>
          <Link href="/uretim?sekme=gecmis" className={`sekme${gecmis ? " sekme--aktif" : ""}`} aria-current={gecmis ? "page" : undefined}>
            Üretim Geçmişi
          </Link>
        </nav>
      </div>
      {gecmis ? <UretimGecmisi key={ara} ilkArama={ara} /> : yeni ? <SirayaEkle /> : <UretimSirasi />}
    </>
  );
}
