// Tüm API çağrıları. İstekler aynı adresteki /api/... yoluna gider; Next.js sunucusu
// oturum çerezindeki token'ı ekleyip .NET API'ye iletir.

// --- Veri tipleri (API'nin JSON'u) ----------------------------------------------

export type Rol = "Admin" | "Kullanici";
/** Ekranda gösterilen rol adı (Kullanici rolü "Çalışan" olarak görünür). */
export const rolAdi = (r: Rol) => (r === "Admin" ? "Yönetici" : "Çalışan");

export interface Kullanici { id: number; ad: string; soyad: string; kullaniciAdi: string; rol: Rol; aktif?: boolean }

/** Parçalar adetle sayılır. */
export interface ParcaOzet { id: number; kod: string; ad: string }

export interface Lot {
  id: number; lotNo: string; parcaId: number; parcaKod: string; parcaAd: string; tedarikciAd: string;
  siparisTarihi: string; siparisNo: string; girisAdet: number; kalanAdet: number;
  /** Üretime kapalı (hatalı lotta kalan parçalar kullanılmasın seçildi). */
  geriCagrildi: boolean;
  sorunlu: boolean; sorunAciklama: string;
  girisZamani: string; sirada: boolean;
  /** Tedarikçinin pakete yazdığı lot no (lotNo: sistemin verdiği stok giriş no). */
  tedarikciLotNo: string;
}

export interface SatisBilgisi { satisId: number; tarih: string; musteriId: number; musteriAd: string; musteriIletisim: string }

export interface ParcaStok {
  id: number; kod: string; ad: string; minStok: number;
  toplamStok: number; kullanilabilirStok: number; lotSayisi: number; kullanildigiUrunler: string[];
  /** Hatalı kaydedilen parça adedi (stoktan düşülen + yalnız sayılan). */
  hataliAdet: number;
  /** Sıradaki üretimlere ayrılan adet; serbest: kullanılabilirden ayrılmamış olanı. */
  ayrilan: number; serbest: number;
  minAltinda: boolean;
}

export interface StokGrubu { parca: ParcaStok; lotlar: Lot[]; siradaki: Lot | null }

export interface MalGirisiIstek {
  parcaId: number; tedarikciAdi: string; adet: number; siparisTarihi: string; siparisNo?: string;
  /** Boş bırakılırsa stok giriş no sistemce verilir (SG-0001). */
  lotNo?: string; tedarikciLotNo?: string;
}
export interface MalGirisiSonucu { lot: Lot; yeniTedarikci: boolean }

export type DuzeltmeTuru = "SayimFarki" | "Fire" | "Diger";
export interface StokDuzeltmeIstek { stokLotuId: number; miktar: number; tur: DuzeltmeTuru; aciklama: string }
/** stoktanDus=false: yalnız hatalı sayısı kaydedilir, stok değişmez. */
export interface StokHataliIstek { stokLotuId: number; adet: number; stoktanDus: boolean; aciklama: string }
export interface StokDuzeltmeSonucu { id: number; lotNo: string; miktar: number; tur: string; aciklama: string; yeniKalanAdet: number }

export interface LotDusumu { lotId: number; lotNo: string; adet: number; sorunlu: boolean }
export interface ParcaIhtiyaci {
  parca: ParcaOzet; birimAdet: number; gereken: number; kullanilabilir: number;
  lotlar: LotDusumu[]; atlananLotlar: string[]; eksik: number;
}
export interface UretimOnizleme {
  urunId: number; urunKod: string; urunAd: string; adet: number;
  parcalar: ParcaIhtiyaci[]; seriNolar: string[]; eksikler: ParcaIhtiyaci[]; yeterli: boolean; atlananLotlar: string[];
  sorunluKullanilanLotlar: string[];
}
export interface UretimSonucu { urunId: number; urunAd: string; seriNolar: string[] }

export interface LotIzUretim {
  uretimId: number; seriNo: string; tarih: string; urunKod: string; urunAd: string; adet: number; satis: SatisBilgisi | null;
  hurdaTuru: HurdaTuru | null;
  /** "acik": açık geri çağırması var. */
  geriCagirmaDurumu: string | null;
}
export interface LotIzMusteri { id: number; ad: string; iletisim: string; satislar: { seriNo: string; tarih: string }[]; seriNolar: string[] }
export interface LotIz {
  lot: Lot; kullanilanAdet: number; uretimler: LotIzUretim[]; musteriler: LotIzMusteri[]; stoktakiUretimler: LotIzUretim[];
}
export interface SeriIzParca { parca: ParcaOzet; lot: Lot; adet: number }
export interface SeriIz {
  uretimId: number; seriNo: string; tarih: string; urunKod: string; urunAd: string;
  parcalar: SeriIzParca[]; satis: SatisBilgisi | null; geriCagrilanLotlar: string[]; hurda: HurdaBilgisi | null; iadeler: IadeBilgisi[];
  geriCagirma: GeriCagirma | null;
}

/** lot: stok giriş no. */
export type NumaraTuru = "lot" | "seri" | "parca";
export interface Aday { tur: NumaraTuru; deger: string }
export interface NumaraArama { tur: NumaraTuru | null; deger: string | null; adaylar: Aday[] }

export interface Hareket {
  tur: "giris" | "uretim" | "satis"; tarih: string; aciklama: string; numara: string; numaraIlk: string; numaraTuru: "lot" | "seri";
}

export interface UretimGecmisi {
  uretimId: number; seriNo: string; urunId: number; urunAd: string; tarih: string;
  musteriAd: string | null; satisTarihi: string | null; geriCagrilanLotlar: string[]; hurda: HurdaBilgisi | null;
  geriCagirmaDurumu: string | null;
}
export type HurdaTuru = "Hurda" | "Kayip";
export interface HurdaBilgisi { tur: HurdaTuru; aciklama: string; zaman: string; kullaniciAd: string }
export interface UretimHurdaIstek { seriNolar: string[]; tur: HurdaTuru; aciklama: string }
export interface UretimHurdaSonucu { seriNolar: string[]; tur: HurdaTuru; aciklama: string }
/** "Kayip" → "Kayıp" */
export const hurdaAdi = (t: HurdaTuru) => (t === "Kayip" ? "Kayıp" : "Hurda");
export interface Satilabilir { uretimId: number; seriNo: string; urunAd: string; geriCagrilanLotlar: string[] }
export interface SatisUyarisi { seriNo: string; lotlar: string[] }
export interface SatisIstek { musteriAdi: string; seriNolar: string[]; tarih?: string; zaman?: string; geriCagrilanOnayi?: boolean }
export interface SatisSonucu {
  satisId: number; musteriId: number; musteriAd: string; yeniMusteri: boolean; tarih: string;
  seriNolar: string[]; uyarilar: SatisUyarisi[];
}
/** seriNolar: hâlâ satılmış olanlar; iadeEdilenler: bu satıştan iade alınanlar. */
export interface SatisGecmisi {
  satisId: number; tarih: string; musteriAd: string; urunAd: string; seriNolar: string[]; iadeEdilenler: string[];
  kalemler: { seriNo: string }[] | null;
  /** Satışın saati; saat tutulmaya başlanmadan önceki satışlarda null. */
  zaman: string | null;
}
export interface IadeBilgisi { musteriAd: string; satisTarihi: string; iadeZamani: string; aciklama: string; kullaniciAd: string }
export type IslemTuru = "giris" | "uretim" | "satis" | "iade" | "duzeltme" | "hurda" | "sorun" | "geri";
export interface GeriCagirma {
  id: number; seriNo: string; urunAd: string; lotNo: string | null; aciklama: string; zaman: string; kullaniciAd: string;
  durum: "musteride" | "geri_alindi" | "depoda" | "hurda" | "satisa_acildi" | "musteriye_verildi";
  musteriAd: string | null; musteriIletisim: string | null; geriAlinmaZamani: string | null; kapanmaZamani: string | null;
  bilgiVerildiZamani: string | null;
}
export interface Islem { zaman: string; tur: IslemTuru; kullaniciAd: string; aciklama: string; numara: string | null; numaraTuru: "lot" | "seri" | null }

/** Üretim sırasındaki iş. durum: Bekliyor, Tamamlandi, Iptal. parcalar[].ayrilan: kalan üretim için ayrılan. */
export interface UretimEmri {
  id: number; sira: number; urunId: number; urunKod: string; urunAd: string; adet: number; uretilen: number;
  durum: "Bekliyor" | "Tamamlandi" | "Iptal"; planlananTarih: string; aciklama: string;
  siparisId: number | null; siparisNo: string | null; musteriAd: string | null;
  olusturmaZamani: string; kullaniciAd: string; bitisZamani: string | null;
  parcalar: { parca: ParcaOzet; birimAdet: number; ayrilan: number }[]; seriNolar: string[];
}
/** siparisKalemiId verilip adet verilmezse kalemin eksiği kadar. */
export interface UretimEmriIstek { urunId: number; adet?: number; siparisKalemiId?: number; planlananTarih?: string; aciklama?: string }

export type SiparisDurumu = "bekliyor" | "uretimde" | "hazir" | "kismen_gonderildi" | "tamamlandi" | "iptal";
export const SIPARIS_DURUM_ADI: Record<SiparisDurumu, string> = {
  bekliyor: "Bekliyor", uretimde: "Üretimde", hazir: "Gönderime Hazır", kismen_gonderildi: "Kısmen Gönderildi",
  tamamlandi: "Tamamlandı", iptal: "İptal Edildi",
};
/** ayrilanlar: bu kaleme ayrılan seriler (gönderilenler dahil); uretimde: sıradaki adet; eksik: ne ayrılan ne sırada. */
export interface SiparisKalemi {
  id: number; urunId: number; urunKod: string; urunAd: string; adet: number;
  ayrilanlar: string[]; gonderilenler: string[]; uretimde: number; eksik: number;
}
export interface Siparis {
  id: number; no: string; musteriId: number; musteriAd: string; tarih: string; istenenTarih: string | null; aciklama: string;
  durum: SiparisDurumu; kalemler: SiparisKalemi[]; olusturmaZamani: string; kullaniciAd: string; iptalZamani: string | null;
}
export interface SiparisIstek {
  musteriId: number; kalemler: { urunId: number; adet: number; seriNolar?: string[] }[];
  tarih?: string; istenenTarih?: string; aciklama?: string;
}
export interface HazirSeri { seriNo: string; tarih: string }

export type SevkiyatDurumu = "Hazirlaniyor" | "Kargoda" | "TeslimEdildi";
export const SEVKIYAT_DURUM_ADI: Record<SevkiyatDurumu, string> = {
  Hazirlaniyor: "Kargoya Verilecek", Kargoda: "Kargoda", TeslimEdildi: "Teslim Edildi",
};
/** Sevkiyat (sevk edilen ürün satılmış sayılır). iadeler: bu sevkiyattan iade alınan seri numaraları. */
export interface Sevkiyat {
  id: number; no: string; musteriId: number; musteriAd: string; siparisId: number | null; siparisNo: string | null;
  tarih: string; zaman: string | null; durum: SevkiyatDurumu; kargoFirmasi: string; takipNo: string; aciklama: string;
  kargoZamani: string | null; teslimZamani: string | null; olusturmaZamani: string; kullaniciAd: string;
  kalemler: { seriNo: string; urunAd: string }[]; iadeler: string[];
}
export interface SevkiyatIstek {
  siparisId: number; seriNolar: string[]; kargoFirmasi?: string; takipNo?: string; aciklama?: string; geriCagrilanOnayi?: boolean;
}

/** Serbest parça stoğuyla en fazla uretilebilir adet üretilebilen ürün; kisitlayanlar: buna yol açan parçalar. */
export interface UretilebilirUyari {
  urunId: number; urunKod: string; urunAd: string; uretilebilir: number;
  kisitlayanlar: { parca: ParcaOzet; birimAdet: number; serbest: number }[];
}
export interface AnaSayfaOzeti {
  acikSiparis: number; siradakiIs: number; siradakiAdet: number; bugunPlanlanan: number;
  hazirlananSevkiyat: number; kargodaSevkiyat: number; depodaUrun: number; minAltindaParca: number; acikGeriCagirma: number;
  uyarilar: UretilebilirUyari[];
  yaklasanTeslimler: { siparisId: number; no: string; musteriAd: string; istenenTarih: string; kalan: number }[];
  /** Ürün ağacı olan her ürün için parça stoğuyla üretilebilecek adet. */
  kapasiteler: { urunId: number; urunAd: string; uretilebilir: number }[];
}
/** planlanan: sıradaki işlerin o güne planlanan adedi; uretilen: o gün üretilen; istenen: o gün teslimi istenen sipariş adedi. */
export interface TakvimGunu {
  tarih: string; urunler: { urunId: number; urunAd: string; planlanan: number; uretilen: number; istenen: number }[];
  planlar: TakvimPlani[];
}
/** Parça alımı hatırlatıcısı (parca dolu) ya da not. */
export interface TakvimPlani {
  id: number; tarih: string; parca: ParcaOzet | null; adet: number | null; tedarikciId: number | null; tedarikciAd: string | null;
  aciklama: string; tamamlandi: boolean; kullaniciAd: string;
}
export interface TakvimPlaniIstek { tarih: string; parcaId?: number; adet?: number; tedarikciId?: number; aciklama?: string }

export interface ParcaIstek { kod: string; ad: string; minStok?: number }
/** Seri no öneki gönderilmez: yeni üründe ürün kodundan oluşur (U1 → U1-0001), sonra değişmez. */
export interface UrunIstek { kod: string; ad: string }
export interface UrunAgaciSatiriIstek { parcaId: number; adet: number }
export interface UrunListe { id: number; kod: string; ad: string; seriOneki: string; parcaCesidi: number; uretimSayisi: number }
export interface UrunAgaciSatiri {
  parcaId: number; parcaKod: string; parcaAd: string; adet: number; kullanilabilirStok: number; minAltinda: boolean;
}
export interface UrunDetay { id: number; kod: string; ad: string; seriOneki: string; seriOnekiDegistirilebilir: boolean; agac: UrunAgaciSatiri[] }
/** iletisim: telefon · e-posta · adres tek satırda (görüntüleme için). */
export interface Iletisim { id: number; ad: string; iletisim: string; telefon: string; eposta: string; adres: string }
export interface IletisimIstek { ad: string; telefon: string; eposta: string; adres: string }

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
const sil = <T>(yol: string) => istek<T>("DELETE", adres(yol));

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

export const firmalar = {
  musteriler,
  tedarikciler,
  musteriEkle: (f: IletisimIstek) => gonder<Iletisim>("musteriler", f),
  musteriGuncelle: (id: number, f: IletisimIstek) => guncelle<Iletisim>(`musteriler/${id}`, f),
  tedarikciEkle: (f: IletisimIstek) => gonder<Iletisim>("tedarikciler", f),
  tedarikciGuncelle: (id: number, f: IletisimIstek) => guncelle<Iletisim>(`tedarikciler/${id}`, f),
};

export const islemler = {
  liste: (s: { tur?: IslemTuru; kullaniciId?: number; once?: string; adet?: number } = {}) => al<Islem[]>("islemler", s),
};

// --- Stok, üretim, satış, izleme ------------------------------------------------------

export const stok = {
  durum: (s: { arama?: string; parca?: string; bitenler?: boolean } = {}) => al<StokGrubu[]>("stok", s),
  giris: (g: MalGirisiIstek) => gonder<MalGirisiSonucu>("stok/giris", g),
  duzelt: (d: StokDuzeltmeIstek) => gonder<StokDuzeltmeSonucu>("stok/duzeltme", d),
  hatali: (h: StokHataliIstek) => gonder<StokDuzeltmeSonucu>("stok/hatali", h),
};

export const uretim = {
  onizle: (urunId: number, adet: number) => al<UretimOnizleme>("uretim/onizleme", { urunId, adet }),
  gecmis: (arama?: string, urunId?: number) => al<UretimGecmisi[]>("uretim/gecmis", { arama, urunId }),
  hurda: (h: UretimHurdaIstek) => gonder<UretimHurdaSonucu>("uretim/hurda", h),
};

export const sira = {
  liste: (bitenler = false) => al<UretimEmri[]>("uretim/sira", { bitenler }),
  ekle: (e: UretimEmriIstek) => gonder<UretimEmri>("uretim/sira", e),
  /** adet verilmezse kalanın hepsi üretilir. */
  uret: (id: number, adet?: number) => gonder<UretimSonucu>(`uretim/sira/${id}/uret`, { adet }),
  iptal: (id: number) => gonder<UretimEmri>(`uretim/sira/${id}/iptal`),
  tasi: (id: number, yon: -1 | 1) => gonder<UretimEmri[]>(`uretim/sira/${id}/tasi`, { yon }),
  planla: (id: number, planlananTarih: string) => guncelle<UretimEmri>(`uretim/sira/${id}/plan`, { planlananTarih }),
};

export const siparisler = {
  liste: (musteriId?: number, acik = false) => al<Siparis[]>("siparis", { musteriId, acik }),
  getir: (id: number) => al<Siparis>(`siparis/${id}`),
  olustur: (s: SiparisIstek) => gonder<Siparis>("siparis", s),
  iptal: (id: number) => gonder<Siparis>(`siparis/${id}/iptal`),
  ayir: (kalemId: number, seriNolar: string[]) => gonder<Siparis>(`siparis/kalem/${kalemId}/ayir`, { seriNolar }),
  birak: (kalemId: number, seriNo: string) => sil<Siparis>(`siparis/kalem/${kalemId}/seri/${parca(seriNo)}`),
  hazir: (urunId: number) => al<HazirSeri[]>("siparis/hazir", { urunId }),
};

export const sevkiyat = {
  liste: (arama?: string, durum?: SevkiyatDurumu, musteriId?: number) => al<Sevkiyat[]>("sevkiyat", { arama, durum, musteriId }),
  getir: (id: number) => al<Sevkiyat>(`sevkiyat/${id}`),
  olustur: (s: SevkiyatIstek) => gonder<Sevkiyat>("sevkiyat", s),
  kargo: (id: number, kargoFirmasi: string, takipNo: string) => guncelle<Sevkiyat>(`sevkiyat/${id}/kargo`, { kargoFirmasi, takipNo }),
  teslim: (id: number) => gonder<Sevkiyat>(`sevkiyat/${id}/teslim`),
};

export const ozet = {
  anaSayfa: () => al<AnaSayfaOzeti>("ozet"),
  /** baslangic, bitis: "YYYY-MM-DD" (en çok 62 gün). */
  takvim: (baslangic: string, bitis: string) => al<TakvimGunu[]>("ozet/takvim", { baslangic, bitis }),
  /** Parça alımı hatırlatıcısı ya da not; stoğa dokunmaz. */
  planEkle: (p: TakvimPlaniIstek) => gonder<TakvimPlani>("ozet/takvim/plan", p),
  planTamam: (id: number, tamamlandi: boolean) => gonder<TakvimPlani>(`ozet/takvim/plan/${id}/tamam?tamamlandi=${tamamlandi}`),
  planSil: (id: number) => sil<void>(`ozet/takvim/plan/${id}`),
};

export const satis = {
  kontrol: (seriNo: string) => al<Satilabilir>("satis/kontrol", { seriNo }),
  uyarilar: (seriNolar: string[]) => gonder<SatisUyarisi[]>("satis/uyarilar", { seriNolar }),
  sat: (s: SatisIstek) => gonder<SatisSonucu>("satis", s),
  gecmis: (arama?: string, musteriId?: number) => al<SatisGecmisi[]>("satis/gecmis", { arama, musteriId }),
  iade: (satisId: number, seriNolar: string[], aciklama: string) =>
    gonder<{ satisId: number; seriNolar: string[] }>(`satis/${satisId}/iade`, { seriNolar, aciklama }),
};

export const izleme = {
  lot: (lotNo: string) => al<LotIz>(`izleme/lot/${parca(lotNo)}`),
  seri: (seriNo: string) => al<SeriIz>(`izleme/seri/${parca(seriNo)}`),
  ara: (q: string) => al<NumaraArama>("izleme/ara", { q }),
  hareketler: (adet = 8) => al<Hareket[]>("izleme/hareketler", { adet }),
  /** Yalnız yönetici: stok girişini hatalı işaretler / hatayı kaldırır. Yalnız seçilen ürünler geri çağrılır. */
  sorun: (lotNo: string, s: { sorunlu: boolean; uretimdeKullan: boolean; aciklama: string; geriCagrilacaklar?: string[] }) =>
    gonder<{ lot: Lot; geriCagrilanSeriNolar: string[] }>(`izleme/lot/${parca(lotNo)}/sorun`, s),
};

export const geriCagirma = {
  liste: (acik = false, arama?: string) => al<GeriCagirma[]>("geri-cagirma", { acik, arama }),
  geriCagir: (seriNolar: string[], aciklama: string, lotNo?: string) => gonder<GeriCagirma[]>("geri-cagirma", { seriNolar, aciklama, lotNo }),
  geriAl: (id: number) => gonder<GeriCagirma>(`geri-cagirma/${id}/geri-al`),
  satisaAc: (id: number) => gonder<GeriCagirma>(`geri-cagirma/${id}/satisa-ac`),
  musteriyeVer: (id: number) => gonder<GeriCagirma>(`geri-cagirma/${id}/musteriye-ver`),
  bilgiVerildi: (id: number) => gonder<GeriCagirma>(`geri-cagirma/${id}/bilgi-verildi`),
  /** Müşterideyse önce iade alınır, sonra hurdaya ayrılır. */
  hurda: (id: number, aciklama: string) => gonder<GeriCagirma>(`geri-cagirma/${id}/hurda`, { aciklama }),
};

// --- Kullanıcılar (Admin) -----------------------------------------------------------

export const kullanicilar = {
  listele: () => al<Kullanici[]>("kullanicilar"),
  olustur: (k: KullaniciOlusturIstek) => gonder<Kullanici>("kullanicilar", k),
  guncelle: (id: number, k: KullaniciGuncelleIstek) => guncelle<Kullanici>(`kullanicilar/${id}`, k),
  /** İşlemi yoksa siler (silindi: true), varsa pasif yapar. */
  sil: (id: number) => sil<{ silindi: boolean }>(`kullanicilar/${id}`),
  etkinlestir: (id: number) => gonder<Kullanici>(`kullanicilar/${id}/etkinlestir`),
};

// --- Hesap: kayıt ol, şifremi unuttum, e-posta, kayıt onayı ---------------------------

export interface OnayBekleyen { id: number; ad: string; soyad: string; kullaniciAdi: string; eposta: string | null }

export const hesap = {
  kayit: (k: { ad: string; soyad: string; kullaniciAdi: string; eposta: string; sifre: string }) =>
    gonder<OnayBekleyen>("hesap/kayit", k),
  sifreUnuttum: (kullaniciAdiVeyaEposta: string) => gonder<void>("hesap/sifre-unuttum", { kullaniciAdiVeyaEposta }),
  sifreSifirla: (kod: string, yeniSifre: string) => gonder<void>("hesap/sifre-sifirla", { kod, yeniSifre }),
  eposta: () => al<{ eposta: string | null }>("hesap/eposta"),
  epostaAyarla: (eposta: string) => guncelle<{ eposta: string | null }>("hesap/eposta", { eposta }),
  onayBekleyenler: () => al<OnayBekleyen[]>("hesap/onay-bekleyenler"),
  onayla: (id: number) => gonder<void>(`hesap/${id}/onayla`),
  reddet: (id: number) => istek<void>("DELETE", adres(`hesap/${id}`)),
};
