"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import HesapMenusu from "./HesapMenusu";
import { useKullanici } from "./Oturum";

const BAGLANTILAR = [
  { yol: "/", ad: "Ana Sayfa" },
  { yol: "/izlenebilirlik", ad: "İzlenebilirlik" },
  { yol: "/uretim", ad: "Üretim" },
  { yol: "/stok", ad: "Stok" },
  { yol: "/satis", ad: "Satış" },
  { yol: "/urunler", ad: "Ürünler" },
  { yol: "/parcalar", ad: "Parçalar" },
] as const;

function aktifMi(yol: string, simdiki: string) {
  return yol === "/" ? simdiki === "/" : simdiki === yol || simdiki.startsWith(`${yol}/`);
}

/** Logo + menü; sağ taraf (hesap menüsü) dışarıdan verilir. Kullanıcı gelmeden de çizilebilir. */
export function UstMenuCercevesi({ admin = false, children }: { admin?: boolean; children?: ReactNode }) {
  const simdiki = usePathname();
  const baglantilar = admin ? [...BAGLANTILAR, { yol: "/yonetim", ad: "Yönetim" }] : BAGLANTILAR;

  return (
    <header className="ust">
      <Link className="logo" href="/">MKC BİLİŞİM</Link>
      <nav className="menu" aria-label="Ana menü">
        {baglantilar.map((b) => {
          const aktif = aktifMi(b.yol, simdiki);
          return (
            <Link key={b.yol} href={b.yol} className={aktif ? "aktif" : undefined} aria-current={aktif ? "page" : undefined}>
              {b.ad}
            </Link>
          );
        })}
      </nav>
      {children}
    </header>
  );
}

export default function UstMenu() {
  const k = useKullanici();
  return (
    <UstMenuCercevesi admin={k.rol === "Admin"}>
      <HesapMenusu />
    </UstMenuCercevesi>
  );
}
