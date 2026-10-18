// Ekranda gösterim biçimleri (tarih, sayı).

const tarihBicimi = new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "2-digit", year: "numeric" });
const tarihSaatBicimi = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit", timeZone: "Europe/Istanbul",
});

/** "2026-03-01" → "01.03.2026" (saat dilimi kaymadan). */
export function tarih(deger: string) {
  const [y, a, g] = deger.slice(0, 10).split("-").map(Number);
  return tarihBicimi.format(new Date(y, a - 1, g));
}

/** ISO zaman → "01.03.2026 14:05" (İstanbul saati). */
export function tarihSaat(deger: string) {
  return tarihSaatBicimi.format(new Date(deger));
}

/** Aramada i/ı/I/İ birbirinin yerine geçer ("indikatör" → "İndikatör"). */
export function katla(s: string) {
  return s.toLocaleLowerCase("tr").replaceAll("ı", "i");
}

/** metin, aranan'ı içeriyor mu (büyük/küçük harf ve i/ı/İ farkı gözetilmez). */
export function icerir(metin: string, aranan: string) {
  return katla(metin).includes(katla(aranan));
}

export function sayi(n: number) {
  return n.toLocaleString("tr-TR");
}

/** Bugünün tarihi (İstanbul) "YYYY-MM-DD" olarak; tarih alanlarının varsayılanı. */
export function bugun() {
  return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Istanbul" }).format(new Date());
}
