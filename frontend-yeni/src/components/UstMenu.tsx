"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import HesapMenusu from "./HesapMenusu";
import { useKullanici } from "./Oturum";

type Baglanti = { yol: string; ad: string };

const GRUPLAR: { ad: string | null; baglantilar: Baglanti[]; admin?: boolean }[] = [
  { ad: null, baglantilar: [{ yol: "/", ad: "Ana Sayfa" }] },
  {
    ad: "İşlemler",
    baglantilar: [
      { yol: "/uretim", ad: "Üretim" },
      { yol: "/stok", ad: "Stok" },
      { yol: "/satis", ad: "Satış" },
    ],
  },
  {
    ad: "Tanımlar",
    baglantilar: [
      { yol: "/urunler", ad: "Ürünler" },
      { yol: "/parcalar", ad: "Parçalar" },
    ],
  },
  { ad: "Sistem", baglantilar: [{ yol: "/yonetim", ad: "Yönetim" }], admin: true },
];

function aktifMi(yol: string, simdiki: string) {
  return yol === "/" ? simdiki === "/" : simdiki === yol || simdiki.startsWith(`${yol}/`);
}

/** Sol menü + sayfa içeriği. Menünün altı (hesap: profil, tema, çıkış) dışarıdan verilir. */
export function UstMenuCercevesi({ admin = false, sag, children }: { admin?: boolean; sag?: ReactNode; children?: ReactNode }) {
  const simdiki = usePathname();

  return (
    <div className="duzen">
      <aside className="yan-menu">
        <Link className="yan-logo" href="/">
          MKC Bilişim
        </Link>
        <nav aria-label="Ana menü">
          {GRUPLAR.filter((g) => !g.admin || admin).map((g, i) => (
            <div key={i} className="yan-grup">
              {g.ad && <div className="yan-grup-ad">{g.ad}</div>}
              {g.baglantilar.map((b) => {
                const aktif = aktifMi(b.yol, simdiki);
                return (
                  <Link key={b.yol} href={b.yol} className={aktif ? "aktif" : undefined} aria-current={aktif ? "page" : undefined}>
                    {b.ad}
                  </Link>
                );
              })}
            </div>
          ))}
        </nav>
        {sag}
      </aside>
      <div className="govde">{children}</div>
    </div>
  );
}

export default function UstMenu({ children }: { children: ReactNode }) {
  const k = useKullanici();
  return (
    <UstMenuCercevesi admin={k.rol === "Admin"} sag={<HesapMenusu />}>
      {children}
    </UstMenuCercevesi>
  );
}
