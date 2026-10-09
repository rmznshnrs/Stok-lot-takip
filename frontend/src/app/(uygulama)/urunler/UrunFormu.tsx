"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, urunler, type ParcaStok, type UrunAgaciSatiri, type UrunDetay } from "@/lib/api";

interface Props {
  urun: UrunDetay | null;
  parcalar: ParcaStok[];
  kaydedildi: (id: number, mesaj: string) => void;
}

/** Ürün bilgileri ve ürün ağacı. Ağaç değişiklikleri "Kaydet" ile uygulanır. */
export default function UrunFormu({ urun, parcalar, kaydedildi }: Props) {
  const [kod, setKod] = useState(urun?.kod ?? "");
  const [ad, setAd] = useState(urun?.ad ?? "");
  const [seriOneki, setSeriOneki] = useState(urun?.seriOneki ?? "");
  const [agac, setAgac] = useState<UrunAgaciSatiri[]>(urun?.agac ?? []);
  const [yeniParca, setYeniParca] = useState("");
  const [yeniAdet, setYeniAdet] = useState("");
  const [satirHatasi, setSatirHatasi] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  function satirEkle() {
    setSatirHatasi("");
    const metin = yeniParca.trim().toLocaleLowerCase("tr");
    const adet = Number(yeniAdet);
    const p =
      parcalar.find((x) => x.kod.toLocaleLowerCase("tr") === metin) ??
      parcalar.find((x) => x.ad.toLocaleLowerCase("tr") === metin);
    if (!p) return setSatirHatasi("Bu kod veya adla bir parça yok.");
    if (!Number.isInteger(adet) || adet < 1) return setSatirHatasi("Adet en az 1 olmalı.");
    if (agac.some((s) => s.parcaId === p.id)) return setSatirHatasi(`${p.kod} zaten ağaçta; adedini satırdan değiştirin.`);
    setAgac([
      ...agac,
      { parcaId: p.id, parcaKod: p.kod, parcaAd: p.ad, birim: p.birim, adet, kullanilabilirStok: p.kullanilabilirStok, minAltinda: p.minAltinda },
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
      return setMesaj({ tur: "error", metin: "Ürün ağacındaki adetler en az 1 olmalı." });
    }
    setBekliyor(true);
    try {
      const bilgi = { kod: kod.trim(), ad: ad.trim(), seriOneki: seriOneki.trim() };
      const kayitli = urun ? await urunler.guncelle(urun.id, bilgi) : await urunler.olustur(bilgi);
      await urunler.agacKaydet(kayitli.id, agac.map((s) => ({ parcaId: s.parcaId, adet: s.adet })));
      kaydedildi(kayitli.id, `${kayitli.kod} – ${kayitli.ad} kaydedildi.`);
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <form className="form-dikey" onSubmit={kaydet}>
      {!urun && <h2>Yeni ürün</h2>}
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
        <div className="alan">
          <label htmlFor="seriOneki">Seri no öneki</label>
          <input
            id="seriOneki"
            className="girdi girdi--mono"
            required
            maxLength={20}
            placeholder="ör. SN-IP-"
            value={seriOneki}
            disabled={urun ? !urun.seriOnekiDegistirilebilir : false}
            onChange={(e) => setSeriOneki(e.target.value)}
          />
          <span className="yardim">
            {urun && !urun.seriOnekiDegistirilebilir ? "Üretim yapıldığı için değiştirilemez." : "Seri no: önek + 4 hane (SN-IP-0001)"}
          </span>
        </div>
      </div>

      <div className="baslik-grup">
        <h3>Ürün ağacı</h3>
        <span className="not">1 adet ürün için hangi parçadan kaç adet kullanılır</span>
      </div>

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
              <tr className="bos"><td colSpan={5}>Ürün ağacı boş. Aşağıdan parça ekleyin.</td></tr>
            )}
            {agac.map((s) => (
              <tr key={s.parcaId}>
                <td>{s.parcaAd}</td>
                <td className="mono">{s.parcaKod}</td>
                <td>
                  <input
                    type="number"
                    min={1}
                    className="girdi girdi--adet"
                    aria-label={`${s.parcaKod} adet`}
                    value={Number.isNaN(s.adet) ? "" : s.adet}
                    onChange={(e) => adetDegistir(s.parcaId, e.target.value)}
                  />
                </td>
                <td className={`sag${s.minAltinda ? " uyari-metin" : ""}`}>{s.kullanilabilirStok}</td>
                <td>
                  <button
                    type="button"
                    className="btn-kaldir"
                    title="Kaldır (Kaydet ile uygulanır)"
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

      <div className="satir-ekle">
        <div className="alan girdi--esnek">
          <input
            list="parca-listesi"
            className="girdi girdi--kesikli"
            aria-label="Parça ekle"
            placeholder="Parça adı veya kodu ile ekle…"
            autoComplete="off"
            value={yeniParca}
            onChange={(e) => setYeniParca(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                satirEkle();
              }
            }}
          />
          {satirHatasi && <span className="hata">{satirHatasi}</span>}
        </div>
        <div className="alan girdi--kisa">
          <input
            type="number"
            min={1}
            className="girdi girdi--kesikli"
            aria-label="Adet"
            placeholder="Adet"
            value={yeniAdet}
            onChange={(e) => setYeniAdet(e.target.value)}
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
      <datalist id="parca-listesi">
        {parcalar.map((p) => (
          <option key={p.id} value={p.kod}>{p.ad}</option>
        ))}
      </datalist>

      <div className="kart-alt">
        {urun ? <Link href={`/uretim?urun=${urun.id}`} className="not--buyuk">Bu üründen üret →</Link> : <span />}
        <button type="submit" className="btn" disabled={bekliyor}>Kaydet</button>
      </div>
    </form>
  );
}
