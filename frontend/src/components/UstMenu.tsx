"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cikisYap, useKullanici } from "./Oturum";

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

export default function UstMenu() {
  const simdiki = usePathname();
  const k = useKullanici();
  const baglantilar = k.rol === "Admin" ? [...BAGLANTILAR, { yol: "/yonetim", ad: "Yönetim" }] : BAGLANTILAR;

  return (
    <header className="ust">
      <Link className="logo" href="/">LOT TAKİP</Link>
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
      <div className="kullanici-alan">
        <span className="kullanici-ad">{k.ad} {k.soyad}</span>
        <button type="button" className="btn-baglanti" onClick={cikisYap}>Çıkış</button>
      </div>
    </header>
  );
}
