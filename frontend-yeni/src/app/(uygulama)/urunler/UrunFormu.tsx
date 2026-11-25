"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, urunler, type ParcaStok, type UrunAgaciSatiri, type UrunDetay } from "@/lib/api";
import SayiGirdi from "@/components/SayiGirdi";

interface Props {
  urun: UrunDetay | null;
  parcalar: ParcaStok[];
  kaydedildi: (id: number, mesaj: string) => void;
}

export default function UrunFormu({ urun, parcalar, kaydedildi }: Props) {
  const [kod, setKod] = useState(urun?.kod ?? "");
  const [ad, setAd] = useState(urun?.ad ?? "");
  const [aciklama, setAciklama] = useState(urun?.aciklama ?? "");
  const [agac, setAgac] = useState<UrunAgaciSatiri[]>(urun?.agac ?? []);
  const [yeniParca, setYeniParca] = useState("");
  const [yeniAdet, setYeniAdet] = useState("");
  const [satirHatasi, setSatirHatasi] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  function satirEkle() {
    setSatirHatasi("");
    const adet = Number(yeniAdet);
    const p = parcalar.find((x) => String(x.id) === yeniParca);
    if (!p) return setSatirHatasi("Parça seçin.");
    if (!Number.isInteger(adet) || adet < 1) return setSatirHatasi("Adet en az 1 olmalı.");
    if (agac.some((s) => s.parcaId === p.id)) return setSatirHatasi(`${p.kod} zaten ağaçta; adedini satırdan değiştirin.`);
    setAgac([
      ...agac,
      { parcaId: p.id, parcaKod: p.kod, parcaAd: p.ad, adet, kullanilabilirStok: p.kullanilabilirStok, minAltinda: p.minAltinda },
    ].sort((a, b) => a.parcaKod.localeCompare(b.parcaKod)));
    setYeniParca("");
    setYeniAdet("");
  }

  function adetDegistir(parcaId: number, deger: string) {
    setAgac(agac.map((s) => (s.parcaId === parcaId ? { ...s, adet: Number(deger) } : s)));
  }

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    if (agac.some((s) => !Number.isInteger(s.adet) || s.adet < 1)) {
      return setMesaj({ tur: "error", metin: "Adet en az 1 olmalı." });
    }
    setBekliyor(true);
    try {
      const bilgi = { kod: kod.trim(), ad: ad.trim(), aciklama: aciklama.trim() };
      const kayitli = urun ? await urunler.guncelle(urun.id, bilgi) : await urunler.olustur(bilgi);
      await urunler.agacKaydet(kayitli.id, agac.map((s) => ({ parcaId: s.parcaId, adet: s.adet })));
      kaydedildi(kayitli.id, `${kayitli.kod} kaydedildi.`);
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <form className="form-dikey" onSubmit={kaydet}>
      <h2>{urun ? `${urun.ad} – düzenle` : "Yeni ürün"}</h2>
      <Mesaj mesaj={mesaj} />
      <div className="izgara-urun">
        <div className="alan">
          <label htmlFor="ad">Ürün adı</label>
          <input id="ad" className="girdi girdi--vurgulu" required maxLength={200} value={ad} onChange={(e) => setAd(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor="kod">Ürün kodu</label>
          <input id="kod" className="girdi girdi--mono" required maxLength={50} value={kod} onChange={(e) => setKod(e.target.value)} />
        </div>
      </div>
      <div className="alan">
        <label htmlFor="urun-aciklama">Açıklama</label>
        <textarea id="urun-aciklama" className="girdi girdi--cok-satir" rows={2} maxLength={500} value={aciklama}
          onChange={(e) => setAciklama(e.target.value)} />
      </div>

      <h3>Ürün ağacı</h3>

      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Parça</th>
              <th>Parça kodu</th>
              <th className="sutun-adet">Adet / ürün</th>
              <th className="sag">Stokta</th>
              <th className="sutun-ikon"><span className="gorunmez">Kaldır</span></th>
            </tr>
          </thead>
          <tbody>
            {agac.length === 0 && (
              <tr className="bos"><td colSpan={5}>Ürün ağacı boş.</td></tr>
            )}
            {agac.map((s) => (
              <tr key={s.parcaId}>
                <td>{s.parcaAd}</td>
                <td className="mono">{s.parcaKod}</td>
                <td>
                  <SayiGirdi min={1} kucuk aria-label={`${s.parcaKod} adet`}
                    value={Number.isNaN(s.adet) ? "" : s.adet} onChange={(v) => adetDegistir(s.parcaId, v)} />
                </td>
                <td className={`sag${s.minAltinda ? " uyari-metin" : ""}`}>{s.kullanilabilirStok}</td>
                <td>
                  <button
                    type="button"
                    className="btn-kaldir"
                    title="Kaldır"
                    aria-label={`${s.parcaKod} satırını kaldır`}
                    onClick={() => setAgac(agac.filter((x) => x.parcaId !== s.parcaId))}
                  >
                    ×
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <div className="satir-ekle satir-ekle--alt">
        <div className="alan girdi--esnek">
          <label htmlFor="parca-ekle">Parça ekle</label>
          <select id="parca-ekle" className="girdi" value={yeniParca} onChange={(e) => setYeniParca(e.target.value)}>
            <option value="">Parça seçin</option>
            {parcalar.filter((p) => !agac.some((s) => s.parcaId === p.id)).map((p) => (
              <option key={p.id} value={p.id}>{p.kod} · {p.ad}</option>
            ))}
          </select>
          {satirHatasi && <span className="hata">{satirHatasi}</span>}
        </div>
        <div className="alan alan--adet">
          <label htmlFor="parca-adet">Adet</label>
          <SayiGirdi
            id="parca-adet"
            min={1}
            value={yeniAdet}
            onChange={setYeniAdet}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                satirEkle();
              }
            }}
          />
        </div>
        <button type="button" className="btn btn--ikincil" onClick={satirEkle}>Ekle</button>
      </div>

      <div className="kart-alt">
        <span />
        <div className="dugme-grup">
          {urun && <Link href={`/urunler?urun=${urun.id}`} className="btn btn--ikincil">Vazgeç</Link>}
          <button type="submit" className="btn" disabled={bekliyor}>Kaydet</button>
        </div>
      </div>
    </form>
  );
}
