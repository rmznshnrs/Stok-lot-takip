"use client";

import { useState, type FormEvent } from "react";
import { ApiHatasi, izleme, type Lot } from "@/lib/api";

/**
 * Yalnız yönetici: lotu hatalı işaretler. Hata açıklaması ve kalan parçaların üretimde kullanılıp
 * kullanılmayacağı (seçim zorunlu) girilir. İlk işaretlemede bu lottan parça taşıyan ürünler kendiliğinden
 * geri çağrılır. Hatalı lotta "Hatayı kaldır" da sunulur.
 */
export default function HataliLotFormu({ lot, kapat, kaydedildi }: {
  lot: Lot;
  kapat: () => void;
  kaydedildi: (mesaj: string) => void;
}) {
  const [aciklama, setAciklama] = useState(lot.sorunAciklama);
  const [kullan, setKullan] = useState<boolean | null>(lot.sorunlu ? !lot.geriCagrildi : null);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function gonder(sorunlu: boolean, uretimdeKullan: boolean) {
    setHata("");
    setBekliyor(true);
    try {
      const s = await izleme.sorun(lot.lotNo, { sorunlu, uretimdeKullan, aciklama: aciklama.trim() });
      const geri = s.geriCagrilanSeriNolar;
      kaydedildi(
        !sorunlu
          ? `${lot.lotNo} artık hatalı sayılmıyor. Geri çağrılmış ürünler listede kalır.`
          : lot.sorunlu
            ? `${lot.lotNo} güncellendi.`
            : `${lot.lotNo} hatalı işaretlendi. ${geri.length ? `${geri.length} ürün geri çağrıldı: ${geri.join(", ")}` : "Bu lottan yapılmış ürün yok."}`,
      );
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "İşlem yapılamadı.");
      setBekliyor(false);
    }
  }

  function kaydet(e: FormEvent) {
    e.preventDefault();
    if (kullan === null) return setHata("Kalan parçaların üretimde kullanılıp kullanılmayacağını seçin.");
    if (!lot.sorunlu && !window.confirm(`${lot.lotNo} hatalı işaretlenecek ve bu lottan parça taşıyan ürünler geri çağrılacak. Onaylıyor musunuz?`)) return;
    gonder(true, kullan);
  }

  return (
    <form className="hatali-form" onSubmit={kaydet}>
      <strong>{lot.lotNo} · {lot.sorunlu ? "hatayı düzenle" : "hatalı lot olarak işaretle"}</strong>
      <div className="alan">
        <label htmlFor={`hata-${lot.id}`}>Hata</label>
        <input id={`hata-${lot.id}`} className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
      </div>
      <div className="hatali-secim" role="radiogroup" aria-label="Kalan parçalar">
        <span className="etiket-metni">Stokta kalan {lot.kalanAdet} parça üretimde</span>
        <label className="onay-etiket"><input type="radio" name={`kullan-${lot.id}`} checked={kullan === true} onChange={() => setKullan(true)} /> kullanılsın</label>
        <label className="onay-etiket"><input type="radio" name={`kullan-${lot.id}`} checked={kullan === false} onChange={() => setKullan(false)} /> kullanılmasın</label>
      </div>
      {!lot.sorunlu && <span className="yardim">Kaydedince bu lottan parça taşıyan ürünler kendiliğinden geri çağrılır.</span>}
      {hata && <span className="hata">{hata}</span>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Vazgeç</button>
        {lot.sorunlu && (
          <button
            type="button"
            className="btn btn--ikincil btn--kucuk"
            disabled={bekliyor}
            onClick={() => window.confirm(`${lot.lotNo} artık hatalı sayılmasın mı?`) && gonder(false, true)}
          >
            Hatayı kaldır
          </button>
        )}
        <button type="submit" className="btn btn--geri btn--kucuk" disabled={bekliyor}>Kaydet</button>
      </div>
    </form>
  );
}
