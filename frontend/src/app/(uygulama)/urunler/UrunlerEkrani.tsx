"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { parcalar, urunler } from "@/lib/api";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import UrunFormu from "./UrunFormu";

export default function UrunlerEkrani() {
  const router = useRouter();
  const params = useSearchParams();
  const yeni = params.get("yeni") === "1";
  const seciliId = Number(params.get("urun")) || null;

  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata, yenile } = useVeri(() => urunler.listele(aranan), [aranan]);
  const { veri: tumParcalar } = useVeri(() => parcalar.listele(), []);
  const [basari, setBasari] = useState<{ id: number; metin: string } | null>(null);
  const { veri: detay, hata: detayHatasi, yenile: detayYenile } = useVeri(
    () => (seciliId ? urunler.getir(seciliId) : Promise.resolve(null)),
    [seciliId],
  );

  // Seçim yoksa ilk ürünü aç; hiç ürün yoksa yeni ürün formu
  useEffect(() => {
    if (yeni || seciliId || !liste || aranan) return;
    router.replace(liste.length ? `/urunler?urun=${liste[0].id}` : "/urunler?yeni=1");
  }, [yeni, seciliId, liste, aranan, router]);

  function kaydedildi(id: number, metin: string) {
    setBasari({ id, metin });
    yenile();
    if (id === seciliId) detayYenile();
    else router.replace(`/urunler?urun=${id}`);
  }

  return (
    <main className="icerik">
      <section className="kart kart--liste">
        <div className="bas-satir bas-satir--liste">
          <h2>Ürünler</h2>
          <Link href="/urunler?yeni=1" className="btn btn--kucuk">+ Yeni ürün</Link>
        </div>
        <input
          type="search"
          className="girdi girdi--liste-ara"
          aria-label="Ürün ara"
          placeholder="Ürün ara…"
          value={arama}
          onChange={(e) => setArama(e.target.value)}
        />
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        {liste?.map((u) => {
          const secili = !yeni && u.id === seciliId;
          return (
            <Link
              key={u.id}
              href={`/urunler?urun=${u.id}`}
              className={`liste-oge${secili ? " liste-oge--secili" : ""}`}
              aria-current={secili ? "page" : undefined}
            >
              <span className="liste-oge-ad">{u.ad}</span>
              <span className="liste-oge-alt">
                Kod: <span className="mono">{u.kod}</span>
                {secili && <> · seri öneki <span className="mono">{u.seriOneki}</span></>}
              </span>
            </Link>
          );
        })}
        {liste?.length === 0 && (
          <p className="not">{aranan ? `“${aranan}” için ürün bulunamadı.` : "Henüz ürün yok."}</p>
        )}
      </section>

      <section className="kart kart--genis kart--bosluklu">
        {detayHatasi && !yeni && <div className="mesaj mesaj--error">{detayHatasi}</div>}
        {!yeni && basari?.id === seciliId && <div className="mesaj mesaj--success" role="status">{basari.metin}</div>}
        {yeni ? (
          <UrunFormu key="yeni" urun={null} parcalar={tumParcalar ?? []} kaydedildi={kaydedildi} />
        ) : detay && detay.id === seciliId ? (
          <UrunFormu key={JSON.stringify(detay)} urun={detay} parcalar={tumParcalar ?? []} kaydedildi={kaydedildi} />
        ) : (
          !detayHatasi && <p className="yukleniyor">Yükleniyor…</p>
        )}
      </section>
    </main>
  );
}
