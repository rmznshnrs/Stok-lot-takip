"use client";

import { useState, type InputHTMLAttributes } from "react";

/** Şifre girişi; sağdaki göz düğmesiyle yazılan şifre gösterilir / gizlenir. */
export default function SifreKutusu({ className = "", ...props }: Omit<InputHTMLAttributes<HTMLInputElement>, "type">) {
  const [goster, setGoster] = useState(false);
  return (
    <div className="sifre-kutu">
      <input {...props} type={goster ? "text" : "password"} className={`girdi ${className}`.trim()} />
      <button
        type="button"
        className="sifre-goz"
        aria-label={goster ? "Şifreyi gizle" : "Şifreyi göster"}
        aria-pressed={goster}
        title={goster ? "Şifreyi gizle" : "Şifreyi göster"}
        onClick={() => setGoster(!goster)}
      >
        <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
          <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7S2 12 2 12Z" />
          <circle cx="12" cy="12" r="3" />
          {goster && <path d="M3 3l18 18" />}
        </svg>
      </button>
    </div>
  );
}
