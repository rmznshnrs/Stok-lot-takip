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

const DIGER: Record<string, string> = { "/profil": "Profil", "/sifre": "Şifre değiştir", "/etiket": "Etiket yazdır" };

function aktifMi(yol: string, simdiki: string) {
  return yol === "/" ? simdiki === "/" : simdiki === yol || simdiki.startsWith(`${yol}/`);
}

/** Üst çubuktaki konum: "İşlemler / Stok" gibi. */
function konum(simdiki: string) {
  for (const g of GRUPLAR) {
    const b = g.baglantilar.find((x) => aktifMi(x.yol, simdiki));
    if (b) return { grup: g.ad, ad: b.ad };
  }
  const diger = Object.keys(DIGER).find((y) => aktifMi(y, simdiki));
  return { grup: null, ad: diger ? DIGER[diger] : "" };
}

/** Sol menü + üst çubuk + sayfa içeriği. Sağ üst köşe (hesap menüsü) dışarıdan verilir. */
export function UstMenuCercevesi({ admin = false, sag, children }: { admin?: boolean; sag?: ReactNode; children?: ReactNode }) {
  const simdiki = usePathname();
  const k = konum(simdiki);

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
      </aside>
      <div className="govde">
        <header className="ust">
          <div className="ust-konum">
            {k.grup && <span>{k.grup} / </span>}
            <strong>{k.ad}</strong>
          </div>
          {sag}
        </header>
        {children}
      </div>
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
