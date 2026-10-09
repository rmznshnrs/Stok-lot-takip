// Tüm API çağrıları. İstekler aynı adresteki /api/... yoluna gider; Next.js sunucusu
// oturum çerezindeki token'ı ekleyip .NET API'ye iletir.

// --- Veri tipleri (API'nin JSON'u) ----------------------------------------------

export type Rol = "Admin" | "Kullanici";

export interface Kullanici { id: number; ad: string; soyad: string; kullaniciAdi: string; rol: Rol }

export interface ParcaOzet { id: number; kod: string; ad: string; birim: string }

export interface Lot {
  id: number; lotNo: string; parcaId: number; parcaKod: string; parcaAd: string; tedarikciAd: string;
  siparisTarihi: string; siparisNo: string; girisAdet: number; kalanAdet: number; geriCagrildi: boolean;
  girisZamani: string; sirada: boolean;
}

export interface SatisBilgisi { satisId: number; tarih: string; musteriId: number; musteriAd: string; musteriIletisim: string }

export interface ParcaStok {
  id: number; kod: string; ad: string; birim: string; minStok: number;
  toplamStok: number; kullanilabilirStok: number; lotSayisi: number; kullanildigiUrunler: string[];
  minAltinda: boolean;
}

export interface StokGrubu { parca: ParcaStok; lotlar: Lot[]; siradaki: Lot | null }

export interface MalGirisiIstek {
  parcaId: number; tedarikciAdi: string; lotNo: string; adet: number; siparisTarihi: string; siparisNo?: string;
}
export interface MalGirisiSonucu { lot: Lot; yeniTedarikci: boolean }

export type DuzeltmeTuru = "SayimFarki" | "Fire" | "Diger";
export interface StokDuzeltmeIstek { stokLotuId: number; miktar: number; tur: DuzeltmeTuru; aciklama: string }
export interface StokDuzeltmeSonucu { id: number; lotNo: string; miktar: number; tur: string; aciklama: string; yeniKalanAdet: number }

export interface LotDusumu { lotId: number; lotNo: string; adet: number }
export interface ParcaIhtiyaci {
  parca: ParcaOzet; birimAdet: number; gereken: number; kullanilabilir: number;
  lotlar: LotDusumu[]; atlananLotlar: string[]; eksik: number;
}
export interface UretimOnizleme {
  urunId: number; urunKod: string; urunAd: string; adet: number;
  parcalar: ParcaIhtiyaci[]; seriNolar: string[]; eksikler: ParcaIhtiyaci[]; yeterli: boolean; atlananLotlar: string[];
}
export interface UretimSonucu { urunId: number; urunAd: string; seriNolar: string[] }

export interface LotIzUretim {
  uretimId: number; seriNo: string; tarih: string; urunKod: string; urunAd: string; adet: number; satis: SatisBilgisi | null;
}
export interface LotIzMusteri { id: number; ad: string; iletisim: string; satislar: { seriNo: string; tarih: string }[]; seriNolar: string[] }
export interface LotIz {
  lot: Lot; kullanilanAdet: number; uretimler: LotIzUretim[]; musteriler: LotIzMusteri[]; stoktakiUretimler: LotIzUretim[];
}
export interface SeriIzParca { parca: ParcaOzet; lot: Lot; adet: number }
export interface SeriIz {
  uretimId: number; seriNo: string; tarih: string; urunKod: string; urunAd: string;
  parcalar: SeriIzParca[]; satis: SatisBilgisi | null; geriCagrilanLotlar: string[];
}

export type NumaraTuru = "lot" | "seri" | "parca";
export interface Aday { tur: NumaraTuru; deger: string }
export interface NumaraArama { tur: NumaraTuru | null; deger: string | null; adaylar: Aday[] }

export interface Hareket {
  tur: "giris" | "uretim" | "satis"; tarih: string; aciklama: string; numara: string; numaraIlk: string; numaraTuru: "lot" | "seri";
}

export interface Satilabilir { uretimId: number; seriNo: string; urunAd: string; geriCagrilanLotlar: string[] }
export interface SatisUyarisi { seriNo: string; lotlar: string[] }
export interface SatisIstek { musteriAdi: string; seriNolar: string[]; tarih?: string; geriCagrilanOnayi?: boolean }
export interface SatisSonucu {
  satisId: number; musteriId: number; musteriAd: string; yeniMusteri: boolean; tarih: string;
  seriNolar: string[]; uyarilar: SatisUyarisi[];
}
export interface SatisGecmisi { satisId: number; tarih: string; musteriAd: string; urunAd: string; seriNolar: string[] }

export interface ParcaIstek { kod: string; ad: string; birim?: string; minStok?: number }
export interface UrunIstek { kod: string; ad: string; seriOneki: string }
export interface UrunAgaciSatiriIstek { parcaId: number; adet: number }
export interface UrunListe { id: number; kod: string; ad: string; seriOneki: string; parcaCesidi: number; uretimSayisi: number }
export interface UrunAgaciSatiri {
  parcaId: number; parcaKod: string; parcaAd: string; birim: string; adet: number; kullanilabilirStok: number; minAltinda: boolean;
}
export interface UrunDetay { id: number; kod: string; ad: string; seriOneki: string; seriOnekiDegistirilebilir: boolean; agac: UrunAgaciSatiri[] }
export interface Iletisim { id: number; ad: string; iletisim: string }

export interface KullaniciOlusturIstek { ad: string; soyad: string; kullaniciAdi: string; sifre: string; rol: Rol }
export interface KullaniciGuncelleIstek { ad: string; soyad: string; rol: Rol; yeniSifre?: string }

// --- Hata --------------------------------------------------------------------------

export interface EksikParca { parca: ParcaOzet; gereken: number; kullanilabilir: number; eksik: number }

/** API'nin ProblemDetails yanıtı. Mesaj kullanıcıya gösterilecek Türkçe metindir. */
export class ApiHatasi extends Error {
  constructor(
    readonly durum: number,
    mesaj: string,
    readonly eksikler: EksikParca[] = [],
    readonly uyarilar: SatisUyarisi[] = [],
    readonly alanHatalari: Record<string, string[]> = {},
  ) {
    super(mesaj);
    this.name = "ApiHatasi";
  }
}

// --- İstek ---------------------------------------------------------------------------

type Sorgu = Record<string, string | number | boolean | null | undefined>;

function adres(yol: string, sorgu?: Sorgu) {
  const p = new URLSearchParams();
  for (const [k, v] of Object.entries(sorgu ?? {})) {
    if (v !== undefined && v !== null && v !== "") p.set(k, String(v));
  }
  const s = p.toString();
  return `/api/${yol}${s ? `?${s}` : ""}`;
}

const parca = (deger: string) => encodeURIComponent(deger);

async function istek<T>(yontem: string, url: string, govde?: unknown): Promise<T> {
  let yanit: Response;
  try {
    yanit = await fetch(url, {
      method: yontem,
      headers: govde === undefined ? undefined : { "Content-Type": "application/json" },
      body: govde === undefined ? undefined : JSON.stringify(govde),
      cache: "no-store",
    });
  } catch {
    throw new ApiHatasi(0, "Sunucuya ulaşılamadı. Bağlantınızı kontrol edin.");
  }

  if (yanit.status === 401 && url !== "/api/oturum") {
    oturumDustu();
    throw new ApiHatasi(401, "Oturumunuz sona erdi. Lütfen yeniden giriş yapın.");
  }
  if (!yanit.ok) throw await hataOku(yanit);
  if (yanit.status === 204) return undefined as T;
  return (await yanit.json()) as T;
}

async function hataOku(yanit: Response): Promise<ApiHatasi> {
  try {
    const p = await yanit.json();
    const alanHatalari: Record<string, string[]> = p.errors ?? {};
    const ilkAlanHatasi = Object.values(alanHatalari).flat()[0];
    const mesaj = p.detail ?? ilkAlanHatasi ?? p.title ?? `İstek başarısız (${yanit.status}).`;
    return new ApiHatasi(yanit.status, mesaj, p.eksikler ?? [], p.uyarilar ?? [], alanHatalari);
  } catch {
    return new ApiHatasi(yanit.status, `İstek başarısız (${yanit.status}).`);
  }
}

function oturumDustu() {
  if (typeof window === "undefined" || window.location.pathname === "/giris") return;
  const sonraki = window.location.pathname + window.location.search;
  // eslint-disable-next-line @next/next/no-location-assign-relative-destination -- tam yenileme: bellekteki veriler temizlensin
  window.location.href = `/giris?sonraki=${encodeURIComponent(sonraki)}`;
}

const al = <T>(yol: string, sorgu?: Sorgu) => istek<T>("GET", adres(yol, sorgu));
const gonder = <T>(yol: string, govde?: unknown) => istek<T>("POST", adres(yol), govde ?? {});
const guncelle = <T>(yol: string, govde: unknown) => istek<T>("PUT", adres(yol), govde);

// --- Oturum ----------------------------------------------------------------------

export const oturum = {
  giris: (kullaniciAdi: string, sifre: string) =>
    istek<{ kullanici: Kullanici }>("POST", "/api/oturum", { kullaniciAdi, sifre }),
  cikis: () => istek<void>("DELETE", "/api/oturum"),
  ben: () => al<Kullanici>("kimlik/ben"),
  sifreDegistir: (eskiSifre: string, yeniSifre: string) => gonder<void>("kimlik/sifre", { eskiSifre, yeniSifre }),
};

// --- Tanımlar ----------------------------------------------------------------------

export const parcalar = {
  listele: (arama?: string) => al<ParcaStok[]>("parcalar", { arama }),
  bul: (metin: string) => al<ParcaOzet>("parcalar/bul", { metin }),
  olustur: (p: ParcaIstek) => gonder<ParcaOzet>("parcalar", p),
  guncelle: (id: number, p: ParcaIstek) => guncelle<ParcaOzet>(`parcalar/${id}`, p),
};

export const urunler = {
  listele: (arama?: string) => al<UrunListe[]>("urunler", { arama }),
  getir: (id: number) => al<UrunDetay>(`urunler/${id}`),
  olustur: (u: UrunIstek) => gonder<UrunDetay>("urunler", u),
  guncelle: (id: number, u: UrunIstek) => guncelle<UrunDetay>(`urunler/${id}`, u),
  agacKaydet: (id: number, satirlar: UrunAgaciSatiriIstek[]) => guncelle<UrunDetay>(`urunler/${id}/agac`, satirlar),
};

export const tedarikciler = () => al<Iletisim[]>("tedarikciler");
export const musteriler = () => al<Iletisim[]>("musteriler");

// --- Stok, üretim, satış, izleme ------------------------------------------------------

export const stok = {
  durum: (s: { arama?: string; parca?: string; bitenler?: boolean } = {}) => al<StokGrubu[]>("stok", s),
  giris: (g: MalGirisiIstek) => gonder<MalGirisiSonucu>("stok/giris", g),
  duzelt: (d: StokDuzeltmeIstek) => gonder<StokDuzeltmeSonucu>("stok/duzeltme", d),
};

export const uretim = {
  onizle: (urunId: number, adet: number) => al<UretimOnizleme>("uretim/onizleme", { urunId, adet }),
  uret: (urunId: number, adet: number) => gonder<UretimSonucu>("uretim", { urunId, adet }),
};

export const satis = {
  kontrol: (seriNo: string) => al<Satilabilir>("satis/kontrol", { seriNo }),
  uyarilar: (seriNolar: string[]) => gonder<SatisUyarisi[]>("satis/uyarilar", { seriNolar }),
  sat: (s: SatisIstek) => gonder<SatisSonucu>("satis", s),
  gecmis: (arama?: string) => al<SatisGecmisi[]>("satis/gecmis", { arama }),
};

export const izleme = {
  lot: (lotNo: string) => al<LotIz>(`izleme/lot/${parca(lotNo)}`),
  seri: (seriNo: string) => al<SeriIz>(`izleme/seri/${parca(seriNo)}`),
  ara: (q: string) => al<NumaraArama>("izleme/ara", { q }),
  hareketler: (adet = 8) => al<Hareket[]>("izleme/hareketler", { adet }),
  geriCagir: (lotNo: string, geriCagir: boolean) => gonder<Lot>(`izleme/lot/${parca(lotNo)}/geri-cagir`, { geriCagir }),
};

// --- Kullanıcılar (Admin) -----------------------------------------------------------

export const kullanicilar = {
  listele: () => al<Kullanici[]>("kullanicilar"),
  olustur: (k: KullaniciOlusturIstek) => gonder<Kullanici>("kullanicilar", k),
  guncelle: (id: number, k: KullaniciGuncelleIstek) => guncelle<Kullanici>(`kullanicilar/${id}`, k),
};
