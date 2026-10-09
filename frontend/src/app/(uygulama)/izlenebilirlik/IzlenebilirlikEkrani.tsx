"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiHatasi, izleme } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";
import LotIzi from "./LotIzi";
import SeriIzi from "./SeriIzi";

type Sekme = "lot" | "seri";

/** 404 → null (bulunamadı); diğer hatalar olduğu gibi. */
async function bulunamazsaNull<T>(p: Promise<T>): Promise<T | null> {
  try {
    return await p;
  } catch (e) {
    if (e instanceof ApiHatasi && e.durum === 404) return null;
    throw e;
  }
}

export default function IzlenebilirlikEkrani() {
  const router = useRouter();
  const params = useSearchParams();
  const lotNo = params.get("lot")?.trim() ?? "";
  const seriNo = params.get("seri")?.trim() ?? "";
  const sekme: Sekme = params.get("sekme") === "seri" || (params.get("sekme") !== "lot" && seriNo && !lotNo) ? "seri" : "lot";
  const aranan = sekme === "lot" ? lotNo : seriNo;

  const [girdi, setGirdi] = useState(aranan);
  const [onceki, setOnceki] = useState(aranan);
  if (onceki !== aranan) {
    // Adres değişince (bağlantıya tıklanınca) arama kutusu da güncellensin
    setOnceki(aranan);
    setGirdi(aranan);
  }

  const { veri, hata, yenile } = useVeri(async () => {
    if (!aranan) return { tur: sekme, iz: null };
    return sekme === "lot"
      ? { tur: "lot" as const, iz: await bulunamazsaNull(izleme.lot(aranan)) }
      : { tur: "seri" as const, iz: await bulunamazsaNull(izleme.seri(aranan)) };
  }, [sekme, aranan]);

  function ara(e: FormEvent) {
    e.preventDefault();
    const deger = girdi.trim();
    router.replace(deger ? `/izlenebilirlik?${sekme}=${encodeURIComponent(deger)}` : `/izlenebilirlik?sekme=${sekme}`);
  }

  const lotIz = veri?.tur === "lot" && sekme === "lot" ? veri.iz : null;
  const seriIz = veri?.tur === "seri" && sekme === "seri" ? veri.iz : null;
  const yuklendi = veri !== null && veri.tur === sekme;

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir iz-ust">
        <nav className="sekmeler sekmeler--buyuk" aria-label="İzleme yönü">
          <Link
            href={lotNo ? `/izlenebilirlik?lot=${encodeURIComponent(lotNo)}` : "/izlenebilirlik?sekme=lot"}
            replace
            className={`sekme${sekme === "lot" ? " sekme--aktif" : ""}`}
            aria-current={sekme === "lot" ? "page" : undefined}
          >
            Lot → Ürün → Müşteri
          </Link>
          <Link
            href={seriNo ? `/izlenebilirlik?sekme=seri&seri=${encodeURIComponent(seriNo)}` : "/izlenebilirlik?sekme=seri"}
            replace
            className={`sekme${sekme === "seri" ? " sekme--aktif" : ""}`}
            aria-current={sekme === "seri" ? "page" : undefined}
          >
            Seri No → Parçalar
          </Link>
        </nav>
        {lotIz && (
          <div className="ozet">
            {lotIz.uretimler.length} üründe kullanıldı ·{" "}
            <strong className={lotIz.musteriler.length ? "uyari-metin" : undefined}>
              {lotIz.musteriler.length} müşteriye ulaştı
            </strong>{" "}
            · {lotIz.stoktakiUretimler.length} ürün depoda
          </div>
        )}
        <form className="arama-satiri" role="search" onSubmit={ara}>
          <input
            type="search"
            className="girdi girdi--ara girdi--mono"
            aria-label={sekme === "lot" ? "Lot no" : "Seri no"}
            placeholder={sekme === "lot" ? "Lot no girin…" : "Seri no girin…"}
            value={girdi}
            onChange={(e) => setGirdi(e.target.value)}
          />
        </form>
      </div>

      {hata && <div className="mesaj mesaj--error">{hata}</div>}

      {!aranan ? (
        <section className="kart kart--genis">
          <p className="not not--buyuk">
            {sekme === "lot"
              ? "Bir lot numarası girin: lottaki parçanın hangi ürünlerde (seri no) kullanıldığı ve o ürünlerin kime satıldığı gösterilir."
              : "Bir seri numarası girin: o ürünün içindeki parçalar, lotları ve adetleri gösterilir."}
          </p>
        </section>
      ) : !yuklendi ? (
        !hata && <p className="yukleniyor">Yükleniyor…</p>
      ) : sekme === "lot" ? (
        lotIz ? (
          <LotIzi iz={lotIz} degisti={yenile} />
        ) : (
          <section className="kart kart--genis">
            <p className="not not--buyuk">
              <span className="mono">{aranan}</span> lot numarası bulunamadı.{" "}
              <Link href={`/stok?gorunum=lot&q=${encodeURIComponent(aranan)}`}>Stokta ara</Link>
            </p>
          </section>
        )
      ) : seriIz ? (
        <SeriIzi iz={seriIz} />
      ) : (
        <section className="kart kart--genis">
          <p className="not not--buyuk"><span className="mono">{aranan}</span> seri numarası bulunamadı.</p>
        </section>
      )}
    </main>
  );
}
