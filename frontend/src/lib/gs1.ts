// GS1 karekod (ilaç kutularındaki gibi): içinde birden fazla bilgi, her biri bir uygulama
// tanımlayıcısıyla (AI). Kullanılanlar: (240) ürün/parça kodu, (21) seri no, (10) lot/parti no,
// (11) üretim tarihi YYAAGG. Şirketin GS1 ürün numarası (GTIN) olursa (240) yerine (01) kullanılır.

export interface Gs1Alan { ai: string; deger: string }

const GS = "\u001d";

// Sabit uzunluklu AI'ler (değer uzunluğu); diğerleri değişken uzunlukta ve GS ile biter
const SABIT: Record<string, number> = { "00": 18, "01": 14, "02": 14, "11": 6, "13": 6, "15": 6, "17": 6, "20": 2 };
// Değişken uzunluklu, bilinen AI'ler (2–4 hane)
const DEGISKEN = ["10", "21", "22", "240", "241", "250", "30", "37", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99"];

/** bwip-js'in beklediği metin: "(240)IP(21)SN-IP-0001(11)260402" */
export function gs1Metni(alanlar: Gs1Alan[]) {
  return alanlar.map((a) => `(${a.ai})${a.deger}`).join("");
}

/** İstanbul saatine göre YYAAGG (GS1 tarih biçimi). */
export function gs1Tarih(iso: string) {
  const t = new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Istanbul" }).format(new Date(iso));
  return t.slice(2, 4) + t.slice(5, 7) + t.slice(8, 10);
}

/**
 * Okutulan metni GS1 alanlarına ayırır. GS1 değilse null.
 * Okuyucular başına "]d2" (DataMatrix) / "]Q3" (QR) / "]C1" ekleyebilir; alanlar GS karakteriyle ayrılır.
 * "(21)SN-1(10)L-1" gibi parantezli yazım da kabul edilir.
 */
export function gs1Coz(metin: string): Map<string, string> | null {
  let m = metin.trim();
  if (m.startsWith("(")) {
    const alanlar = new Map<string, string>();
    for (const e of m.matchAll(/\((\d{2,4})\)([^(]*)/g)) alanlar.set(e[1], e[2]);
    return alanlar.size ? alanlar : null;
  }
  const onekli = /^\][dQC][0-9]/.test(m);
  // Ayırıcısı düşmüş olsa da GTIN (01 + 14 hane) ile başlayan metin GS1'dir (tedarikçi kodları)
  const gtinIle = /^01\d{14}/.test(m);
  if (!onekli && !gtinIle && !m.includes(GS)) return null;
  if (onekli) m = m.slice(3);
  m = m.replace(/^\u001d+/, "");

  const alanlar = new Map<string, string>();
  while (m.length) {
    const sabit = Object.keys(SABIT).find((ai) => m.startsWith(ai));
    if (sabit) {
      alanlar.set(sabit, m.slice(2, 2 + SABIT[sabit]));
      m = m.slice(2 + SABIT[sabit]).replace(/^\u001d/, "");
      continue;
    }
    const ai = [...DEGISKEN].sort((a, b) => b.length - a.length).find((x) => m.startsWith(x));
    if (!ai) break;
    const son = m.indexOf(GS);
    alanlar.set(ai, son < 0 ? m.slice(ai.length) : m.slice(ai.length, son));
    m = son < 0 ? "" : m.slice(son + 1);
  }
  return alanlar.size ? alanlar : null;
}

/**
 * Okutulan ya da yazılan metinden aranan numarayı çıkarır: GS1 karekodsa seri (21) veya lot (10),
 * değilse metnin kendisi. tercih verilmezse önce seri, sonra lot.
 */
export function numaraAyikla(metin: string, tercih?: "seri" | "lot") {
  const alanlar = gs1Coz(metin);
  if (!alanlar) return metin.trim();
  const sira = tercih === "lot" ? ["10", "21"] : tercih === "seri" ? ["21", "10"] : ["21", "10"];
  for (const ai of sira) {
    const d = alanlar.get(ai);
    if (d) return d.trim();
  }
  return metin.trim();
}
