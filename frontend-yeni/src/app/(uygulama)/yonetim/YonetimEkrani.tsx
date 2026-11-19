"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useAdminMi } from "@/components/Oturum";
import IslemGecmisi from "./IslemGecmisi";
import Kullanicilar from "./Kullanicilar";
import Raporlar from "./Raporlar";

export default function YonetimEkrani() {
  const admin = useAdminMi();
  const sekme = useSearchParams().get("sekme");
  const islemlerde = sekme === "islemler";
  const raporda = sekme === "rapor";
  if (!admin) {
    return (
      <main className="icerik">
        <section className="kart kart--genis">
          <p className="not not--buyuk">Yetkiniz yok.</p>
        </section>
      </main>
    );
  }
  return (
    <>
      <div className="ust-sekme-satiri">
        <h1>Yönetim</h1>
        <nav className="sekmeler sekmeler--buyuk" aria-label="Yönetim">
          <Link href="/yonetim" className={`sekme${sekme ? "" : " sekme--aktif"}`} aria-current={sekme ? undefined : "page"}>
            Kullanıcılar
          </Link>
          <Link href="/yonetim?sekme=islemler" className={`sekme${islemlerde ? " sekme--aktif" : ""}`} aria-current={islemlerde ? "page" : undefined}>
            İşlem geçmişi
          </Link>
          <Link href="/yonetim?sekme=rapor" className={`sekme${raporda ? " sekme--aktif" : ""}`} aria-current={raporda ? "page" : undefined}>
            Excel Raporu
          </Link>
        </nav>
      </div>
      <main className="icerik icerik--dikey">{islemlerde ? <IslemGecmisi /> : raporda ? <Raporlar /> : <Kullanicilar />}</main>
    </>
  );
}
