"use client";

import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { Fragment, useState } from "react";
import { useSayfalama } from "@/components/Sayfalama";
import { parcalar, stok, type StokGrubu } from "@/lib/api";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import StokGirisleri from "@/components/StokGirisleri";
import MalGirisiFormu from "./MalGirisiFormu";
import UrunStokTablosu from "./UrunStokTablosu";

type Gorunum = "urun" | "parca";

const GORUNUMLER: { deger: Gorunum; ad: string }[] = [
  { deger: "urun", ad: "Ürün bazında" },
  { deger: "parca", ad: "Parça bazında" },
];

export default function StokEkrani() {
  const yol = usePathname();
  const router = useRouter();
  const params = useSearchParams();
  const parcaKod = params.get("parca") ?? "";
  const istenen = params.get("gorunum");
  const gorunum: Gorunum = istenen === "urun" ? "urun" : "parca";

  const [arama, setArama] = useState(params.get("q") ?? "");
  const aranan = useGecikmeli(arama.trim());

  const { veri: gruplar, hata, yenile } = useVeri(
    () => stok.durum({ arama: aranan, parca: parcaKod, }),
    [aranan, parcaKod, gorunum],
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

  const [formAcik, setFormAcik] = useState(params.get("giris") === "1");

  return (
    <main className="icerik icerik--dikey">
      <div className="sayfa-bas">
        <h1>Stok</h1>
      </div>

      {formAcik && (
        <MalGirisiFormu
          parcalar={tumParcalar ?? []}
          kaydedildi={() => {
            yenile();
            parcalariYenile();
          }}
        />
      )}

      <section className="panel">
        <div className="panel-bas">
        <nav className="sekmeler" aria-label="Görünüm">
          {GORUNUMLER.map((g) => (
            <Link
              key={g.deger}
              href={adres({ gorunum: g.deger, bitenler: null })}
              replace
              className={`sekme${gorunum === g.deger ? " sekme--aktif" : ""}`}
              aria-current={gorunum === g.deger ? "page" : undefined}
            >
              {g.ad}
            </Link>
          ))}
        </nav>
        <div className="arama-satiri">
          <input
            type="search"
            className="girdi girdi--ara"
            aria-label="Stokta ara"
            placeholder="Parça, ürün, lot ya da seri no"
            value={arama}
            onChange={(e) => setArama(e.target.value)}
          />
          <button type="button" className="btn btn--kucuk" aria-expanded={formAcik} onClick={() => {
            if (!formAcik && gorunum !== "parca") router.replace(adres({ gorunum: "parca" }));
            setFormAcik(!formAcik);
          }}>
            {formAcik ? "Formu Kapat" : "+ Mal Girişi"}
          </button>
        </div>
        </div>
        {parcaKod && (
          <div className="panel-arac">
            <span className="filtre">
              Yalnızca <span className="mono">{parcaKod}</span> gösteriliyor ·{" "}
              <Link href={adres({ parca: null, gorunum })} replace>tümünü göster</Link>
            </span>
          </div>
        )}
        {hata && <div className="panel-ic"><div className="mesaj mesaj--error">{hata}</div></div>}

        <div className="tablo-kap">
          {gorunum === "urun" ? (
            <UrunStokTablosu aranan={aranan} />
          ) : (
            <ParcaTablosu
              gruplar={gruplar}
              aranan={aranan}
              ilkAcik={parcaKod}
              degisti={() => { yenile(); parcalariYenile(); }}
            />
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
      <td colSpan={sutun}>{aranan ? "Sonuç yok." : "Parça yok."}</td>
    </tr>
  );
}

function ParcaTablosu({ gruplar, aranan, ilkAcik, degisti }: {
  gruplar: StokGrubu[] | null;
  aranan: string;
  ilkAcik: string;
  degisti: () => void;
}) {
  const [acik, setAcik] = useState<string | null>(ilkAcik || null);
  const { gorunen, cubuk } = useSayfalama(gruplar, aranan);
  return (
    <>
      <table className="tablo">
        <thead>
          <tr>
            <th>Kod</th>
            <th>Parça</th>
            <th className="sag">Toplam</th>
            <th className="sag">Kullanılabilir</th>
            <th className="sag">Üretim Sırasında</th>
            <th className="sag">Min stok</th>
          </tr>
        </thead>
        <tbody>
          <BosSatir gruplar={gruplar} aranan={aranan} sutun={6} />
          {gorunen?.map((g) => {
            const { parca: p } = g;
            const acikMi = acik === p.kod;
            return (
              <Fragment key={p.id}>
                <tr className={`satir-tikla${p.minAltinda ? " uyari-satir" : ""}`}>
                  <td className="mono">{p.kod}</td>
                  <td>
                    <button type="button" className="btn-baglanti parca-ac satir-bag" aria-expanded={acikMi} onClick={() => setAcik(acikMi ? null : p.kod)}>
                      <span className="parca-ok" aria-hidden="true">{acikMi ? "▾" : "▸"}</span>
                      {p.ad}
                    </button>{" "}
                    <span className="lot-etiket">· {g.lotlar.length} lot</span>
                  </td>
                  <td className="sag">{p.toplamStok}</td>
                  <td className={`sag${p.minAltinda ? " uyari-metin" : ""}`}>
                    {p.serbest}{p.minAltinda && " · min altında"}
                  </td>
                  <td className={`sag${p.ayrilan ? "" : " soluk"}`}>{p.ayrilan}</td>
                  <td className="sag soluk">{p.minStok}</td>
                </tr>
                {acikMi && (
                  <tr className="acilan-satir">
                    <td colSpan={6}>
                      <StokGirisleri lotlar={g.lotlar} islemler degisti={degisti} />
                    </td>
                  </tr>
                )}
              </Fragment>
            );
          })}
        </tbody>
      </table>
      {cubuk}
    </>
  );
}

