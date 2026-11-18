import { SEVKIYAT_DURUM_ADI, type Sevkiyat } from "@/lib/api";

export const sevkiyatAdresi = (id: number) => `/sevkiyat?id=${id}`;

export function sevkiyatRozeti(s: Sevkiyat) {
  const renk = s.durum === "TeslimEdildi" ? "rozet--basari" : s.durum === "Kargoda" ? "rozet--mavi" : "rozet--uyari";
  return <span className={`rozet rozet--kucuk ${renk}`}>{SEVKIYAT_DURUM_ADI[s.durum]}</span>;
}

/** "Ürün 2 × 3, Ürün 1 × 1" */
export function urunOzeti(s: Sevkiyat) {
  const sayac = new Map<string, number>();
  for (const k of s.kalemler) sayac.set(k.urunAd, (sayac.get(k.urunAd) ?? 0) + 1);
  return [...sayac].map(([ad, n]) => `${ad} × ${n}`).join(", ");
}
