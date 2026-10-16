// Etikette basılacak kod: yok, çizgili barkod veya karekod. Son seçim tarayıcıda hatırlanır.

export type EtiketTuru = "yok" | "cizgili" | "karekod";

export const ETIKET_TURLERI: { deger: EtiketTuru; ad: string }[] = [
  { deger: "yok", ad: "Barkod yok" },
  { deger: "cizgili", ad: "Çizgili barkod" },
  { deger: "karekod", ad: "Karekod" },
];

const ANAHTAR = "etiket-turu";

export function etiketTuruMu(deger: string | null): deger is EtiketTuru {
  return ETIKET_TURLERI.some((t) => t.deger === deger);
}

export function kayitliEtiketTuru(): EtiketTuru {
  try {
    const t = localStorage.getItem(ANAHTAR);
    return etiketTuruMu(t) ? t : "cizgili";
  } catch {
    return "cizgili";
  }
}

export function etiketTuruKaydet(t: EtiketTuru) {
  try {
    localStorage.setItem(ANAHTAR, t);
  } catch {
    // hatırlanmazsa sorun değil
  }
}
