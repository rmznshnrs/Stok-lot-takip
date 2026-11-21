"use client";

import { useSearchParams } from "next/navigation";
import { Fragment, useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import SayiGirdi from "@/components/SayiGirdi";
import { useSayfalama } from "@/components/Sayfalama";
import { ApiHatasi, parcalar, type ParcaStok } from "@/lib/api";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { useSiralama } from "@/lib/siralama";

type Form = { kod: string; ad: string; minStok: string; aciklama: string };
const BOS_FORM: Form = { kod: "", ad: "", minStok: "0", aciklama: "" };

export default function ParcalarEkrani() {
  const ilkParca = useSearchParams().get("parca") ?? "";
  const [acik, setAcik] = useState<string | null>(ilkParca || null);
  const [arama, setArama] = useState(ilkParca);
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata: listeHatasi, yenile } = useVeri(() => parcalar.listele(aranan), [aranan]);
  const { sirali, baslik, siralamaAnahtari } = useSiralama(liste, {
    kod: (p) => p.kod, ad: (p) => p.ad, aciklama: (p) => p.aciklama, min: (p) => p.minStok,
  });
  const { gorunen, cubuk } = useSayfalama(sirali, `${aranan}|${siralamaAnahtari}`);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [yeniId, setYeniId] = useState<number | null>(null);
  const [formAcik, setFormAcik] = useState(false);

  return (
    <main className="icerik icerik--dikey">
      <div className="sayfa-bas">
        <input type="search" className="girdi girdi--ara" aria-label="Parça ara" placeholder="Parça kodu ya da adı"
          value={arama} onChange={(e) => setArama(e.target.value)} />
        <button type="button" className="btn" aria-expanded={formAcik} onClick={() => { setMesaj(null); setFormAcik(!formAcik); }}>
          {formAcik ? "Formu Kapat" : "+ Yeni Parça"}
        </button>
      </div>

      {!formAcik && <Mesaj mesaj={mesaj} />}

      {formAcik && (
        <section className="acilir-form">
          <h3>Yeni Parça</h3>
          <ParcaFormu
            ilk={BOS_FORM}
            dugme="Parçayı Kaydet"
            kaydet={async (istek) => {
              const p = await parcalar.olustur(istek);
              setMesaj({ tur: "success", metin: `${p.kod} kaydedildi.` });
              setYeniId(p.id);
              setFormAcik(false);
              yenile();
            }}
            vazgec={() => setFormAcik(false)}
          />
        </section>
      )}

      <section className="panel">
        {listeHatasi && <div className="panel-ic"><div className="mesaj mesaj--error">{listeHatasi}</div></div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                {baslik("kod", "Kod")}
                {baslik("ad", "Ad")}
                {baslik("aciklama", "Açıklama")}
                {baslik("min", "Min Stok", "sag")}
                <th>Kullanıldığı Ürünler</th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !listeHatasi && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && <tr className="bos"><td colSpan={5}>{aranan ? "Sonuç yok." : "Parça yok."}</td></tr>}
              {gorunen?.map((p) => (
                <Fragment key={p.id}>
                  <ParcaSatiri p={p} yeni={p.id === yeniId} acik={acik === p.kod} ac={() => setAcik(acik === p.kod ? null : p.kod)} />
                  {acik === p.kod && (
                    <tr className="acilan-satir">
                      <td colSpan={5}>
                        <ParcaFormu
                          key={`${p.kod}|${p.ad}|${p.minStok}|${p.aciklama}`}
                          ilk={{ kod: p.kod, ad: p.ad, minStok: String(p.minStok), aciklama: p.aciklama ?? "" }}
                          dugme="Değişiklikleri Kaydet"
                          kaydet={async (istek) => {
                            const k = await parcalar.guncelle(p.id, istek);
                            setMesaj({ tur: "success", metin: `${k.kod} kaydedildi.` });
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

function ParcaFormu({ ilk, dugme, kaydet, vazgec }: {
  ilk: Form;
  dugme: string;
  kaydet: (istek: { kod: string; ad: string; minStok: number; aciklama: string }) => Promise<void>;
  vazgec?: () => void;
}) {
  const [form, setForm] = useState(ilk);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const degisti = (Object.keys(ilk) as (keyof Form)[]).some((k) => form[k] !== ilk[k]);
  const on = `parca-${ilk.kod || "yeni"}`;

  async function gonder(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      await kaydet({ kod: form.kod.trim(), ad: form.ad.trim(), minStok: Number(form.minStok) || 0, aciklama: form.aciklama.trim() });
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <form className="form-dikey parca-duzenle" onSubmit={gonder}>
      <div className="izgara-form izgara-form--4">
        <div className="alan">
          <label htmlFor={`${on}-kod`}>Parça Kodu</label>
          <input id={`${on}-kod`} className="girdi girdi--mono" required maxLength={50} value={form.kod}
            onChange={(e) => setForm({ ...form, kod: e.target.value })} />
        </div>
        <div className="alan">
          <label htmlFor={`${on}-ad`}>Parça Adı</label>
          <input id={`${on}-ad`} className="girdi" required maxLength={200} value={form.ad}
            onChange={(e) => setForm({ ...form, ad: e.target.value })} />
        </div>
        <div className="alan">
          <label htmlFor={`${on}-min`}>Minimum Stok</label>
          <SayiGirdi id={`${on}-min`} min={0} value={form.minStok} onChange={(v) => setForm({ ...form, minStok: v })} />
        </div>
      </div>
      <div className="alan">
        <label htmlFor={`${on}-aciklama`}>Açıklama</label>
        <textarea id={`${on}-aciklama`} className="girdi girdi--cok-satir" rows={2} maxLength={500} value={form.aciklama}
          onChange={(e) => setForm({ ...form, aciklama: e.target.value })} />
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
      <div className="dugme-grup">
        <button type="submit" className="btn" disabled={bekliyor || !degisti}>{bekliyor ? "Kaydediliyor…" : dugme}</button>
        {vazgec && <button type="button" className="btn btn--ikincil" onClick={vazgec}>Vazgeç</button>}
      </div>
    </form>
  );
}

function ParcaSatiri({ p, yeni, acik, ac }: { p: ParcaStok; yeni: boolean; acik: boolean; ac: () => void }) {
  return (
    <tr className="satir-tikla">
      <td className="mono">{p.kod}</td>
      <td>
        <button type="button" className="btn-baglanti parca-ac satir-bag" aria-expanded={acik} onClick={ac}>
          <span className="parca-ok" aria-hidden="true">{acik ? "▾" : "▸"}</span>
          {p.ad}
        </button>{" "}
        {yeni && <span className="rozet rozet--kucuk rozet--mavi">yeni</span>}
      </td>
      <td className={p.aciklama ? "yardim" : "soluk"}>{p.aciklama || "—"}</td>
      <td className="sag">{p.minStok}</td>
      {p.kullanildigiUrunler.length ? <td>{p.kullanildigiUrunler.join(", ")}</td> : <td className="soluk">Henüz hiçbir ürün ağacında yok</td>}
    </tr>
  );
}
