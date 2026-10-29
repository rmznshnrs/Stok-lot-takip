"use client";

import { useState } from "react";
import { ApiHatasi, geriCagirma, type GeriCagirma } from "@/lib/api";

export const DURUM_ADI: Record<GeriCagirma["durum"], string> = {
  musteride: "Müşteriden geri istendi",
  geri_alindi: "Geri alındı, depoda",
  depoda: "Depoda, satışa kapalı",
  hurda: "Hurdaya ayrıldı",
  satisa_acildi: "Satışa açıldı",
  musteriye_verildi: "Müşteriye geri verildi",
};

export const acikMi = (g: GeriCagirma) => g.durum === "musteride" || g.durum === "geri_alindi" || g.durum === "depoda";

/**
 * Yalnız yönetici: geri çağrılan ürün için sıradaki adım.
 * Müşterideki (ya da müşteriden geri alınmış) ürün: müşteriye bilgi verildi, sonra müşteriye geri ver ya da hurdaya ayır.
 * Depodaki ürün: satışa aç ya da hurdaya ayır.
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
    await yap(`${g.seriNo} hurdaya ayrılsın mı?${g.durum === "musteride" ? " Ürün müşteriden geri alınmış sayılır." : ""}`,
      () => geriCagirma.hurda(g.id, aciklama.trim()));
  }

  if (!acikMi(g)) return null;
  const dugme = (yazi: string, tikla: () => void) => (
    <button type="button" className="btn btn--ikincil btn--kucuk" disabled={bekliyor} onClick={tikla}>{yazi}</button>
  );
  const musteriyle = g.durum === "musteride" || g.durum === "geri_alindi";
  return (
    <span className="dugme-grup">
      {g.durum === "musteride" && !g.bilgiVerildiZamani &&
        dugme("Müşteriye bilgi verildi", () => yap(`${g.musteriAd ?? "Müşteri"} bilgilendirildi mi?`, () => geriCagirma.bilgiVerildi(g.id)))}
      {musteriyle
        ? dugme("Müşteriye geri ver", () => yap(`${g.seriNo} müşteriye geri verilsin mi? Geri çağırma kapanır.`, () => geriCagirma.musteriyeVer(g.id)))
        : dugme("Satışa aç", () => yap(`${g.seriNo} satışa açılsın mı?`, () => geriCagirma.satisaAc(g.id)))}
      {dugme("Hurdaya ayır", hurda)}
      {hata && <span className="hata">{hata}</span>}
    </span>
  );
}
