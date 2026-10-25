"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { simdikiTema, temaUygula, type Tema } from "@/lib/tema";
import { cikisYap } from "./Oturum";

/** Üst çubuğun sağı: profil düğmesi ve yanında ⋮ menüsü (açık/koyu mod, çıkış). */
export default function HesapMenusu() {
  const simdiki = usePathname();
  const [acik, setAcik] = useState(false);
  const [tema, setTema] = useState<Tema>(simdikiTema);
  const kutu = useRef<HTMLDivElement>(null);

  // Dışarı tıklayınca veya Esc ile kapanır
  useEffect(() => {
    if (!acik) return;
    const tikla = (e: MouseEvent) => {
      if (!kutu.current?.contains(e.target as Node)) setAcik(false);
    };
    const tus = (e: KeyboardEvent) => {
      if (e.key === "Escape") setAcik(false);
    };
    document.addEventListener("mousedown", tikla);
    document.addEventListener("keydown", tus);
    return () => {
      document.removeEventListener("mousedown", tikla);
      document.removeEventListener("keydown", tus);
    };
  }, [acik]);

  function temaDegistir() {
    const yeni = tema === "koyu" ? "acik" : "koyu";
    temaUygula(yeni);
    setTema(yeni);
    setAcik(false);
  }

  return (
    <div className="hesap" ref={kutu}>
      <Link href="/profil" className={`btn btn--ikincil btn--kucuk${simdiki === "/profil" ? " btn--secili" : ""}`}>
        Profil
      </Link>
      <button
        type="button"
        className="btn btn--ikincil btn--kucuk hesap-nokta"
        aria-label="Diğer seçenekler"
        aria-haspopup="menu"
        aria-expanded={acik}
        onClick={() => setAcik(!acik)}
      >
        ⋮
      </button>
      {acik && (
        <div className="hesap-menu" role="menu">
          <button type="button" className="hesap-oge" role="menuitem" onClick={temaDegistir}>
            {tema === "koyu" ? "Açık mod" : "Koyu mod"}
          </button>
          <button type="button" className="hesap-oge hesap-oge--cikis" role="menuitem" onClick={cikisYap}>
            Çıkış
          </button>
        </div>
      )}
    </div>
  );
}
