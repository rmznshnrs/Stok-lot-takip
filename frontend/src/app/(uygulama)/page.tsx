"use client";

import { useKullanici } from "@/components/Oturum";

export default function AnaSayfa() {
  const k = useKullanici();
  return (
    <main className="icerik icerik--dikey">
      <section className="kart">
        <h2>Hoş geldiniz, {k.ad}</h2>
        <p className="not">Üst menüden bir ekran seçin.</p>
      </section>
    </main>
  );
}
