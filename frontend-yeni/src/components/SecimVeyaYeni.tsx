"use client";

import { useState } from "react";

const DIGER = "__diger__";

export default function SecimVeyaYeni({ id, secenekler, value, onChange, required, yeniEtiketi = "Diğer…", bosEtiketi = "Seçin" }: {
  id: string;
  secenekler: string[];
  value: string;
  onChange: (deger: string) => void;
  required?: boolean;
  yeniEtiketi?: string;
  bosEtiketi?: string;
}) {
  const listede = secenekler.some((s) => s.toLocaleLowerCase("tr") === value.trim().toLocaleLowerCase("tr"));
  const [yeni, setYeni] = useState(!listede && value.trim() !== "");

  return (
    <span className="secim-veya-yeni">
      <select id={id} className="girdi" required={required && !yeni} value={yeni ? DIGER : listede ? value : ""}
        onChange={(e) => {
          if (e.target.value === DIGER) {
            setYeni(true);
            onChange("");
          } else {
            setYeni(false);
            onChange(e.target.value);
          }
        }}>
        <option value="">{bosEtiketi}</option>
        {secenekler.map((s) => <option key={s} value={s}>{s}</option>)}
        <option value={DIGER}>{yeniEtiketi}</option>
      </select>
      {yeni && (
        <input className="girdi" aria-label={yeniEtiketi} required={required} maxLength={200} autoComplete="off" autoFocus
          value={value} onChange={(e) => onChange(e.target.value)} />
      )}
    </span>
  );
}
