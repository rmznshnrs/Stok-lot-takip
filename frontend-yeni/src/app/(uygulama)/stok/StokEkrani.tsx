"use client";

import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";
import { Fragment, useState } from "react";
import HataliLotFormu from "@/components/HataliLotFormu";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useSayfalama } from "@/components/Sayfalama";
import { useAdminMi } from "@/components/Oturum";
import { lotAdresi } from "@/lib/adres";
import { parcalar, stok, type Lot, type StokGrubu } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { AdetDuzeltFormu } from "./LotIslemFormlari";
import MalGirisiFormu from "./MalGirisiFormu";
import UrunStokTablosu from "./UrunStokTablosu";

type Gorunum = "urun" | "parca" | "biten";

const GORUNUMLER: { deger: Gorunum; ad: string }[] = [
  { deger: "urun", ad: "Ürün bazında" },
  { deger: "parca", ad: "Parça bazında" },
  { deger: "biten", ad: "Tükenen parça stokları" },
];

export default function StokEkrani() {
  const yol = usePathname();
  const params = useSearchParams();
  const parcaKod = params.get("parca") ?? "";
  const istenen = params.get("gorunum");
  // Eski "lot" (giriş bazında) adresleri parça bazında açılır
  const gorunum: Gorunum = istenen === "urun" ? "urun" : istenen === "biten" ? "biten" : "parca";

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
            <UrunStokTablosu aranan={aranan} bildir={(metin) => setMesaj({ tur: "success", metin })} />
          ) : gorunum === "biten" ? (
            <TukenenTablosu gruplar={gruplar} aranan={aranan} />
          ) : (
            <ParcaTablosu
              gruplar={gruplar}
              aranan={aranan}
              ilkAcik={parcaKod}
              degisti={(metin) => {
                setMesaj({ tur: "success", metin });
                yenile();
                parcalariYenile();
              }}
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

/** Parçalar; satıra basınca o parçanın stok girişleri (FIFO sırasıyla) ve işlemleri altta açılır. */
function ParcaTablosu({ gruplar, aranan, ilkAcik, degisti }: {
  gruplar: StokGrubu[] | null;
  aranan: string;
  ilkAcik: string;
  degisti: (mesaj: string) => void;
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
            <th className="sag">Min stok</th>
            <th className="sag">Hatalı</th>
            <th>Sıradaki giriş</th>
          </tr>
        </thead>
        <tbody>
          <BosSatir gruplar={gruplar} aranan={aranan} sutun={7} />
          {gorunen?.map((g) => {
            const { parca: p, siradaki } = g;
            const acikMi = acik === p.kod;
            return (
              <Fragment key={p.id}>
                <tr className={p.minAltinda ? "uyari-satir" : undefined}>
                  <td className="mono">{p.kod}</td>
                  <td>
                    <button type="button" className="btn-baglanti parca-ac" aria-expanded={acikMi} onClick={() => setAcik(acikMi ? null : p.kod)}>
                      <span className="parca-ok" aria-hidden="true">{acikMi ? "▾" : "▸"}</span>
                      {p.ad}
                    </button>{" "}
                    <span className="lot-etiket">· {g.lotlar.length} giriş</span>
                  </td>
                  <td className="sag">{p.toplamStok}</td>
                  <td className={`sag${p.minAltinda ? " uyari-metin" : ""}`}>
                    {p.kullanilabilirStok}{p.minAltinda && " · min altında"}
                  </td>
                  <td className="sag soluk">{p.minStok}</td>
                  <td className={`sag${p.hataliAdet ? " uyari-metin" : " soluk"}`}>{p.hataliAdet}</td>
                  <td className="mono">
                    {siradaki ? (
                      <>{siradaki.lotNo} <span className="lot-etiket">· {siradaki.kalanAdet} kaldı</span></>
                    ) : (
                      <span className="soluk">—</span>
                    )}
                  </td>
                </tr>
                {acikMi && (
                  <tr className="acilan-satir">
                    <td colSpan={7}>
                      <div className="giris-bas">{p.ad} · stok girişleri ({g.lotlar.length})</div>
                      <GirisTablosu lotlar={g.lotlar} minAltinda={p.minAltinda} degisti={degisti} />
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
          <th>Stok giriş no</th>
          <th>Tedarikçi lot no</th>
          <th>Tedarikçi</th>
          <th>Sipariş tarihi</th>
          <th className="sag">Gelen</th>
        </tr>
      </thead>
      <tbody>
        {gruplar === null && <tr className="bos"><td colSpan={6}>Yükleniyor…</td></tr>}
        {gruplar !== null && lotlar.length === 0 && (
          <tr className="bos"><td colSpan={6}>{aranan ? "Sonuç yok." : "Tükenen parça stoğu yok."}</td></tr>
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
            <td className="mono soluk">{lot.tedarikciLotNo || "—"}</td>
            <td>{lot.tedarikciAd}</td>
            <td>{tarih(lot.siparisTarihi)}</td>
            <td className="sag">{lot.girisAdet}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}

type LotIslemi = "hata" | "duzelt";

/** Bir parçanın stok girişleri ve yönetici işlemleri (hatalı işaretle, stoktan düş). */
function GirisTablosu({ lotlar, minAltinda, degisti }: { lotlar: Lot[]; minAltinda: boolean; degisti: (mesaj: string) => void }) {
  const admin = useAdminMi();
  const [acik, setAcik] = useState<{ lotId: number; islem: LotIslemi } | null>(null);
  const sutun = admin ? 8 : 7;
  const ac = (lotId: number, islem: LotIslemi) =>
    setAcik(acik?.lotId === lotId && acik.islem === islem ? null : { lotId, islem });
  if (!lotlar.length) return <p className="not">Stok yok.</p>;
  return (
    <table className="tablo tablo--sik tablo--girisler">
      <thead>
        <tr>
          <th>Stok giriş no</th>
          <th>Tedarikçi lot no</th>
          <th>Tedarikçi</th>
          <th>Sipariş tarihi</th>
          <th className="sag">Gelen</th>
          <th className="sag">Kalan</th>
          <th>Durum</th>
          {admin && <th><span className="gorunmez">İşlem</span></th>}
        </tr>
      </thead>
      <tbody>
        {lotlar.map((lot) => {
          const durum = lot.sorunlu
            ? `Hatalı${lot.geriCagrildi ? ", üretime kapalı" : ""}`
            : !lot.kalanAdet ? "Bitti" : lot.sirada ? "Sıradaki" : "";
          return (
            <Fragment key={lot.id}>
              <tr className={lot.sorunlu ? "uyari-satir" : undefined}>
                <td className="mono">
                  <Link href={lotAdresi(lot.lotNo)} className={`giris-no${lot.sorunlu ? " uyari-metin" : ""}`}>{lot.lotNo}</Link>
                </td>
                <td className="mono soluk">{lot.tedarikciLotNo || "—"}</td>
                <td>{lot.tedarikciAd}</td>
                <td>{tarih(lot.siparisTarihi)}</td>
                <td className="sag">{lot.girisAdet}</td>
                <td className={`sag${!lot.kalanAdet ? " soluk" : ""}`}>{lot.kalanAdet}</td>
                <td className={lot.sorunlu ? "uyari-metin" : "soluk"}>
                  {durum}
                  {minAltinda && lot.sirada && !lot.sorunlu && <span className="uyari-metin"> · min altında</span>}
                </td>
                {admin && (
                  <td className="hucre-dar sag">
                    <span className="dugme-grup dugme-grup--sag">
                      {([
                        ["hata", lot.sorunlu ? "Hatayı düzenle" : "Hatalı işaretle"],
                        ["duzelt", "Stoktan düş"],
                      ] as const).map(([islem, ad]) => (
                        <button
                          key={islem}
                          type="button"
                          className="btn-baglanti"
                          aria-expanded={acik?.lotId === lot.id && acik.islem === islem}
                          onClick={() => ac(lot.id, islem)}
                        >
                          {ad}
                        </button>
                      ))}
                    </span>
                  </td>
                )}
              </tr>
              {acik?.lotId === lot.id && (
                <tr>
                  <td colSpan={sutun}>
                    {(() => {
                      const ozellik = {
                        lot,
                        kapat: () => setAcik(null),
                        kaydedildi: (metin: string) => {
                          setAcik(null);
                          degisti(metin);
                        },
                      };
                      return acik.islem === "hata" ? <HataliLotFormu {...ozellik} />
                        : <AdetDuzeltFormu {...ozellik} />;
                    })()}
                  </td>
                </tr>
              )}
            </Fragment>
          );
        })}
      </tbody>
    </table>
  );
}
