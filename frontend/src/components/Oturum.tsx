"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { ApiHatasi, oturum, type Kullanici } from "@/lib/api";

const OturumBaglami = createContext<Kullanici | null>(null);

/** Giriş yapmış kullanıcı. Yalnız OturumSaglayici içinde kullanılır. */
export function useKullanici(): Kullanici {
  const k = useContext(OturumBaglami);
  if (!k) throw new Error("useKullanici, OturumSaglayici dışında kullanıldı.");
  return k;
}

export function useAdminMi() {
  return useKullanici().rol === "Admin";
}

export async function cikisYap() {
  try {
    await oturum.cikis();
  } finally {
    // eslint-disable-next-line @next/next/no-location-assign-relative-destination -- tam yenileme: bellekteki veriler temizlensin
    window.location.href = "/giris";
  }
}

/** Kullanıcıyı API'den bir kez alır; gelene kadar sayfayı göstermez. */
export function OturumSaglayici({ children }: { children: ReactNode }) {
  const [kullanici, setKullanici] = useState<Kullanici | null>(null);
  const [hata, setHata] = useState("");

  useEffect(() => {
    oturum
      .ben()
      .then(setKullanici)
      .catch((e) => {
        // 401'de api.ts zaten giriş sayfasına yönlendirir
        if (!(e instanceof ApiHatasi && e.durum === 401)) setHata(e.message);
      });
  }, []);

  if (hata) {
    return (
      <main className="giris-sayfa">
        <div className="mesaj mesaj--error" role="alert">{hata}</div>
      </main>
    );
  }
  if (!kullanici) return <main className="icerik"><p className="yukleniyor">Yükleniyor…</p></main>;
  return <OturumBaglami.Provider value={kullanici}>{children}</OturumBaglami.Provider>;
}
