"use client";

import { useRouter } from "next/navigation";
import { useSyncExternalStore } from "react";

type Gezinme = { canGoForward: boolean; addEventListener: (t: string, f: () => void) => void; removeEventListener: (t: string, f: () => void) => void };
const gezinme = () => (typeof window === "undefined" ? undefined : (window as unknown as { navigation?: Gezinme }).navigation);

function abone(degisti: () => void) {
  const g = gezinme();
  let zamanlayici = 0;
  const ertele = () => {
    window.clearTimeout(zamanlayici);
    zamanlayici = window.setTimeout(degisti, 0);
  };
  g?.addEventListener("currententrychange", ertele);
  return () => {
    window.clearTimeout(zamanlayici);
    g?.removeEventListener("currententrychange", ertele);
  };
}

export default function GeriIleri() {
  const router = useRouter();
  const ileriVar = useSyncExternalStore(abone, () => gezinme()?.canGoForward ?? false, () => false);
  return (
    <div className="geri-ileri" role="group" aria-label="Sayfa geçmişi">
      <button type="button" className="btn btn--ikincil geri-ileri-btn" onClick={() => router.back()}>← Geri</button>
      {ileriVar && <button type="button" className="btn btn--ikincil geri-ileri-btn" onClick={() => router.forward()}>İleri →</button>}
    </div>
  );
}
