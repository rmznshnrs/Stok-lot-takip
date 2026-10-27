"use client";

import { useState, type FormEvent } from "react";
import { ApiHatasi, stok, type DuzeltmeTuru, type Lot } from "@/lib/api";

const TURLER: { deger: DuzeltmeTuru; ad: string }[] = [
  { deger: "SayimFarki", ad: "Sayım farkı" },
  { deger: "Fire", ad: "Fire" },
  { deger: "Diger", ad: "Diğer" },
];

type Ozellik = { lot: Lot; kapat: () => void; kaydedildi: (mesaj: string) => void };

async function calistir(is: () => Promise<string>, kaydedildi: (m: string) => void, setHata: (h: string) => void,
                        setBekliyor: (b: boolean) => void) {
  setHata("");
  setBekliyor(true);
  try {
    kaydedildi(await is());
  } catch (err) {
    setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
    setBekliyor(false);
  }
}

/** Yalnız yönetici: stok girişinden hatalı çıkan parça adedi; stoktan düşülür ya da yalnız sayı kaydedilir. */
export function HataliAdetFormu({ lot, kapat, kaydedildi }: Ozellik) {
  const [adet, setAdet] = useState("");
  const [dus, setDus] = useState<boolean | null>(null);
  const [aciklama, setAciklama] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  function kaydet(e: FormEvent) {
    e.preventDefault();
    const n = Number(adet);
    if (!Number.isInteger(n) || n < 1) return setHata("Adet en az 1 olmalı.");
    if (dus === null) return setHata("Stoktan düşülsün mü, seçin.");
    calistir(async () => {
      const s = await stok.hatali({ stokLotuId: lot.id, adet: n, stoktanDus: dus, aciklama: aciklama.trim() });
      return `${s.lotNo}: ${n} hatalı kaydedildi.`;
    }, kaydedildi, setHata, setBekliyor);
  }

  return (
    <form className="hatali-form" onSubmit={kaydet}>
      <strong>{lot.lotNo} · hatalı parça sayısı ekle</strong>
      <div className="izgara-2">
        <div className="alan">
          <label htmlFor={`hatali-adet-${lot.id}`}>Hatalı adet</label>
          <input id={`hatali-adet-${lot.id}`} type="number" min={1} className="girdi" required value={adet} onChange={(e) => setAdet(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor={`hatali-aciklama-${lot.id}`}>Açıklama</label>
          <input id={`hatali-aciklama-${lot.id}`} className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
        </div>
      </div>
      <div className="hatali-secim" role="radiogroup" aria-label="Stok">
        <span className="etiket-metni">Hatalı parçalar</span>
        <label className="onay-etiket"><input type="radio" name={`dus-${lot.id}`} checked={dus === true} onChange={() => setDus(true)} /> stoktan düşülsün</label>
        <label className="onay-etiket"><input type="radio" name={`dus-${lot.id}`} checked={dus === false} onChange={() => setDus(false)} /> yalnız sayı kaydedilsin</label>
      </div>
      {hata && <span className="hata">{hata}</span>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Vazgeç</button>
        <button type="submit" className="btn btn--geri btn--kucuk" disabled={bekliyor}>Kaydet</button>
      </div>
    </form>
  );
}

/** Yalnız yönetici: stok girişinin kalan adedini elle artırır (+) ya da stoktan düşer (−). */
export function AdetDuzeltFormu({ lot, kapat, kaydedildi }: Ozellik) {
  const [miktar, setMiktar] = useState("");
  const [tur, setTur] = useState<DuzeltmeTuru>("SayimFarki");
  const [aciklama, setAciklama] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  function kaydet(e: FormEvent) {
    e.preventDefault();
    const m = Number(miktar);
    if (!Number.isInteger(m) || m === 0) return setHata("Miktar 0 olamaz.");
    if (!window.confirm(`${lot.lotNo}: ${lot.kalanAdet} → ${lot.kalanAdet + m} olsun mu?`)) return;
    calistir(async () => {
      const s = await stok.duzelt({ stokLotuId: lot.id, miktar: m, tur, aciklama: aciklama.trim() });
      return `${s.lotNo} kaydedildi.`;
    }, kaydedildi, setHata, setBekliyor);
  }

  return (
    <form className="hatali-form" onSubmit={kaydet}>
      <strong>{lot.lotNo} · stoktan düş / adet düzelt</strong>
      <span className="yardim">Kalan {lot.kalanAdet} · düşmek için eksi girin</span>
      <div className="izgara-form">
        <div className="alan">
          <label htmlFor={`miktar-${lot.id}`}>Miktar</label>
          <input id={`miktar-${lot.id}`} type="number" className="girdi" required value={miktar} onChange={(e) => setMiktar(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor={`tur-${lot.id}`}>Neden</label>
          <select id={`tur-${lot.id}`} className="girdi" value={tur} onChange={(e) => setTur(e.target.value as DuzeltmeTuru)}>
            {TURLER.map((t) => <option key={t.deger} value={t.deger}>{t.ad}</option>)}
          </select>
        </div>
        <div className="alan">
          <label htmlFor={`duzelt-aciklama-${lot.id}`}>Açıklama</label>
          <input id={`duzelt-aciklama-${lot.id}`} className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
        </div>
      </div>
      {hata && <span className="hata">{hata}</span>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Vazgeç</button>
        <button type="submit" className="btn btn--geri btn--kucuk" disabled={bekliyor}>Kaydet</button>
      </div>
    </form>
  );
}
