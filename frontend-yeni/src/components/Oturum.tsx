"use client";

import { createContext, useContext, useEffect, useState, type ReactNode } from "react";
import { ApiHatasi, oturum, type Kullanici } from "@/lib/api";
import { UstMenuCercevesi } from "./UstMenu";

const OturumBaglami = createContext<Kullanici | null>(null);

const HAFIZA = "kullanici";

export function hatirla(k: Kullanici | null) {
  try {
    if (k) localStorage.setItem(HAFIZA, JSON.stringify(k));
    else localStorage.removeItem(HAFIZA);
  } catch {
  }
}

function hatirlanan(): Kullanici | null {
  try {
    const k = localStorage.getItem(HAFIZA);
    return k ? (JSON.parse(k) as Kullanici) : null;
  } catch {
    return null;
  }
}

export function useKullanici(): Kullanici {
  const k = useContext(OturumBaglami);
  if (!k) throw new Error("useKullanici, OturumSaglayici dışında kullanıldı.");
  return k;
}

export function useAdminMi() {
  return useKullanici().rol === "Admin";
}

export async function cikisYap() {
  hatirla(null);
  try {
    await oturum.cikis();
  } finally {
    // eslint-disable-next-line @next/next/no-location-assign-relative-destination -- tam yenileme: bellekteki veriler temizlensin
    window.location.href = "/giris";
  }
}

export function OturumSaglayici({ children }: { children: ReactNode }) {
  const [kullanici, setKullanici] = useState<Kullanici | null>(null);
  const [hata, setHata] = useState("");

  useEffect(() => {
    let gecerli = true;
    const onceki = hatirlanan();
    if (onceki) queueMicrotask(() => gecerli && setKullanici((k) => k ?? onceki));
    oturum
      .ben()
      .then((k) => {
        hatirla(k);
        if (gecerli) setKullanici(k);
      })
      .catch((e) => {
        if (e instanceof ApiHatasi && e.durum === 401) hatirla(null);
        else if (gecerli && !onceki) setHata(e.message);
      });
    return () => {
      gecerli = false;
    };
  }, []);

  if (hata) {
    return (
      <UstMenuCercevesi>
        <main className="giris-sayfa">
          <div className="mesaj mesaj--error" role="alert">{hata}</div>
        </main>
      </UstMenuCercevesi>
    );
  }
  if (!kullanici) {
    return (
      <UstMenuCercevesi>
        <main className="sayfa-bekliyor" aria-busy="true" aria-label="Yükleniyor">
          <span className="donen" />
        </main>
      </UstMenuCercevesi>
    );
  }
  return <OturumBaglami.Provider value={kullanici}>{children}</OturumBaglami.Provider>;
}
