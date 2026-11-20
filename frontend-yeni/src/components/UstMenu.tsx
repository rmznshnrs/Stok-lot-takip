"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import HesapMenusu from "./HesapMenusu";
import Ikon from "./Ikon";
import { useKullanici } from "./Oturum";

type Baglanti = { yol: string; ad: string };

const GRUPLAR: { ad: string | null; baglantilar: Baglanti[]; admin?: boolean }[] = [
  { ad: null, baglantilar: [{ yol: "/", ad: "Ana Sayfa" }] },
  {
    ad: "İşlemler",
    baglantilar: [
      { yol: "/siparisler", ad: "Talepler" },
      { yol: "/uretim", ad: "Üretim" },
      { yol: "/stok", ad: "Stok" },
      { yol: "/sevkiyat", ad: "Sevkiyat" },
      { yol: "/takvim", ad: "Takvim" },
    ],
  },
  {
    ad: "Firmalar",
    baglantilar: [
      { yol: "/musteriler", ad: "Müşteriler" },
      { yol: "/tedarikciler", ad: "Tedarikçiler" },
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

export function UstMenuCercevesi({ admin = false, sag, ust, children }: { admin?: boolean; sag?: ReactNode; ust?: ReactNode; children?: ReactNode }) {
  const simdiki = usePathname();

  return (
    <div className="duzen">
      <aside className="yan-menu">
        <Link className="yan-logo" href="/" aria-label="MKC Teknoloji">
          <span className="yan-logo-harf">M</span>
          <span className="yan-yazi">MKC Teknoloji</span>
        </Link>
        <nav aria-label="Ana menü">
          {GRUPLAR.filter((g) => !g.admin || admin).map((g, i) => (
            <div key={i} className="yan-grup">
              {g.ad && <div className="yan-grup-ad">{g.ad}</div>}
              {g.baglantilar.map((b) => {
                const aktif = aktifMi(b.yol, simdiki);
                return (
                  <Link key={b.yol} href={b.yol} className={aktif ? "aktif" : undefined} aria-current={aktif ? "page" : undefined} title={b.ad}>
                    <Ikon ad={b.yol} />
                    <span className="yan-yazi">{b.ad}</span>
                  </Link>
                );
              })}
            </div>
          ))}
        </nav>
        {sag}
      </aside>
      <div className="govde">
        {ust}
        {children}
      </div>
    </div>
  );
}

export default function UstMenu({ children }: { children: ReactNode }) {
  const k = useKullanici();
  return (
    <UstMenuCercevesi admin={k.rol === "Admin"} sag={<HesapMenusu />} ust={<UstCubuk ad={`${k.ad} ${k.soyad}`} />}>
      {children}
    </UstMenuCercevesi>
  );
}

function UstCubuk({ ad }: { ad: string }) {
  const tarih = new Intl.DateTimeFormat("tr", { weekday: "long", day: "numeric", month: "long" }).format(new Date());
  const harfler = ad.split(" ").filter(Boolean).map((p) => p[0]).slice(0, 2).join("");
  return (
    <div className="ust-cubuk">
      <Link href="/" className="ust-cubuk-marka">MKC Teknoloji</Link>
      <span className="ust-cubuk-sag">
        <span className="ust-cubuk-tarih" suppressHydrationWarning>{tarih}</span>
        <Link href="/profil" className="ust-cubuk-kisi" title={ad}>
        <span className="ust-cubuk-ad">{ad}</span>
        <span className="ust-cubuk-harf">{harfler}</span>
        </Link>
      </span>
    </div>
  );
}
