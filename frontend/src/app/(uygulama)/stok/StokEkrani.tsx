"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { Fragment, useState } from "react";
import HataliLotFormu from "@/components/HataliLotFormu";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useAdminMi } from "@/components/Oturum";
import { lotAdresi } from "@/lib/adres";
import { parcalar, stok, type StokGrubu } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import MalGirisiFormu from "./MalGirisiFormu";

type Gorunum = "parca" | "lot" | "biten";

const GORUNUMLER: { deger: Gorunum; ad: string }[] = [
  { deger: "parca", ad: "Parça bazında" },
  { deger: "lot", ad: "Lot bazında" },
  { deger: "biten", ad: "Tükenen lotlar" },
];

export default function StokEkrani() {
  const yol = usePathname();
  const params = useSearchParams();
  const parcaKod = params.get("parca") ?? "";
  const istenen = params.get("gorunum");
  const gorunum: Gorunum =
    istenen === "biten" ? "biten" : istenen === "lot" || (parcaKod && istenen !== "parca") ? "lot" : "parca";

  const [arama, setArama] = useState(params.get("q") ?? "");
  const aranan = useGecikmeli(arama.trim());

  const { veri: gruplar, hata, yenile } = useVeri(
    () => stok.durum({ arama: aranan, parca: parcaKod, bitenler: gorunum === "biten" }),
    [aranan, parcaKod, gorunum],
  );
  const { veri: tumParcalar, yenile: parcalariYenile } = useVeri(() => parcalar.listele(), []);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);

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
          </div>
          <div className="arama-satiri">
            <input
              type="search"
              className="girdi girdi--ara"
              aria-label="Stokta ara"
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
        <Mesaj mesaj={mesaj} />

        <div className="tablo-kap">
          {gorunum === "biten" ? (
            <TukenenTablosu gruplar={gruplar} aranan={aranan} />
          ) : gorunum === "lot" ? (
            <LotTablosu
              gruplar={gruplar}
              aranan={aranan}
              isaretlendi={(metin) => {
                setMesaj({ tur: "success", metin });
                yenile();
                parcalariYenile();
              }}
            />
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

/** Kalanı 0'a düşmüş lotlar (son gelen üstte). Lota tıklayınca kime gittiği açılır. */
function TukenenTablosu({ gruplar, aranan }: { gruplar: StokGrubu[] | null; aranan: string }) {
  const lotlar = (gruplar ?? [])
    .flatMap(({ parca, lotlar }) => lotlar.filter((l) => l.kalanAdet === 0).map((l) => ({ parca, lot: l })))
    .sort((a, b) => b.lot.siparisTarihi.localeCompare(a.lot.siparisTarihi));
  return (
    <table className="tablo">
      <thead>
        <tr>
          <th>Parça</th>
          <th>Lot No</th>
          <th>Tedarikçi</th>
          <th>Sipariş tarihi</th>
          <th className="sag">Gelen</th>
        </tr>
      </thead>
      <tbody>
        {gruplar === null && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
        {gruplar !== null && lotlar.length === 0 && (
          <tr className="bos"><td colSpan={5}>{aranan ? `“${aranan}” için tükenen lot yok.` : "Tükenen lot yok."}</td></tr>
        )}
        {lotlar.map(({ parca: p, lot }) => (
          <tr key={lot.id} className={lot.sorunlu ? "uyari-satir" : undefined}>
            <td>{p.ad} <span className="mono yardim">{p.kod}</span></td>
            <td className="mono">
              <Link href={lotAdresi(lot.lotNo)} className={lot.sorunlu ? "uyari-metin" : "lot-baglanti"}>
                {lot.lotNo}
              </Link>
              {lot.sorunlu && <> <span className="lot-etiket lot-etiket--uyari">· hatalı</span></>}
            </td>
            <td>{lot.tedarikciAd}</td>
            <td>{tarih(lot.siparisTarihi)}</td>
            <td className="sag">{lot.girisAdet}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function LotTablosu({ gruplar, aranan, isaretlendi }: {
  gruplar: StokGrubu[] | null;
  aranan: string;
  isaretlendi: (mesaj: string) => void;
}) {
  const admin = useAdminMi();
  const [acikLot, setAcikLot] = useState<number | null>(null);
  const sutun = admin ? 7 : 6;
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
          {admin && <th><span className="gorunmez">Hatalı lot</span></th>}
        </tr>
      </thead>
      <tbody>
        <BosSatir gruplar={gruplar} aranan={aranan} sutun={sutun} />
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
                <td colSpan={sutun - 1} className="soluk">{aranan ? "Aramayla eşleşen lot yok." : "Stokta lot yok."}</td>
              </tr>
            );
          }
          return lotlar.map((lot, i) => {
            const izAdresi = lotAdresi(lot.lotNo);
            const kalanSinifi = p.minAltinda && !lot.sorunlu ? " uyari-metin" : !lot.kalanAdet ? " soluk" : "";
            return (
              <Fragment key={lot.id}>
              <tr className={lot.sorunlu ? "uyari-satir" : undefined}>
                {i === 0 && parcaHucresi}
                <td className="mono">
                  {lot.sorunlu ? (
                    <>
                      <Link href={izAdresi} className="uyari-metin">{lot.lotNo}</Link>{" "}
                      <span className="lot-etiket lot-etiket--uyari">· hatalı{lot.geriCagrildi ? ", üretime kapalı" : ""}</span>
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
                {admin && (
                  <td className="hucre-dar">
                    {!lot.sorunlu && (
                      <button
                        type="button"
                        className="btn btn--ikincil btn--kucuk"
                        aria-expanded={acikLot === lot.id}
                        onClick={() => setAcikLot(acikLot === lot.id ? null : lot.id)}
                      >
                        Hatalı işaretle
                      </button>
                    )}
                  </td>
                )}
              </tr>
              {acikLot === lot.id && (
                <tr>
                  <td colSpan={sutun - (i === 0 ? 1 : 0)}>
                    <HataliLotFormu
                      lot={lot}
                      kapat={() => setAcikLot(null)}
                      kaydedildi={(metin) => {
                        setAcikLot(null);
                        isaretlendi(metin);
                      }}
                    />
                  </td>
                </tr>
              )}
              </Fragment>
            );
          });
        })}
      </tbody>
    </table>
  );
}
