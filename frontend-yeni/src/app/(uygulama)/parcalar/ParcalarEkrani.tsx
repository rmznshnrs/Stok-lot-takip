"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Fragment, useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import StokGirisleri from "@/components/StokGirisleri";
import { ApiHatasi, parcalar, stok, type ParcaStok } from "@/lib/api";
import { useSayfalama } from "@/components/Sayfalama";
import { useGecikmeli, useVeri } from "@/lib/kancalar";

const BOS_FORM = { kod: "", ad: "", minStok: "0" };

export default function ParcalarEkrani() {
  const ilkParca = useSearchParams().get("parca") ?? "";
  const [acik, setAcik] = useState<string | null>(ilkParca || null);
  const [arama, setArama] = useState(ilkParca);
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata: listeHatasi, yenile } = useVeri(() => parcalar.listele(aranan), [aranan]);
  const { gorunen, cubuk } = useSayfalama(liste, aranan);

  const [form, setForm] = useState(BOS_FORM);
  const [duzenlenen, setDuzenlenen] = useState<ParcaStok | null>(null);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [yeniId, setYeniId] = useState<number | null>(null);
  const [bekliyor, setBekliyor] = useState(false);
  const [formAcik, setFormAcik] = useState(false);

  function vazgec() {
    setDuzenlenen(null);
    setForm(BOS_FORM);
    setMesaj(null);
    setFormAcik(false);
  }

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setBekliyor(true);
    setMesaj(null);
    const istek = { kod: form.kod.trim(), ad: form.ad.trim(), minStok: Number(form.minStok) || 0 };
    try {
      const p = duzenlenen ? await parcalar.guncelle(duzenlenen.id, istek) : await parcalar.olustur(istek);
      setMesaj({ tur: "success", metin: `${p.kod} kaydedildi.` });
      setYeniId(duzenlenen ? null : p.id);
      setDuzenlenen(null);
      setForm(BOS_FORM);
      setFormAcik(false);
      yenile();
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  const alan = (ad: keyof typeof BOS_FORM) => ({
    value: form[ad],
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [ad]: e.target.value }),
  });

  return (
    <main className="icerik icerik--dikey">
      <div className="sayfa-bas">
        <input
          type="search"
          className="girdi girdi--ara"
          aria-label="Parça ara"
          placeholder="Parça kodu ya da adı"
          value={arama}
          onChange={(e) => setArama(e.target.value)}
        />
        <button
          type="button"
          className="btn"
          aria-expanded={formAcik && !duzenlenen}
          onClick={() => {
            if (formAcik && !duzenlenen) vazgec();
            else {
              setDuzenlenen(null);
              setForm(BOS_FORM);
              setMesaj(null);
              setFormAcik(true);
            }
          }}
        >
          {formAcik && !duzenlenen ? "Formu kapat" : "+ Yeni parça"}
        </button>
      </div>

      {!formAcik && <Mesaj mesaj={mesaj} />}

      {formAcik && (
        <section className="acilir-form">
          <h3>{duzenlenen ? `${duzenlenen.kod} parçasını düzenle` : "Yeni parça"}</h3>
          <form className="form-dikey" onSubmit={kaydet}>
            <Mesaj mesaj={mesaj} />
            <div className="izgara-form izgara-form--4">
              <div className="alan">
                <label htmlFor="kod">Parça kodu</label>
                <input id="kod" className="girdi girdi--mono" required maxLength={50} {...alan("kod")} />
              </div>
              <div className="alan">
                <label htmlFor="ad">Parça adı</label>
                <input id="ad" className="girdi" required maxLength={200} {...alan("ad")} />
              </div>
              <div className="alan">
                <label htmlFor="minStok">Minimum stok</label>
                <input id="minStok" type="number" min={0} className="girdi" {...alan("minStok")} />
              </div>
            </div>
            <div className="dugme-grup">
              <button type="submit" className="btn" disabled={bekliyor}>
                {duzenlenen ? "Değişiklikleri kaydet" : "Parçayı kaydet"}
              </button>
              <button type="button" className="btn btn--ikincil" onClick={vazgec}>Vazgeç</button>
            </div>
          </form>
        </section>
      )}

      <section className="panel">
        {listeHatasi && <div className="panel-ic"><div className="mesaj mesaj--error">{listeHatasi}</div></div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Kod</th>
                <th>Ad</th>
                <th className="sag">Kullanılabilir</th>
                <th className="sag">Üretim Sırasında</th>
                <th className="sag">Hatalı</th>
                <th>Kullanıldığı ürünler</th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !listeHatasi && (
                <tr className="bos"><td colSpan={6}>Yükleniyor…</td></tr>
              )}
              {liste?.length === 0 && (
                <tr className="bos">
                  <td colSpan={6}>{aranan ? "Sonuç yok." : "Parça yok."}</td>
                </tr>
              )}
              {gorunen?.map((p) => (
                <Fragment key={p.id}>
                  <ParcaSatiri
                    p={p}
                    yeni={p.id === yeniId}
                    acik={acik === p.kod}
                    ac={() => setAcik(acik === p.kod ? null : p.kod)}
                  />
                  {acik === p.kod && (
                    <tr className="acilan-satir">
                      <td colSpan={6}>
                        <ParcaDuzenle
                          p={p}
                          kaydedildi={(metin) => {
                            setMesaj({ tur: "success", metin });
                            yenile();
                          }}
                        />
                        <ParcaGirisleri
                          kod={p.kod}
                          degisti={(metin) => {
                            setMesaj({ tur: "success", metin });
                            yenile();
                          }}
                        />
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
        {cubuk}
      </section>
    </main>
  );
}

function ParcaGirisleri({ kod, degisti }: { kod: string; degisti: (mesaj: string) => void }) {
  const { veri, hata, yenile } = useVeri(() => stok.durum({ parca: kod, bitenler: true }), [kod]);
  const [bitenlerAcik, setBitenlerAcik] = useState(false);
  const lotlar = veri?.[0]?.lotlar;
  if (hata) return <div className="mesaj mesaj--error">{hata}</div>;
  if (!lotlar) return <p className="yukleniyor">Yükleniyor…</p>;
  const elde = lotlar.filter((l) => l.kalanAdet > 0);
  const bitenler = lotlar.filter((l) => l.kalanAdet === 0);
  const guncellendi = (m: string) => { yenile(); degisti(m); };
  return (
    <div className="form-dikey">
      <StokGirisleri lotlar={elde} islemler degisti={guncellendi} />
      {bitenler.length > 0 && (
        <>
          <button type="button" className="btn btn--ikincil btn--kucuk biten-lot-dugmesi" aria-expanded={bitenlerAcik}
            onClick={() => setBitenlerAcik(!bitenlerAcik)}>
            {bitenlerAcik ? "▾" : "▸"} Biten Lotlar ({bitenler.length})
          </button>
          {bitenlerAcik && <StokGirisleri lotlar={bitenler} />}
        </>
      )}
    </div>
  );
}

function ParcaDuzenle({ p, kaydedildi }: { p: ParcaStok; kaydedildi: (mesaj: string) => void }) {
  const [form, setForm] = useState({ kod: p.kod, ad: p.ad, minStok: String(p.minStok) });
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const degisti = form.kod !== p.kod || form.ad !== p.ad || form.minStok !== String(p.minStok);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      const k = await parcalar.guncelle(p.id, { kod: form.kod.trim(), ad: form.ad.trim(), minStok: Number(form.minStok) || 0 });
      kaydedildi(`${k.kod} kaydedildi.`);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <form className="form-dikey parca-duzenle" onSubmit={kaydet}>
      <div className="izgara-form izgara-form--4">
        <div className="alan">
          <label htmlFor={`kod-${p.id}`}>Parça Kodu</label>
          <input id={`kod-${p.id}`} className="girdi girdi--mono" required maxLength={50} value={form.kod}
            onChange={(e) => setForm({ ...form, kod: e.target.value })} />
        </div>
        <div className="alan">
          <label htmlFor={`ad-${p.id}`}>Parça Adı</label>
          <input id={`ad-${p.id}`} className="girdi" required maxLength={200} value={form.ad}
            onChange={(e) => setForm({ ...form, ad: e.target.value })} />
        </div>
        <div className="alan">
          <label htmlFor={`min-${p.id}`}>Minimum Stok</label>
          <input id={`min-${p.id}`} type="number" min={0} className="girdi" value={form.minStok}
            onChange={(e) => setForm({ ...form, minStok: e.target.value })} />
        </div>
        <div className="alan alan--dugme">
          <button type="submit" className="btn" disabled={bekliyor || !degisti}>Değişiklikleri Kaydet</button>
        </div>
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
    </form>
  );
}

function ParcaSatiri({ p, yeni, acik, ac }: {
  p: ParcaStok; yeni: boolean; acik: boolean; ac: () => void;
}) {
  const ipucu = `Kullanılabilir ${p.serbest}${p.ayrilan ? ` (${p.ayrilan} üretim sırasında)` : ""} · toplam ${p.toplamStok} · minimum ${p.minStok}`;
  const stokAdresi = `/stok?parca=${encodeURIComponent(p.kod)}`;
  return (
    <tr className={`satir-tikla${p.minAltinda ? " uyari-satir" : ""}`}>
      <td className="mono">{p.kod}</td>
      <td>
        <button type="button" className="btn-baglanti parca-ac satir-bag" aria-expanded={acik} onClick={ac}>
          <span className="parca-ok" aria-hidden="true">{acik ? "▾" : "▸"}</span>
          {p.ad}
        </button>{" "}
        {yeni && <span className="rozet rozet--kucuk rozet--mavi">yeni</span>}
      </td>
      {p.minAltinda ? (
        <td className="sag uyari-metin" title={ipucu}>
          <Link className="uyari-metin" href={stokAdresi}>{p.serbest}</Link> · min altında
        </td>
      ) : p.toplamStok ? (
        <td className="sag" title={ipucu}><Link href={stokAdresi}>{p.serbest}</Link></td>
      ) : (
        <td className="sag soluk">0</td>
      )}
      <td className={`sag${p.ayrilan ? "" : " soluk"}`}>{p.ayrilan}</td>
      <td className={`sag${p.hataliAdet ? " uyari-metin" : " soluk"}`}>{p.hataliAdet}</td>
      {p.kullanildigiUrunler.length ? (
        <td>{p.kullanildigiUrunler.join(", ")}</td>
      ) : (
        <td className="soluk">Henüz hiçbir ürün ağacında yok</td>
      )}
    </tr>
  );
}
