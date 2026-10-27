"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cikisYap } from "./Oturum";

/** Sol menünün altı: profil ve çıkış. */
export default function HesapMenusu() {
  const simdiki = usePathname();
  const profilde = simdiki === "/profil";
  return (
    <div className="yan-alt">
      <Link href="/profil" className={`yan-alt-oge yan-alt-oge--profil${profilde ? " aktif" : ""}`} aria-current={profilde ? "page" : undefined}>
        Profil
      </Link>
      <button type="button" className="yan-alt-oge yan-alt-oge--cikis" onClick={cikisYap}>
        Çıkış
      </button>
    </div>
  );
}
