"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, izleme, stok, type DuzeltmeTuru, type Lot } from "@/lib/api";

const TURLER: { deger: DuzeltmeTuru; ad: string }[] = [
  { deger: "SayimFarki", ad: "Sayım farkı" },
  { deger: "Fire", ad: "Fire" },
  { deger: "Diger", ad: "Diğer" },
];

/** Sayım farkı, fire vb. için lotun kalan adedini artırır (+) veya azaltır (−). */
export default function StokDuzeltme() {
  const [lotNo, setLotNo] = useState("");
  const [lot, setLot] = useState<Lot | null>(null);
  const [miktar, setMiktar] = useState("");
  const [tur, setTur] = useState<DuzeltmeTuru>("SayimFarki");
  const [aciklama, setAciklama] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  async function lotBul() {
    const no = lotNo.trim();
    if (!no) return;
    setMesaj(null);
    setLot(null);
    try {
      setLot((await izleme.lot(no)).lot);
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi && err.durum === 404 ? `${no} lot numarası bulunamadı.` : (err as Error).message });
    }
  }

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    if (!lot) return;
    const m = Number(miktar);
    if (!Number.isInteger(m) || m === 0) return setMesaj({ tur: "error", metin: "Miktar sıfırdan farklı bir tam sayı olmalı (azaltmak için eksi)." });
    if (!window.confirm(`${lot.lotNo}: kalan ${lot.kalanAdet} → ${lot.kalanAdet + m}. Onaylıyor musunuz?`)) return;
    setBekliyor(true);
    setMesaj(null);
    try {
      const s = await stok.duzelt({ stokLotuId: lot.id, miktar: m, tur, aciklama: aciklama.trim() });
      setMesaj({ tur: "success", metin: `${s.lotNo} düzeltildi (${s.miktar > 0 ? "+" : ""}${s.miktar}). Yeni kalan: ${s.yeniKalanAdet}.` });
      setLot({ ...lot, kalanAdet: s.yeniKalanAdet });
      setMiktar("");
      setAciklama("");
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Düzeltme yapılamadı." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <section className="kart kart--form kart--form-genis">
      <h2>Stok düzeltme</h2>
      <form className="form-dikey" onSubmit={kaydet}>
        <Mesaj mesaj={mesaj} />
        <div className="alan">
          <label htmlFor="duzeltme-lot">Lot numarası</label>
          <div className="satir-ekle">
            <input
              id="duzeltme-lot"
              className="girdi girdi--esnek girdi--mono"
              placeholder="L-…"
              autoComplete="off"
              value={lotNo}
              onChange={(e) => {
                setLotNo(e.target.value);
                setLot(null);
              }}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  lotBul();
                }
              }}
            />
            <button type="button" className="btn btn--ikincil" onClick={lotBul}>Bul</button>
          </div>
        </div>

        {lot && (
          <>
            <div className="bilgi-kutu">
              <strong>{lot.parcaAd}</strong> <span className="mono">{lot.parcaKod}</span>
              <br />
              Gelen {lot.girisAdet} · Kalan <strong>{lot.kalanAdet}</strong>
              {lot.geriCagrildi && " · geri çağrıldı"}
            </div>
            <div className="izgara-2">
              <div className="alan">
                <label htmlFor="miktar">Miktar (+/−)</label>
                <input id="miktar" type="number" className="girdi" placeholder="ör. -3" required value={miktar} onChange={(e) => setMiktar(e.target.value)} />
              </div>
              <div className="alan">
                <label htmlFor="tur">Tür</label>
                <select id="tur" className="girdi" value={tur} onChange={(e) => setTur(e.target.value as DuzeltmeTuru)}>
                  {TURLER.map((t) => <option key={t.deger} value={t.deger}>{t.ad}</option>)}
                </select>
              </div>
            </div>
            <div className="alan">
              <label htmlFor="aciklama">Açıklama</label>
              <input id="aciklama" className="girdi" required maxLength={500} placeholder="ör. Sayımda 3 adet eksik çıktı" value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
            </div>
          </>
        )}
        <button type="submit" className="btn btn--tam" disabled={!lot || bekliyor}>Düzeltmeyi kaydet</button>
      </form>
    </section>
  );
}
