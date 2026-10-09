"use client";

import { useAdminMi } from "@/components/Oturum";
import Kullanicilar from "./Kullanicilar";
import StokDuzeltme from "./StokDuzeltme";

/** Yalnız Admin: kullanıcı yönetimi ve stok düzeltme. API de rolü ayrıca denetler. */
export default function YonetimEkrani() {
  const admin = useAdminMi();
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
    <main className="icerik">
      <StokDuzeltme />
      <Kullanicilar />
    </main>
  );
}
