"use client";

import { useRouter } from "next/navigation";
import { useSyncExternalStore } from "react";

type Gezinme = { canGoForward: boolean; addEventListener: (t: string, f: () => void) => void; removeEventListener: (t: string, f: () => void) => void };
const gezinme = () => (typeof window === "undefined" ? undefined : (window as unknown as { navigation?: Gezinme }).navigation);

function abone(degisti: () => void) {
  const g = gezinme();
  g?.addEventListener("currententrychange", degisti);
  return () => g?.removeEventListener("currententrychange", degisti);
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
