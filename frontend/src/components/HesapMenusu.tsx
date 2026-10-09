"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { simdikiTema, temaUygula, type Tema } from "@/lib/tema";
import { cikisYap, useKullanici } from "./Oturum";

/** Üst menünün sağındaki ☰: kullanıcı, şifre değiştir, açık/koyu tema, çıkış. */
export default function HesapMenusu() {
  const k = useKullanici();
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
  }

  return (
    <div className="hesap" ref={kutu}>
      <button
        type="button"
        className="hesap-dugme"
        aria-label="Hesap menüsü"
        aria-haspopup="menu"
        aria-expanded={acik}
        onClick={() => setAcik(!acik)}
      >
        ☰
      </button>
      {acik && (
        <div className="hesap-menu" role="menu">
          <div className="hesap-kim">
            <span className="hesap-ad">{k.ad} {k.soyad}</span>
            <span className="hesap-rol">{k.rol === "Admin" ? "Yönetici" : "Kullanıcı"} · {k.kullaniciAdi}</span>
          </div>
          <Link href="/sifre" className="hesap-oge" role="menuitem" onClick={() => setAcik(false)}>
            Şifre değiştir
          </Link>
          <button type="button" className="hesap-oge" role="menuitem" onClick={temaDegistir}>
            {tema === "koyu" ? "Açık moda geç" : "Koyu moda geç"}
          </button>
          <button type="button" className="hesap-oge hesap-oge--cikis" role="menuitem" onClick={cikisYap}>
            Çıkış yap
          </button>
        </div>
      )}
    </div>
  );
}
