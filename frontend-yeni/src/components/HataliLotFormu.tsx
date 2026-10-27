"use client";

import { useState, type FormEvent } from "react";
import SeriSecimi from "@/components/SeriSecimi";
import { ApiHatasi, izleme, type Lot } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";

/**
 * Yalnız yönetici: stok girişini hatalı işaretler. Hata açıklaması ve kalan parçaların üretimde kullanılıp
 * kullanılmayacağı (seçim zorunlu) girilir. Ürünler kendiliğinden geri çağrılmaz; bu girişin parçalarını
 * taşıyan ürünlerden geri çağrılacaklar seçilir. Hatalı girişte "Hatayı kaldır" da sunulur.
 */
export default function HataliLotFormu({ lot, kapat, kaydedildi }: {
  lot: Lot;
  kapat: () => void;
  kaydedildi: (mesaj: string) => void;
}) {
  const [aciklama, setAciklama] = useState(lot.sorunAciklama);
  const [kullan, setKullan] = useState<boolean | null>(lot.sorunlu ? !lot.geriCagrildi : null);
  const [secili, setSecili] = useState<string[]>([]);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const { veri: iz } = useVeri(() => izleme.lot(lot.lotNo), [lot.lotNo]);
  const adaylar = iz
    ? iz.uretimler
        .filter((u) => !u.hurdaTuru && !u.geriCagirmaDurumu)
        .map((u) => ({ seriNo: u.seriNo, alt: u.satis ? u.satis.musteriAd : "depoda" }))
    : null;

  async function gonder(sorunlu: boolean, uretimdeKullan: boolean) {
    setHata("");
    setBekliyor(true);
    try {
      const s = await izleme.sorun(lot.lotNo, { sorunlu, uretimdeKullan, aciklama: aciklama.trim(), geriCagrilacaklar: secili });
      const geri = s.geriCagrilanSeriNolar;
      kaydedildi(!sorunlu ? `${lot.lotNo}: hata kaldırıldı.` : `${lot.lotNo} kaydedildi.${geri.length ? ` ${geri.length} ürün geri çağrıldı.` : ""}`);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "İşlem yapılamadı.");
      setBekliyor(false);
    }
  }

  function kaydet(e: FormEvent) {
    e.preventDefault();
    if (kullan === null) return setHata("Kullanılsın / kullanılmasın seçin.");
    if (!window.confirm(`${lot.lotNo} hatalı işaretlensin mi?${secili.length ? ` ${secili.length} ürün geri çağrılacak.` : ""}`)) return;
    gonder(true, kullan);
  }

  return (
    <form className="hatali-form" onSubmit={kaydet}>
      <strong>{lot.lotNo} · {lot.sorunlu ? "hatayı düzenle" : "hatalı olarak işaretle"}</strong>
      <div className="alan">
        <label htmlFor={`hata-${lot.id}`}>Hata</label>
        <input id={`hata-${lot.id}`} className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
      </div>
      <div className="hatali-secim" role="radiogroup" aria-label="Kalan parçalar">
        <span className="etiket-metni">Stokta kalan {lot.kalanAdet} parça üretimde</span>
        <label className="onay-etiket"><input type="radio" name={`kullan-${lot.id}`} checked={kullan === true} onChange={() => setKullan(true)} /> kullanılsın</label>
        <label className="onay-etiket"><input type="radio" name={`kullan-${lot.id}`} checked={kullan === false} onChange={() => setKullan(false)} /> kullanılmasın</label>
      </div>
      <div className="alan">
        <span className="etiket-metni">Geri çağrılacak ürünler (isteğe bağlı)</span>
        <SeriSecimi
          adaylar={adaylar}
          secili={secili}
          setSecili={setSecili}
          bos="Ürün yok."
        />
      </div>
      {hata && <span className="hata">{hata}</span>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Vazgeç</button>
        {lot.sorunlu && (
          <button
            type="button"
            className="btn btn--ikincil btn--kucuk"
            disabled={bekliyor}
            onClick={() => window.confirm(`${lot.lotNo}: hata kaldırılsın mı?`) && gonder(false, true)}
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
