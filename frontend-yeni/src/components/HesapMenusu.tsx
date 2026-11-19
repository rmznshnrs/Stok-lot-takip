"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { simdikiTema, temaUygula, type Tema } from "@/lib/tema";
import { cikisYap } from "./Oturum";

export default function HesapMenusu() {
  const simdiki = usePathname();
  const [tema, setTema] = useState<Tema>(() => (typeof document === "undefined" ? "acik" : simdikiTema()));

  function temaDegistir() {
    const yeni = tema === "koyu" ? "acik" : "koyu";
    temaUygula(yeni);
    setTema(yeni);
  }

  const profilde = simdiki === "/profil";
  return (
    <div className="yan-alt">
      <Link href="/profil" className={`yan-alt-oge yan-alt-oge--profil${profilde ? " aktif" : ""}`} aria-current={profilde ? "page" : undefined}>
        Profil
      </Link>
      <button type="button" className="yan-alt-oge yan-alt-oge--tema" onClick={temaDegistir} suppressHydrationWarning>
        {tema === "koyu" ? "Açık mod" : "Koyu mod"}
      </button>
      <button type="button" className="yan-alt-oge yan-alt-oge--cikis" onClick={cikisYap}>
        Çıkış
      </button>
    </div>
  );
}
