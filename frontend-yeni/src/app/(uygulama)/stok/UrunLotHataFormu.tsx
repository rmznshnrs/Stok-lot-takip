"use client";

import { useState, type FormEvent } from "react";
import SeriSecimi from "@/components/SeriSecimi";
import { ApiHatasi, urunLot, type UretimGecmisi, type UrunLot } from "@/lib/api";

/**
 * Yalnız yönetici: ürün lotunu hatalı işaretler. Hatalı adet kullanıcıca belirlenir (0: belirtilmedi);
 * ürünler kendiliğinden geri çağrılmaz, istenirse lottakilerden seçilir. Hatalı lotta "Hatayı kaldır" da var.
 */
export default function UrunLotHataFormu({ lot, seriler, kapat, kaydedildi }: {
  lot: UrunLot;
  seriler: UretimGecmisi[];
  kapat: () => void;
  kaydedildi: (mesaj: string) => void;
}) {
  const [aciklama, setAciklama] = useState(lot.sorunAciklama);
  const [adet, setAdet] = useState(lot.hataliAdet ? String(lot.hataliAdet) : "");
  const [secili, setSecili] = useState<string[]>([]);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const adaylar = seriler
    .filter((g) => g.lotNo === lot.lotNo && !g.hurda && !g.geriCagirmaDurumu)
    .map((g) => ({ seriNo: g.seriNo, alt: g.musteriAd ?? "depoda" }));

  async function gonder(sorunlu: boolean) {
    const n = adet.trim() ? Number(adet) : 0;
    if (sorunlu && (!Number.isInteger(n) || n < 0 || n > lot.toplam))
      return setHata(`Hatalı adet en çok ${lot.toplam} olabilir.`);
    setHata("");
    setBekliyor(true);
    try {
      const s = await urunLot.sorun(lot.lotNo, { sorunlu, aciklama: aciklama.trim(), hataliAdet: n, geriCagrilacaklar: secili });
      const geri = s.geriCagrilanSeriNolar;
      kaydedildi(!sorunlu ? `${lot.lotNo}: hata kaldırıldı.` : `${lot.lotNo} kaydedildi.${geri.length ? ` ${geri.length} ürün geri çağrıldı.` : ""}`);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "İşlem yapılamadı.");
      setBekliyor(false);
    }
  }

  function kaydet(e: FormEvent) {
    e.preventDefault();
    if (window.confirm(`${lot.lotNo} hatalı işaretlensin mi?${secili.length ? ` ${secili.length} ürün geri çağrılacak.` : ""}`)) gonder(true);
  }

  return (
    <form className="hatali-form" onSubmit={kaydet}>
      <strong>{lot.lotNo} · {lot.sorunlu ? "hatayı düzenle" : "hatalı olarak işaretle"}</strong>
      <div className="izgara-2">
        <div className="alan">
          <label htmlFor={`urun-hata-${lot.lotNo}`}>Hata</label>
          <input id={`urun-hata-${lot.lotNo}`} className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor={`urun-adet-${lot.lotNo}`}>Hatalı adet (lotta {lot.toplam} ürün)</label>
          <input id={`urun-adet-${lot.lotNo}`} type="number" min={0} max={lot.toplam} className="girdi" value={adet} onChange={(e) => setAdet(e.target.value)} />
        </div>
      </div>
      <div className="alan">
        <span className="etiket-metni">Geri çağrılacak ürünler (isteğe bağlı)</span>
        <SeriSecimi adaylar={adaylar} secili={secili} setSecili={setSecili} bos="Ürün yok." />
      </div>
      {hata && <span className="hata">{hata}</span>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Vazgeç</button>
        {lot.sorunlu && (
          <button
            type="button"
            className="btn btn--ikincil btn--kucuk"
            disabled={bekliyor}
            onClick={() => window.confirm(`${lot.lotNo}: hata kaldırılsın mı?`) && gonder(false)}
          >
            Hatayı kaldır
          </button>
        )}
        <button type="submit" className="btn btn--geri btn--kucuk" disabled={bekliyor}>
          {secili.length ? `Kaydet ve ${secili.length} ürünü geri çağır` : "Kaydet"}
        </button>
      </div>
    </form>
  );
}
