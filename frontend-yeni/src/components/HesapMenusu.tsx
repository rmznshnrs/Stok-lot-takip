"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState } from "react";
import { simdikiTema, temaUygula, type Tema } from "@/lib/tema";
import Ikon from "./Ikon";
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
      <Link href="/profil" className={`yan-alt-oge yan-alt-oge--profil${profilde ? " aktif" : ""}`} aria-current={profilde ? "page" : undefined} title="Profil">
        <Ikon ad="profil" />
        <span className="yan-yazi">Profil</span>
      </Link>
      <button type="button" className="yan-alt-oge yan-alt-oge--tema" onClick={temaDegistir} suppressHydrationWarning
        title={tema === "koyu" ? "Açık mod" : "Koyu mod"}>
        <Ikon ad={tema === "koyu" ? "gunes" : "ay"} />
        <span className="yan-yazi" suppressHydrationWarning>{tema === "koyu" ? "Açık mod" : "Koyu mod"}</span>
      </button>
      <button type="button" className="yan-alt-oge yan-alt-oge--cikis" onClick={cikisYap} title="Çıkış">
        <Ikon ad="cikis" />
        <span className="yan-yazi">Çıkış</span>
      </button>
    </div>
  );
}
