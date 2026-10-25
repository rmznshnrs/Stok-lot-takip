"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import SeriSayfasi from "@/components/SeriSayfasi";
import UretimEkrani from "./UretimEkrani";
import GeriCagirmalar from "./GeriCagirmalar";
import UretimGecmisi from "./UretimGecmisi";

/** Üretim sayfasının iki sekmesi: yeni üretim ve üretim geçmişi (?sekme=gecmis). */
export default function UretimSayfasi() {
  const params = useSearchParams();
  const sekme = params.get("sekme");
  const seri = params.get("seri")?.trim();
  const gecmis = sekme === "gecmis";
  const geri = sekme === "geri";
  return (
    <>
      <div className="ust-sekme-satiri">
        <h1>Üretim</h1>
        <nav className="sekmeler sekmeler--buyuk" aria-label="Üretim">
          <Link href="/uretim" className={`sekme${gecmis || geri ? "" : " sekme--aktif"}`} aria-current={gecmis || geri ? undefined : "page"}>
            Yeni üretim
          </Link>
          <Link href="/uretim?sekme=gecmis" className={`sekme${gecmis ? " sekme--aktif" : ""}`} aria-current={gecmis ? "page" : undefined}>
            Üretim geçmişi
          </Link>
          <Link href="/uretim?sekme=geri" className={`sekme${geri ? " sekme--aktif" : ""}`} aria-current={geri ? "page" : undefined}>
            Geri çağırmalar
          </Link>
        </nav>
      </div>
      {seri ? (
        <SeriSayfasi seriNo={seri} geri={geri ? "/uretim?sekme=geri" : "/uretim?sekme=gecmis"} />
      ) : gecmis ? (
        <UretimGecmisi />
      ) : geri ? (
        <GeriCagirmalar />
      ) : (
        <UretimEkrani />
      )}
    </>
  );
}
