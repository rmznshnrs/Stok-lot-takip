"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiHatasi, izleme, ozet as ozetApi, type Aday, type NumaraArama, type NumaraTuru, type AnaSayfaOzeti } from "@/lib/api";
import { lotAdresi, seriAdresi } from "@/lib/adres";
import { bugun, tarih } from "@/lib/bicim";
import { numaraAyikla } from "@/lib/gs1";
import { useVeri } from "@/lib/kancalar";

function izAdresi(tur: NumaraTuru, deger: string) {
  const d = encodeURIComponent(deger);
  if (tur === "lot") return lotAdresi(deger);
  if (tur === "seri") return seriAdresi(deger);
  return `/stok?parca=${d}`;
}

const HIZLI = [
  { yol: "/stok?giris=1", baslik: "Mal girişi" },
  { yol: "/uretim?yeni=1", baslik: "Sıraya ekle" },
  { yol: "/sevkiyat?yeni=1", baslik: "Yeni sevkiyat" },
];

const HAREKET_ADI = { giris: "Mal girişi", uretim: "Üretim", satis: "Sevkiyat" } as const;


export default function AnaSayfaEkrani() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [sonuc, setSonuc] = useState<{ q: string; arama: NumaraArama } | null>(null);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const { veri: hareketler, hata: hareketHatasi } = useVeri(() => izleme.hareketler(8), []);
  const { veri: o, hata: ozetHatasi } = useVeri(() => ozetApi.anaSayfa(), []);
  const ozet = [
    { ad: "Açık sipariş", deger: o?.acikSiparis, yol: "/takvim", dikkat: false },
    { ad: "Üretim sırasındaki ürün", deger: o?.siradakiAdet, alt: o && `${o.siradakiIs} iş · bugüne kadar planlanan ${o.bugunPlanlanan}`, yol: "/uretim", dikkat: false },
    { ad: "Gönderilmeyi bekleyen sevkiyat", deger: o && o.hazirlananSevkiyat + o.kargodaSevkiyat, alt: o && `${o.hazirlananSevkiyat} hazırlanıyor · ${o.kargodaSevkiyat} kargoda`, yol: "/sevkiyat", dikkat: false },
    { ad: "Depodaki ürün", deger: o?.depodaUrun, yol: "/stok?gorunum=urun", dikkat: false },
    { ad: "Minimum stok altındaki parça", deger: o?.minAltindaParca, yol: "/stok?gorunum=parca", dikkat: true },
    { ad: "Açık geri çağırma", deger: o?.acikGeriCagirma, yol: "/musteriler?sekme=geri", dikkat: true },
  ];

  function ara(e: FormEvent) {
    e.preventDefault();
    aramaYap(q);
  }

  async function aramaYap(ham: string) {
    const metin = numaraAyikla(ham);
    if (!metin) return;
    setHata("");
    setBekliyor(true);
    try {
      const arama = await izleme.ara(metin);
      if (arama.tur && arama.deger) return router.push(izAdresi(arama.tur, arama.deger));
      setSonuc({ q: metin, arama });
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Arama yapılamadı.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <main className="icerik icerik--dikey">
      <section className="ana-ust">
        <div className="arama-bolumu">
          <form className="ana-arama" role="search" onSubmit={ara}>
            <input
              type="search"
              className="girdi"
              aria-label="Lot, seri no veya parça kodu"
              autoFocus
              value={q}
              onChange={(e) => setQ(e.target.value)}
            />
            <button type="submit" className="btn btn--ikincil" disabled={bekliyor}>Ara</button>
          </form>
          {hata ? (
            <div className="mesaj mesaj--error">{hata}</div>
          ) : sonuc?.arama.adaylar.length ? (
            <div className="aday-kutu">
              <div className="not">Benzer sonuçlar:</div>
              <div className="seri-listesi">
                {sonuc.arama.adaylar.map((a: Aday) => (
                  <Link key={`${a.tur}-${a.deger}`} href={izAdresi(a.tur, a.deger)} className="seri-cip seri-cip--mavi">
                    <span className="cip-tur">{a.tur === "lot" ? "stok giriş" : a.tur === "seri" ? "seri"  : "parça"}</span> {a.deger}
                  </Link>
                ))}
              </div>
            </div>
          ) : sonuc ? (
            <div className="mesaj mesaj--warning">“{sonuc.q}” ile eşleşen lot, seri no veya parça kodu bulunamadı.</div>
          ) : null}
        </div>
        <div className="dugme-grup">
          {HIZLI.map((h) => (
            <Link key={h.yol} href={h.yol} className="btn btn--ikincil">{h.baslik}</Link>
          ))}
        </div>
      </section>

      {ozetHatasi && <div className="mesaj mesaj--error">{ozetHatasi}</div>}
      <section className="ozet-kutular">
        {ozet.map((o) => (
          <Link key={o.ad} href={o.yol} className={`ozet-kutu${o.dikkat && o.deger ? " ozet-kutu--dikkat" : ""}`}>
            <span>{o.ad}</span>
            <b>{o.deger ?? "…"}</b>
            {o.alt && <small className="yardim">{o.alt}</small>}
          </Link>
        ))}
      </section>

      {o && <UretimUyarilari ozet={o} />}
      {o && o.yaklasanTeslimler.length > 0 && <YaklasanTeslimler ozet={o} />}

      <section className="panel">
        <div className="panel-bas"><h3>Son hareketler</h3></div>
        {hareketHatasi && <div className="panel-ic"><div className="mesaj mesaj--error">{hareketHatasi}</div></div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th style={{ width: 120 }}>Tarih</th>
                <th style={{ width: 120 }}>İşlem</th>
                <th>Açıklama</th>
                <th className="sag">Numara</th>
              </tr>
            </thead>
            <tbody>
              {hareketler === null && !hareketHatasi && <tr className="bos"><td colSpan={4}>Yükleniyor…</td></tr>}
              {hareketler?.length === 0 && <tr className="bos"><td colSpan={4}>Henüz hareket yok.</td></tr>}
              {hareketler?.map((h, i) => (
                <tr key={i}>
                  <td>{tarih(h.tarih)}</td>
                  <td><span className={`rozet rozet--kucuk ${h.tur === "giris" ? "rozet--basari" : h.tur === "uretim" ? "rozet--mavi" : "rozet--uyari"}`}>{HAREKET_ADI[h.tur]}</span></td>
                  <td>{h.aciklama}</td>
                  <td className="sag mono"><Link href={izAdresi(h.numaraTuru, h.numaraIlk)}>{h.numara}</Link></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}

/** Parça stoğuyla en fazla 3 adet üretilebilen ürünler (sıradaki işlere ayrılan parçalar düşülmüş). */
function UretimUyarilari({ ozet: o }: { ozet: AnaSayfaOzeti }) {
  return (
    <section className={`panel${o.uyarilar.length ? " panel--dikkat" : ""}`}>
      <div className="panel-bas">
        <h3>Üretim Uyarıları</h3>
        {o.uyarilar.length > 0 && <span className="rozet rozet--kucuk rozet--uyari">{o.uyarilar.length} ürün</span>}
      </div>
      <div className="panel-ic form-dikey">
        {o.uyarilar.length === 0 ? (
          <p className="not">Bütün ürünler için en az 4 adetlik parça var.</p>
        ) : (
          o.uyarilar.map((u) => (
            <div key={u.urunId} className="bas-satir uyari-satiri">
              <span>
                <strong>{u.urunAd}</strong>{" "}
                <span className={u.uretilebilir === 0 ? "uyari-metin" : undefined}>
                  {u.uretilebilir === 0 ? "parça stoğuyla hiç üretilemiyor" : `parça stoğuyla en fazla ${u.uretilebilir} adet üretilebilir`}
                </span>
                <span className="yardim">
                  {" · "}
                  {u.kisitlayanlar.map((k) => `${k.parca.ad}: ${k.serbest} ${k.parca.birim} serbest (1 üründe ${k.birimAdet})`).join(" · ")}
                </span>
              </span>
              <Link href={`/stok?parca=${encodeURIComponent(u.kisitlayanlar[0]?.parca.kod ?? "")}`} className="btn btn--ikincil btn--kucuk">Parça Stoğu</Link>
            </div>
          ))
        )}
      </div>
    </section>
  );
}

/** 7 gün içinde ya da geçmişte teslimi istenip henüz tamamı gönderilmemiş siparişler. */
function YaklasanTeslimler({ ozet: o }: { ozet: AnaSayfaOzeti }) {
  const bugunMetni = bugun();
  return (
    <section className="panel">
      <div className="panel-bas"><h3>Yaklaşan Teslimler</h3></div>
      <div className="tablo-kap">
        <table className="tablo">
          <thead>
            <tr>
              <th>Teslim</th>
              <th>Sipariş</th>
              <th>Müşteri</th>
              <th className="sag">Gönderilecek</th>
            </tr>
          </thead>
          <tbody>
            {o.yaklasanTeslimler.map((t) => (
              <tr key={t.siparisId} className={t.istenenTarih < bugunMetni ? "uyari-satir" : undefined}>
                <td>{tarih(t.istenenTarih)}{t.istenenTarih < bugunMetni && <span className="uyari-metin"> · gecikti</span>}</td>
                <td className="mono"><Link href={`/musteriler?siparis=${t.siparisId}`}>{t.no}</Link></td>
                <td>{t.musteriAd}</td>
                <td className="sag">{t.kalan}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
