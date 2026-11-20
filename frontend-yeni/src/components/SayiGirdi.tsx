"use client";

import { useEffect, useRef, type KeyboardEvent } from "react";

interface Props {
  id?: string;
  value: string | number;
  onChange: (deger: string) => void;
  min?: number;
  max?: number;
  required?: boolean;
  disabled?: boolean;
  placeholder?: string;
  "aria-label"?: string;
  onKeyDown?: (e: KeyboardEvent<HTMLInputElement>) => void;
  kucuk?: boolean;
}

export default function SayiGirdi({ id, value, onChange, min, max, required, disabled, placeholder, kucuk, onKeyDown, ...diger }: Props) {
  const deger = useRef(value);
  const zamanlayici = useRef<number | null>(null);
  useEffect(() => { deger.current = value; }, [value]);
  useEffect(() => () => durdur(), []);

  function sinirla(n: number) {
    if (min !== undefined && n < min) return min;
    if (max !== undefined && n > max) return max;
    return n;
  }

  function adim(yon: number) {
    const simdiki = Number(deger.current);
    const yeni = sinirla((Number.isFinite(simdiki) && String(deger.current).trim() !== "" ? simdiki : (min ?? 0) - (yon > 0 ? 1 : 0)) + yon);
    deger.current = String(yeni);
    onChange(String(yeni));
  }

  function durdur() {
    if (zamanlayici.current !== null) window.clearTimeout(zamanlayici.current);
    zamanlayici.current = null;
  }

  function basla(yon: number) {
    if (disabled) return;
    adim(yon);
    let gecikme = 380;
    const tekrar = () => {
      adim(yon);
      gecikme = Math.max(40, gecikme * 0.8);
      zamanlayici.current = window.setTimeout(tekrar, gecikme);
    };
    zamanlayici.current = window.setTimeout(tekrar, gecikme);
  }

  const n = Number(value);
  const enAz = min !== undefined && String(value).trim() !== "" && n <= min;
  const enCok = max !== undefined && n >= max;

  return (
    <span className={`sayi-girdi${kucuk ? " sayi-girdi--kucuk" : ""}`}>
      <button type="button" className="sayi-dugme" aria-label="Azalt" tabIndex={-1} disabled={disabled || enAz}
        onPointerDown={(e) => { e.preventDefault(); basla(-1); }} onPointerUp={durdur} onPointerLeave={durdur} onPointerCancel={durdur}
        onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") adim(-1); }}>−</button>
      <input id={id} type="number" inputMode="numeric" className="girdi sayi-kutu" value={value} min={min} max={max}
        required={required} disabled={disabled} placeholder={placeholder} aria-label={diger["aria-label"]}
        onChange={(e) => onChange(e.target.value)} onKeyDown={onKeyDown} />
      <button type="button" className="sayi-dugme" aria-label="Arttır" tabIndex={-1} disabled={disabled || enCok}
        onPointerDown={(e) => { e.preventDefault(); basla(1); }} onPointerUp={durdur} onPointerLeave={durdur} onPointerCancel={durdur}
        onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") adim(1); }}>+</button>
    </span>
  );
}
