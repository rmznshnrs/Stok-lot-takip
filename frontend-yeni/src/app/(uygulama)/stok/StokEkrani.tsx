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
import UrunStokTablosu from "./UrunStokTablosu";

type Gorunum = "urun" | "parca" | "lot" | "biten";

const GORUNUMLER: { deger: Gorunum; ad: string }[] = [
  { deger: "urun", ad: "Ürün bazında" },
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
    istenen === "urun" ? "urun" : istenen === "biten" ? "biten" : istenen === "lot" || (parcaKod && istenen !== "parca") ? "lot" : "parca";

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

  const [formAcik, setFormAcik] = useState(params.get("giris") === "1");

  return (
    <main className="icerik icerik--dikey">
      <div className="sayfa-bas">
        <h1>Stok</h1>
        <button type="button" className="btn" aria-expanded={formAcik} onClick={() => setFormAcik(!formAcik)}>
          {formAcik ? "Formu kapat" : "+ Mal girişi"}
        </button>
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
        <div className="panel-arac">
          <input
            type="search"
            className="girdi girdi--ara"
            aria-label="Stokta ara"
            value={arama}
            onChange={(e) => setArama(e.target.value)}
          />
          {parcaKod && (
            <span className="filtre">
              Yalnızca <span className="mono">{parcaKod}</span> gösteriliyor ·{" "}
              <Link href={adres({ parca: null, gorunum })} replace>tümünü göster</Link>
            </span>
          )}
        </div>
        {(hata || mesaj) && (
          <div className="panel-ic">
            {hata && <div className="mesaj mesaj--error">{hata}</div>}
            <Mesaj mesaj={mesaj} />
          </div>
        )}

        <div className="tablo-kap">
          {gorunum === "urun" ? (
            <UrunStokTablosu aranan={aranan} />
          ) : gorunum === "biten" ? (
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
  const sutun = admin ? 8 : 7;
  return (
    <table className="tablo">
      <thead>
        <tr>
          <th>Parça</th>
          <th>Lot no</th>
          <th>Tedarikçi</th>
          <th>Sipariş tarihi</th>
          <th className="sag">Gelen</th>
          <th className="sag">Kalan</th>
          <th>Durum</th>
          {admin && <th><span className="gorunmez">Hatalı lot</span></th>}
        </tr>
      </thead>
      <tbody>
        <BosSatir gruplar={gruplar} aranan={aranan} sutun={sutun} />
        {gruplar?.map(({ parca: p, lotlar }) => {
          const parcaHucresi = (
            <td>
              {p.ad} <span className="mono yardim">{p.kod}</span>
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
            const durum = lot.sorunlu
              ? `Hatalı${lot.geriCagrildi ? ", üretime kapalı" : ""}`
              : !lot.kalanAdet
                ? "Bitti"
                : lot.sirada
                  ? "Sıradaki"
                  : "";
            return (
              <Fragment key={lot.id}>
                <tr className={lot.sorunlu ? "uyari-satir" : undefined}>
                  {i === 0 ? parcaHucresi : <td />}
                  <td className="mono">
                    <Link href={lotAdresi(lot.lotNo)} className={lot.sorunlu ? "uyari-metin" : undefined}>{lot.lotNo}</Link>
                  </td>
                  <td>{lot.tedarikciAd}</td>
                  <td>{tarih(lot.siparisTarihi)}</td>
                  <td className="sag">{lot.girisAdet}</td>
                  <td className={`sag${!lot.kalanAdet ? " soluk" : ""}`}>{lot.kalanAdet}</td>
                  <td className={lot.sorunlu ? "uyari-metin" : "soluk"}>
                    {durum}
                    {p.minAltinda && lot.sirada && !lot.sorunlu && <span className="uyari-metin"> · min altında</span>}
                  </td>
                  {admin && (
                    <td className="hucre-dar sag">
                      {!lot.sorunlu && (
                        <button
                          type="button"
                          className="btn-baglanti"
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
                    <td colSpan={sutun}>
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
