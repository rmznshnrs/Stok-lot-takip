"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, stok, type DuzeltmeTuru, type ParcaStok } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

const TURLER: { deger: DuzeltmeTuru; ad: string }[] = [
  { deger: "SayimFarki", ad: "Sayım farkı" },
  { deger: "Fire", ad: "Fire" },
  { deger: "Diger", ad: "Diğer" },
];

/** Yalnız Admin: parçanın bir lotunun kalan adedini artırır (+) veya azaltır (−). */
export default function ParcaAdetDuzeltme({ parca, kapat, kaydedildi }: {
  parca: ParcaStok;
  kapat: () => void;
  kaydedildi: () => void;
}) {
  const { veri: gruplar, hata, yenile } = useVeri(() => stok.durum({ parca: parca.kod, bitenler: true }), [parca.kod]);
  const lotlar = gruplar?.find((g) => g.parca.id === parca.id)?.lotlar ?? [];

  const [lotId, setLotId] = useState<number | null>(null);
  const [miktar, setMiktar] = useState("");
  const [tur, setTur] = useState<DuzeltmeTuru>("SayimFarki");
  const [aciklama, setAciklama] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);
  const lot = lotlar.find((l) => l.id === lotId) ?? null;

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    if (!lot) return setMesaj({ tur: "error", metin: "Düzeltilecek lotu seçin." });
    const m = Number(miktar);
    if (!Number.isInteger(m) || m === 0) return setMesaj({ tur: "error", metin: "Miktar sıfırdan farklı bir tam sayı olmalı (azaltmak için eksi)." });
    if (!window.confirm(`${lot.lotNo}: kalan ${lot.kalanAdet} → ${lot.kalanAdet + m}. Onaylıyor musunuz?`)) return;
    setBekliyor(true);
    setMesaj(null);
    try {
      const s = await stok.duzelt({ stokLotuId: lot.id, miktar: m, tur, aciklama: aciklama.trim() });
      setMesaj({ tur: "success", metin: `${s.lotNo} düzeltildi (${s.miktar > 0 ? "+" : ""}${s.miktar}). Yeni kalan: ${s.yeniKalanAdet}.` });
      setMiktar("");
      setAciklama("");
      yenile();
      kaydedildi();
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Düzeltme yapılamadı." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <>
      <h2>Adet düzelt</h2>
      <p className="not">
        <strong>{parca.ad}</strong> <span className="mono">{parca.kod}</span>. Sayımda farklı çıkan ya da bozulan (fire)
        parçalar için lotu seçin; azaltmak için eksi, artırmak için artı adet girin.
      </p>
      <form className="form-dikey" onSubmit={kaydet}>
        <Mesaj mesaj={mesaj} />
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="alan">
          <span className="etiket-metni">Lot</span>
          {gruplar === null && !hata && <p className="yukleniyor">Yükleniyor…</p>}
          {gruplar && lotlar.length === 0 && <p className="not">Bu parçanın hiç lotu yok; önce mal girişi yapın.</p>}
          <div className="lot-secim">
            {lotlar.map((l) => (
              <label key={l.id} className={`lot-secenek${l.id === lotId ? " lot-secenek--secili" : ""}`}>
                <input type="radio" name="lot" checked={l.id === lotId} onChange={() => setLotId(l.id)} />
                <span className="mono">{l.lotNo}</span>
                <span className="not">{tarih(l.siparisTarihi)}{l.geriCagrildi && " · geri çağrıldı"}</span>
                <span className="lot-secenek-adet">{l.kalanAdet} / {l.girisAdet}</span>
              </label>
            ))}
          </div>
        </div>
        <fieldset className="alan-grubu" disabled={!lot}>
          <div className="izgara-2">
            <div className="alan">
              <label htmlFor="miktar">Miktar (+/−)</label>
              <input id="miktar" type="number" className="girdi" required value={miktar} onChange={(e) => setMiktar(e.target.value)} />
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
            <input id="aciklama" className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
          </div>
        </fieldset>
        <button type="submit" className="btn btn--tam" disabled={!lot || bekliyor}>Düzeltmeyi kaydet</button>
        <button type="button" className="btn btn--ikincil" onClick={kapat}>Kapat</button>
      </form>
    </>
  );
}
