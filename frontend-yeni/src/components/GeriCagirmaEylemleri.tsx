"use client";

import { useState } from "react";
import { ApiHatasi, geriCagirma, uretim, type GeriCagirma } from "@/lib/api";

export const DURUM_ADI: Record<GeriCagirma["durum"], string> = {
  musteride: "Müşteride, geri alınacak",
  geri_alindi: "Geri alındı, depoda",
  depoda: "Depoda, satışa kapalı",
  hurda: "Hurdaya ayrıldı",
  satisa_acildi: "Satışa açıldı",
  musteriye_verildi: "Müşteriye geri verildi",
};

export const acikMi = (g: GeriCagirma) => g.durum === "musteride" || g.durum === "geri_alindi" || g.durum === "depoda";

/**
 * Yalnız yönetici: geri çağrılan ürün için sıradaki adım. Müşterideyse iade al ya da (sorun yoksa) müşteride
 * bırak; geri alınmışsa müşteriye geri ver, satışa aç ya da hurdaya ayır.
 */
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
    const aciklama = window.prompt(`${g.seriNo} · hurda nedeni:`, g.aciklama);
    if (!aciklama?.trim()) return;
    await yap(`${g.seriNo} hurdaya ayrılsın mı?`, () =>
      uretim.hurda({ seriNolar: [g.seriNo], tur: "Hurda", aciklama: aciklama.trim() }));
  }

  if (!acikMi(g)) return null;
  return (
    <span className="dugme-grup">
      {g.durum === "musteride" ? (
        <>
          <button
            type="button"
            className="btn btn--ikincil btn--kucuk"
            disabled={bekliyor}
            onClick={() => yap(`${g.seriNo} iade alınsın mı?`,
              () => geriCagirma.geriAl(g.id))}
          >
            İade al
          </button>
          <button
            type="button"
            className="btn btn--ikincil btn--kucuk"
            disabled={bekliyor}
            onClick={() => yap(`${g.seriNo} müşteride kalsın mı?`,
              () => geriCagirma.musteriyeVer(g.id))}
          >
            Müşteride kalsın
          </button>
        </>
      ) : (
        <>
          {g.durum === "geri_alindi" && (
            <button
              type="button"
              className="btn btn--ikincil btn--kucuk"
              disabled={bekliyor}
              onClick={() => yap(`${g.seriNo} müşteriye geri verilsin mi?`,
                () => geriCagirma.musteriyeVer(g.id))}
            >
              Müşteriye geri ver
            </button>
          )}
          <button
            type="button"
            className="btn btn--ikincil btn--kucuk"
            disabled={bekliyor}
            onClick={() => yap(`${g.seriNo} satışa açılsın mı?`, () => geriCagirma.satisaAc(g.id))}
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
