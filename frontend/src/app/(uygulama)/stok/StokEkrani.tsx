"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { parcalar, stok, type StokGrubu } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import MalGirisiFormu from "./MalGirisiFormu";

type Gorunum = "parca" | "lot";

export default function StokEkrani() {
  const router = useRouter();
  const yol = usePathname();
  const params = useSearchParams();
  const parcaKod = params.get("parca") ?? "";
  const gorunum: Gorunum = params.get("gorunum") === "lot" || (parcaKod && params.get("gorunum") !== "parca") ? "lot" : "parca";
  const bitenler = params.get("bitenler") === "1";

  const [arama, setArama] = useState(params.get("q") ?? "");
  const aranan = useGecikmeli(arama.trim());

  const { veri: gruplar, hata, yenile } = useVeri(
    () => stok.durum({ arama: aranan, parca: parcaKod, bitenler: gorunum === "lot" && bitenler }),
    [aranan, parcaKod, gorunum, bitenler],
  );
  const { veri: tumParcalar, yenile: parcalariYenile } = useVeri(() => parcalar.listele(), []);

  function adres(degisen: Record<string, string | null>) {
    const p = new URLSearchParams(params.toString());
    for (const [k, v] of Object.entries(degisen)) {
      if (v) p.set(k, v);
      else p.delete(k);
    }
    const s = p.toString();
    return `${yol}${s ? `?${s}` : ""}`;
  }

  return (
    <main className="icerik">
      <MalGirisiFormu
        parcalar={tumParcalar ?? []}
        kaydedildi={() => {
          yenile();
          parcalariYenile();
        }}
      />

      <section className="kart kart--genis">
        <div className="bas-satir">
          <div className="bas-satir-sol">
            <h2>Stok durumu</h2>
            <nav className="sekmeler" aria-label="Görünüm">
              {(["parca", "lot"] as const).map((g) => (
                <Link
                  key={g}
                  href={adres({ gorunum: g })}
                  replace
                  className={`sekme${gorunum === g ? " sekme--aktif" : ""}`}
                  aria-current={gorunum === g ? "page" : undefined}
                >
                  {g === "parca" ? "Parça bazında" : "Lot bazında"}
                </Link>
              ))}
            </nav>
          </div>
          <div className="arama-satiri">
            {gorunum === "lot" && (
              <label className="onay-etiket">
                <input
                  type="checkbox"
                  checked={bitenler}
                  onChange={(e) => router.replace(adres({ bitenler: e.target.checked ? "1" : null }))}
                />{" "}
                Biten lotlar da
              </label>
            )}
            <input
              type="search"
              className="girdi girdi--ara"
              aria-label="Stokta ara"
              placeholder="Parça veya lot ara…"
              value={arama}
              onChange={(e) => setArama(e.target.value)}
            />
          </div>
        </div>

        {parcaKod && (
          <div className="filtre">
            Yalnızca <span className="mono">{parcaKod}</span> gösteriliyor ·{" "}
            <Link href={adres({ parca: null, gorunum })} replace>tümünü göster</Link>
          </div>
        )}
        {hata && <div className="mesaj mesaj--error">{hata}</div>}

        <div className="tablo-kap">
          {gorunum === "lot" ? (
            <LotTablosu gruplar={gruplar} aranan={aranan} />
          ) : (
            <ParcaTablosu gruplar={gruplar} aranan={aranan} lotAdresi={(kod) => adres({ parca: kod, gorunum: "lot" })} />
          )}
        </div>
      </section>
    </main>
  );
}

function BosSatir({ gruplar, aranan, sutun }: { gruplar: StokGrubu[] | null; aranan: string; sutun: number }) {
  if (gruplar === null) return <tr className="bos"><td colSpan={sutun}>Yükleniyor…</td></tr>;
  if (gruplar.length) return null;
  return (
    <tr className="bos">
      <td colSpan={sutun}>{aranan ? `“${aranan}” için sonuç bulunamadı.` : "Henüz parça tanımlanmadı."}</td>
    </tr>
  );
}

function ParcaTablosu({ gruplar, aranan, lotAdresi }: { gruplar: StokGrubu[] | null; aranan: string; lotAdresi: (kod: string) => string }) {
  return (
    <table className="tablo">
      <thead>
        <tr>
          <th>Kod</th>
          <th>Parça</th>
          <th className="sag">Toplam</th>
          <th className="sag">Kullanılabilir</th>
          <th className="sag">Min stok</th>
          <th className="sag">Lot sayısı</th>
          <th>Sıradaki lot</th>
        </tr>
      </thead>
      <tbody>
        <BosSatir gruplar={gruplar} aranan={aranan} sutun={7} />
        {gruplar?.map(({ parca: p, siradaki }) => (
          <tr key={p.id} className={p.minAltinda ? "uyari-satir" : undefined}>
            <td className="mono">{p.kod}</td>
            <td><Link href={lotAdresi(p.kod)} replace>{p.ad}</Link></td>
            <td className="sag">{p.toplamStok}</td>
            <td className={`sag${p.minAltinda ? " uyari-metin" : ""}`}>
              {p.kullanilabilirStok}{p.minAltinda && " · min altında"}
            </td>
            <td className="sag soluk">{p.minStok}</td>
            <td className="sag">{p.lotSayisi}</td>
            <td className="mono">
              {siradaki ? (
                <>{siradaki.lotNo} <span className="lot-etiket">· {siradaki.kalanAdet} kaldı</span></>
              ) : (
                <span className="soluk">—</span>
              )}
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function LotTablosu({ gruplar, aranan }: { gruplar: StokGrubu[] | null; aranan: string }) {
  return (
    <table className="tablo">
      <thead>
        <tr>
          <th>Parça</th>
          <th>Lot No</th>
          <th>Tedarikçi</th>
          <th>Sipariş tarihi</th>
          <th className="sag">Gelen</th>
          <th className="sag">Kalan</th>
        </tr>
      </thead>
      <tbody>
        <BosSatir gruplar={gruplar} aranan={aranan} sutun={6} />
        {gruplar?.map(({ parca: p, lotlar }) => {
          const parcaHucresi = (
            <td className="parca-hucre" rowSpan={Math.max(lotlar.length, 1)}>
              <div className="parca-ad">{p.ad}</div>
              <div className="mono yardim">{p.kod}</div>
              <div className={`parca-toplam${p.minAltinda ? " parca-toplam--az" : ""}`}>Toplam: {p.toplamStok}</div>
              <div className={`yardim${p.minAltinda ? " uyari-metin" : ""}`}>
                kullanılabilir: {p.kullanilabilirStok}{p.minAltinda && ` · min ${p.minStok} altında`}
              </div>
            </td>
          );
          if (!lotlar.length) {
            return (
              <tr key={p.id}>
                {parcaHucresi}
                <td colSpan={5} className="soluk">{aranan ? "Aramayla eşleşen lot yok." : "Stokta lot yok."}</td>
              </tr>
            );
          }
          return lotlar.map((lot, i) => {
            const izAdresi = `/izlenebilirlik?lot=${encodeURIComponent(lot.lotNo)}`;
            const kalanSinifi = p.minAltinda && !lot.geriCagrildi ? " uyari-metin" : !lot.kalanAdet ? " soluk" : "";
            return (
              <tr key={lot.id} className={lot.geriCagrildi ? "uyari-satir" : undefined}>
                {i === 0 && parcaHucresi}
                <td className="mono">
                  {lot.geriCagrildi ? (
                    <>
                      <Link href={izAdresi} className="uyari-metin">{lot.lotNo}</Link>{" "}
                      <span className="lot-etiket lot-etiket--uyari">· geri çağrıldı</span>
                    </>
                  ) : (
                    <>
                      <Link href={izAdresi} className="lot-baglanti">{lot.lotNo}</Link>
                      {lot.sirada && <> <span className="lot-etiket">· sıradaki</span></>}
                      {!lot.kalanAdet && <> <span className="lot-etiket lot-etiket--soluk">· bitti</span></>}
                    </>
                  )}
                </td>
                <td>{lot.tedarikciAd}</td>
                <td>{tarih(lot.siparisTarihi)}</td>
                <td className="sag">{lot.girisAdet}</td>
                <td className={`sag${kalanSinifi}`}>{lot.kalanAdet}</td>
              </tr>
            );
          });
        })}
      </tbody>
    </table>
  );
}
