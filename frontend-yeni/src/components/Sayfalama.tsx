"use client";

import { useState } from "react";

const BOYUTLAR = [25, 50, 100] as const;
const ANAHTAR = "sayfa-boyutu";

function kayitliBoyut(): number {
  try {
    const b = Number(window.localStorage.getItem(ANAHTAR));
    return (BOYUTLAR as readonly number[]).includes(b) ? b : 25;
  } catch {
    return 25;
  }
}

/**
 * Uzun listeyi sayfalara böler. anahtar değişince (arama, süzgeç) ilk sayfaya dönülür.
 * Sayfa başına adet (25 / 50 / 100) tarayıcıda hatırlanır. cubuk: listenin altına konacak gezinme çubuğu.
 */
export function useSayfalama<T>(liste: T[] | null | undefined, anahtar = "") {
  const [boyut, setBoyut] = useState(() => (typeof window === "undefined" ? 25 : kayitliBoyut()));
  const [durum, setDurum] = useState({ anahtar, sayfa: 1 });
  const toplam = liste?.length ?? 0;
  const sayfaSayisi = Math.max(1, Math.ceil(toplam / boyut));
  const sayfa = Math.min(durum.anahtar === anahtar ? durum.sayfa : 1, sayfaSayisi);
  const gorunen = liste ? liste.slice((sayfa - 1) * boyut, sayfa * boyut) : liste;

  function boyutSec(b: number) {
    setBoyut(b);
    setDurum({ anahtar, sayfa: 1 });
    try {
      window.localStorage.setItem(ANAHTAR, String(b));
    } catch {
      // tarayıcı kaydetmeye izin vermiyorsa yalnız bu sayfada geçerli
    }
  }

  const cubuk = toplam > BOYUTLAR[0] ? (
    <Sayfalama toplam={toplam} sayfa={sayfa} sayfaSayisi={sayfaSayisi} boyut={boyut}
      git={(s) => setDurum({ anahtar, sayfa: s })} boyutSec={boyutSec} />
  ) : null;
  return { gorunen, cubuk };
}

function Sayfalama({ toplam, sayfa, sayfaSayisi, boyut, git, boyutSec }: {
  toplam: number; sayfa: number; sayfaSayisi: number; boyut: number;
  git: (sayfa: number) => void; boyutSec: (boyut: number) => void;
}) {
  // Çevredeki en çok 5 sayfa numarası
  const ilk = Math.max(1, Math.min(sayfa - 2, sayfaSayisi - 4));
  const numaralar = Array.from({ length: Math.min(5, sayfaSayisi) }, (_, i) => ilk + i);
  return (
    <nav className="sayfalama" aria-label="Sayfalar">
      <span className="sayfalama-bilgi">
        {(sayfa - 1) * boyut + 1}–{Math.min(sayfa * boyut, toplam)} / {toplam}
      </span>
      <label className="sayfalama-boyut">
        Sayfa başına
        <select className="girdi girdi--kucuk" value={boyut} onChange={(e) => boyutSec(Number(e.target.value))}>
          {BOYUTLAR.map((b) => <option key={b} value={b}>{b}</option>)}
        </select>
      </label>
      <span className="sayfalama-dugmeler">
        <button type="button" className="sayfa-dugme" disabled={sayfa === 1} onClick={() => git(sayfa - 1)} aria-label="Önceki sayfa">‹</button>
        {numaralar.map((n) => (
          <button key={n} type="button" className={`sayfa-dugme${n === sayfa ? " sayfa-dugme--aktif" : ""}`}
            aria-current={n === sayfa ? "page" : undefined} onClick={() => git(n)}>
            {n}
          </button>
        ))}
        <button type="button" className="sayfa-dugme" disabled={sayfa === sayfaSayisi} onClick={() => git(sayfa + 1)} aria-label="Sonraki sayfa">›</button>
      </span>
    </nav>
  );
}
