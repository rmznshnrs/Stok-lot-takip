"use client";

import { useState } from "react";
import { ApiHatasi, geriCagirma, uretim, type GeriCagirma } from "@/lib/api";

export const DURUM_ADI: Record<GeriCagirma["durum"], string> = {
  musteride: "Müşteride, geri alınacak",
  geri_alindi: "Geri alındı, depoda",
  depoda: "Depoda, satışa kapalı",
  hurda: "Hurdaya ayrıldı",
  satisa_acildi: "Satışa açıldı",
};

export const acikMi = (g: GeriCagirma) => g.durum === "musteride" || g.durum === "geri_alindi" || g.durum === "depoda";

/** Yalnız yönetici: geri çağrılan ürün için sıradaki adım (geri alındı / satışa aç / hurdaya ayır). */
export default function GeriCagirmaEylemleri({ g, degisti }: { g: GeriCagirma; degisti: () => void }) {
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function yap(soru: string, is: () => Promise<unknown>) {
    if (!window.confirm(soru)) return;
    setHata("");
    setBekliyor(true);
    try {
      await is();
      degisti();
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "İşlem yapılamadı.");
    } finally {
      setBekliyor(false);
    }
  }

  async function hurda() {
    const aciklama = window.prompt(`${g.seriNo} hurdaya ayrılacak. Açıklama:`, g.aciklama);
    if (!aciklama?.trim()) return;
    await yap(`${g.seriNo} hurdaya ayrılsın mı? Bu ürün bir daha satılamaz.`, () =>
      uretim.hurda({ seriNolar: [g.seriNo], tur: "Hurda", aciklama: aciklama.trim() }));
  }

  if (!acikMi(g)) return null;
  return (
    <span className="dugme-grup">
      {g.durum === "musteride" ? (
        <button
          type="button"
          className="btn btn--ikincil btn--kucuk"
          disabled={bekliyor}
          onClick={() => yap(`${g.seriNo} müşteriden (${g.musteriAd}) geri alındı mı? Satış iadesi yapılacak, ürün depoya dönecek.`,
            () => geriCagirma.geriAl(g.id))}
        >
          Geri alındı
        </button>
      ) : (
        <>
          <button
            type="button"
            className="btn btn--ikincil btn--kucuk"
            disabled={bekliyor}
            onClick={() => yap(`${g.seriNo} sorunsuz bulundu / onarıldı mı? Yeniden satışa açılacak.`, () => geriCagirma.satisaAc(g.id))}
          >
            Satışa aç
          </button>
          <button type="button" className="btn btn--ikincil btn--kucuk" disabled={bekliyor} onClick={hurda}>
            Hurdaya ayır
          </button>
        </>
      )}
      {hata && <span className="hata">{hata}</span>}
    </span>
  );
}
