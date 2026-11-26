const tarihBicimi = new Intl.DateTimeFormat("tr-TR", { day: "2-digit", month: "2-digit", year: "numeric" });
const tarihSaatBicimi = new Intl.DateTimeFormat("tr-TR", {
  day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit", timeZone: "Europe/Istanbul",
});

export function tarih(deger: string) {
  const [y, a, g] = deger.slice(0, 10).split("-").map(Number);
  return tarihBicimi.format(new Date(y, a - 1, g));
}

export function tarihSaat(deger: string) {
  return tarihSaatBicimi.format(new Date(deger));
}

export function katla(s: string) {
  return s.toLocaleLowerCase("tr").replaceAll("ı", "i");
}

export function icerir(metin: string, aranan: string) {
  return katla(metin).includes(katla(aranan));
}

export function sayi(n: number) {
  return n.toLocaleString("tr-TR");
}

export function simdi() {
  return new Intl.DateTimeFormat("sv-SE", {
    timeZone: "Europe/Istanbul", year: "numeric", month: "2-digit", day: "2-digit", hour: "2-digit", minute: "2-digit",
  }).format(new Date()).replace(" ", "T");
}

export function istanbulZamani(deger: string) {
  return `${deger}:00+03:00`;
}

export function bugun() {
  return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Istanbul" }).format(new Date());
}

export const YAKIN_GUN = 60;

export function yakinMi(zaman: string | null | undefined, gun = YAKIN_GUN) {
  if (!zaman) return true;
  return Date.now() - new Date(zaman).getTime() <= gun * 86_400_000;
}
