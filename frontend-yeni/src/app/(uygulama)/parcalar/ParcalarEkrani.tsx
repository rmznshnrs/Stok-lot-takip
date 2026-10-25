"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useAdminMi } from "@/components/Oturum";
import { ApiHatasi, parcalar, type ParcaStok } from "@/lib/api";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import ParcaAdetDuzeltme from "./ParcaAdetDuzeltme";

const BOS_FORM = { kod: "", ad: "", birim: "adet", minStok: "0" };

export default function ParcalarEkrani() {
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata: listeHatasi, yenile } = useVeri(() => parcalar.listele(aranan), [aranan]);

  const [form, setForm] = useState(BOS_FORM);
  const [duzenlenen, setDuzenlenen] = useState<ParcaStok | null>(null);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [yeniId, setYeniId] = useState<number | null>(null);
  const [bekliyor, setBekliyor] = useState(false);
  const admin = useAdminMi();
  const [duzeltilen, setDuzeltilen] = useState<ParcaStok | null>(null);
  const [formAcik, setFormAcik] = useState(false);

  function duzenle(p: ParcaStok) {
    setDuzeltilen(null);
    setDuzenlenen(p);
    setForm({ kod: p.kod, ad: p.ad, birim: p.birim, minStok: String(p.minStok) });
    setMesaj(null);
    setFormAcik(true);
  }

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
    const istek = { kod: form.kod.trim(), ad: form.ad.trim(), birim: form.birim.trim() || "adet", minStok: Number(form.minStok) || 0 };
    try {
      const p = duzenlenen ? await parcalar.guncelle(duzenlenen.id, istek) : await parcalar.olustur(istek);
      setMesaj({ tur: "success", metin: `${p.kod} – ${p.ad} ${duzenlenen ? "güncellendi" : "kaydedildi"}.` });
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
          value={arama}
          onChange={(e) => setArama(e.target.value)}
        />
        <button
          type="button"
          className="btn"
          aria-expanded={formAcik && !duzenlenen}
          onClick={() => {
            setDuzeltilen(null);
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

      {!formAcik && !duzeltilen && <Mesaj mesaj={mesaj} />}

      {duzeltilen && (
        <section className="acilir-form">
          <ParcaAdetDuzeltme key={duzeltilen.id} parca={duzeltilen} kapat={() => setDuzeltilen(null)} kaydedildi={yenile} />
        </section>
      )}

      {formAcik && !duzeltilen && (
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
                <label htmlFor="birim">Birim</label>
                <input id="birim" className="girdi" maxLength={20} {...alan("birim")} />
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
                <th>Birim</th>
                <th className="sag">Kullanılabilir stok</th>
                <th className="sag" title="Kalanı olan lot sayısı">Lot sayısı</th>
                <th>Kullanıldığı ürünler</th>
                <th className="sutun-ikon"><span className="gorunmez">İşlem</span></th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !listeHatasi && (
                <tr className="bos"><td colSpan={7}>Yükleniyor…</td></tr>
              )}
              {liste?.length === 0 && (
                <tr className="bos">
                  <td colSpan={7}>{aranan ? `“${aranan}” için parça bulunamadı.` : "Henüz parça tanımlanmadı."}</td>
                </tr>
              )}
              {liste?.map((p) => (
                <ParcaSatiri
                  key={p.id}
                  p={p}
                  yeni={p.id === yeniId}
                  duzenle={() => duzenle(p)}
                  adetDuzelt={admin ? () => { vazgec(); setDuzeltilen(p); } : undefined}
                />
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}

function ParcaSatiri({ p, yeni, duzenle, adetDuzelt }: {
  p: ParcaStok; yeni: boolean; duzenle: () => void; adetDuzelt?: () => void;
}) {
  const ipucu = `Kullanılabilir ${p.kullanilabilirStok} · toplam ${p.toplamStok} · minimum ${p.minStok}`;
  const stokAdresi = `/stok?parca=${encodeURIComponent(p.kod)}`;
  return (
    <tr className={p.minAltinda ? "uyari-satir" : undefined}>
      <td className="mono">{p.kod}</td>
      <td>
        {p.ad} {yeni && <span className="rozet rozet--kucuk rozet--mavi">yeni</span>}
      </td>
      <td>{p.birim}</td>
      {p.minAltinda ? (
        <td className="sag uyari-metin" title={ipucu}>
          <Link className="uyari-metin" href={stokAdresi}>{p.kullanilabilirStok}</Link> · min altında
        </td>
      ) : p.toplamStok ? (
        <td className="sag" title={ipucu}><Link href={stokAdresi}>{p.kullanilabilirStok}</Link></td>
      ) : (
        <td className="sag soluk">0</td>
      )}
      <td className={`sag${p.lotSayisi ? "" : " soluk"}`}>{p.lotSayisi}</td>
      {p.kullanildigiUrunler.length ? (
        <td>{p.kullanildigiUrunler.join(", ")}</td>
      ) : (
        <td className="soluk">Henüz hiçbir ürün ağacında yok</td>
      )}
      <td className="ikon-hucre">
        {adetDuzelt && (
          <button type="button" className="btn-ikon" title="Adet düzelt" aria-label={`${p.kod} adet düzelt`} onClick={adetDuzelt}>
            ±
          </button>
        )}
        <button type="button" className="btn-ikon" title="Düzenle" aria-label={`${p.kod} düzenle`} onClick={duzenle}>
          ✎
        </button>
      </td>
    </tr>
  );
}
