"use client";

import { useAdminMi } from "@/components/Oturum";
import Kullanicilar from "./Kullanicilar";

/** Yalnız Admin: kullanıcı yönetimi. API de rolü ayrıca denetler. Stok düzeltme Parçalar ekranında. */
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
      <Kullanicilar />
    </main>
  );
}
