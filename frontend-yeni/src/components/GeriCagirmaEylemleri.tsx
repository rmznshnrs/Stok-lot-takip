"use client";

import { useState } from "react";
import { KargoFirmaSecimi } from "@/app/(uygulama)/sevkiyat/SevkiyatDetay";
import { ApiHatasi, geriCagirma, type GeriCagirma, type KargoBilgisi } from "@/lib/api";

export const DURUM_ADI: Record<GeriCagirma["durum"], string> = {
  musteride: "Müşteriden geri İstendi",
  geri_alindi: "Geri alındı, depoda",
  depoda: "Depoda, satışa kapalı",
  hurda: "Hurdaya ayrıldı",
  satisa_acildi: "Satışa açıldı",
  musteriye_verildi: "Müşteriye geri verildi",
};

export const acikMi = (g: GeriCagirma) => g.durum === "musteride" || g.durum === "geri_alindi" || g.durum === "depoda";

export default function GeriCagirmaEylemleri({ g, degisti }: { g: GeriCagirma; degisti: () => void }) {
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const [kargoFormu, setKargoFormu] = useState<"geri-al" | "geri-ver" | null>(null);

  async function yap(soru: string | null, is: () => Promise<unknown>) {
    if (soru && !window.confirm(soru)) return;
    setHata("");
    setBekliyor(true);
    try {
      await is();
      setKargoFormu(null);
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
      {g.durum === "musteride" && dugme("Müşteriden geri alındı", () => setKargoFormu(kargoFormu === "geri-al" ? null : "geri-al"))}
      {musteriyle
        ? dugme("Müşteriye geri ver", () => setKargoFormu(kargoFormu === "geri-ver" ? null : "geri-ver"))
        : dugme("Satışa aç", () => yap(`${g.seriNo} satışa açılsın mı?`, () => geriCagirma.satisaAc(g.id)))}
      {dugme("Hurdaya ayır", hurda)}
      {kargoFormu && (
        <KargoFormu id={`gc-${g.id}-${kargoFormu}`} bekliyor={bekliyor}
          baslik={kargoFormu === "geri-al" ? "Müşteriden geliş kargosu" : "Müşteriye gönderim kargosu"}
          dugmeYazisi={kargoFormu === "geri-al" ? "Geri Alındı Olarak Kaydet" : "Müşteriye Gönder"}
          vazgec={() => setKargoFormu(null)}
          kaydet={(k) => yap(null, () => kargoFormu === "geri-al" ? geriCagirma.geriAl(g.id, k) : geriCagirma.musteriyeVer(g.id, k))} />
      )}
      {hata && <span className="hata">{hata}</span>}
    </span>
  );
}

function KargoFormu({ id, baslik, dugmeYazisi, bekliyor, kaydet, vazgec }: {
  id: string; baslik: string; dugmeYazisi: string; bekliyor: boolean;
  kaydet: (k: KargoBilgisi) => void; vazgec: () => void;
}) {
  const [kargoFirmasi, setKargoFirmasi] = useState("");
  const [takipNo, setTakipNo] = useState("");
  return (
    <span className="kargo-formu">
      <b>{baslik}</b>
      <span className="alan">
        <label htmlFor={`${id}-firma`}>Kargo Firması</label>
        <KargoFirmaSecimi id={`${id}-firma`} value={kargoFirmasi} onChange={setKargoFirmasi} />
      </span>
      <span className="alan">
        <label htmlFor={`${id}-takip`}>Takip No</label>
        <input id={`${id}-takip`} className="girdi girdi--mono" maxLength={100} value={takipNo} onChange={(e) => setTakipNo(e.target.value)} />
      </span>
      <span className="dugme-grup">
        <button type="button" className="btn btn--kucuk" disabled={bekliyor}
          onClick={() => kaydet({ kargoFirmasi: kargoFirmasi.trim(), takipNo: takipNo.trim() })}>{bekliyor ? "Kaydediliyor…" : dugmeYazisi}</button>
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={vazgec}>Vazgeç</button>
      </span>
    </span>
  );
}

export function KargoOzeti({ g }: { g: GeriCagirma }) {
  const satir = (ad: string, firma?: string | null, takip?: string | null) =>
    firma || takip ? <span>{ad}: {[firma, takip].filter(Boolean).join(" · ")}</span> : null;
  const gelis = satir("Geliş kargosu", g.geriAlmaKargoFirmasi, g.geriAlmaTakipNo);
  const gidis = satir("Gönderim kargosu", g.geriGondermeKargoFirmasi, g.geriGondermeTakipNo);
  if (!gelis && !gidis) return null;
  return <span className="kargo-ozeti">{gelis}{gidis}</span>;
}
