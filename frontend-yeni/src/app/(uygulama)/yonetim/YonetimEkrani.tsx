"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useAdminMi } from "@/components/Oturum";
import IslemGecmisi from "./IslemGecmisi";
import Kullanicilar from "./Kullanicilar";

/** Yalnız Admin: kullanıcı yönetimi ve işlem geçmişi (?sekme=islemler). API de rolü ayrıca denetler. Stok düzeltme Parçalar ekranında. */
export default function YonetimEkrani() {
  const admin = useAdminMi();
  const islemlerde = useSearchParams().get("sekme") === "islemler";
  if (!admin) {
    return (
      <main className="icerik">
        <section className="kart kart--genis">
          <p className="not not--buyuk">Bu sayfa yalnız yöneticiler içindir.</p>
        </section>
      </main>
    );
  }
  return (
    <>
      <div className="ust-sekme-satiri">
        <h1>Yönetim</h1>
        <nav className="sekmeler sekmeler--buyuk" aria-label="Yönetim">
          <Link href="/yonetim" className={`sekme${islemlerde ? "" : " sekme--aktif"}`} aria-current={islemlerde ? undefined : "page"}>
            Kullanıcılar
          </Link>
          <Link href="/yonetim?sekme=islemler" className={`sekme${islemlerde ? " sekme--aktif" : ""}`} aria-current={islemlerde ? "page" : undefined}>
            İşlem geçmişi
          </Link>
        </nav>
      </div>
      <main className="icerik icerik--dikey">{islemlerde ? <IslemGecmisi /> : <Kullanicilar />}</main>
    </>
  );
}
