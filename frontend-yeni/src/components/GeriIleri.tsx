"use client";

import { useRouter } from "next/navigation";

export default function GeriIleri() {
  const router = useRouter();
  return (
    <div className="geri-ileri" role="group" aria-label="Sayfa geçmişi">
      <button type="button" className="btn btn--ikincil geri-ileri-btn" onClick={() => router.back()}>← Geri</button>
      <button type="button" className="btn btn--ikincil geri-ileri-btn" onClick={() => router.forward()}>İleri →</button>
    </div>
  );
}
