"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import GeriCagirmalar from "@/components/GeriCagirmalar";
import { OnumuzdekiGunler, SiradakiIsler, UretimUyarilari, YaklasanTeslimler } from "./AnaSayfaPanolari";
import { ApiHatasi, izleme, ozet as ozetApi, type Aday, type NumaraArama, type NumaraTuru } from "@/lib/api";
import { lotAdresi, seriAdresi } from "@/lib/adres";
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

export default function AnaSayfaEkrani() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [sonuc, setSonuc] = useState<{ q: string; arama: NumaraArama } | null>(null);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const [geriAcik, setGeriAcik] = useState(false);
  const { veri: o, hata: ozetHatasi } = useVeri(() => ozetApi.anaSayfa(), []);
  const ozet = [
    { ad: "Açık sipariş", deger: o?.acikSiparis, yol: "/takvim", dikkat: false },
    { ad: "Üretim sırasındaki ürün", deger: o?.siradakiAdet, alt: o && `${o.siradakiIs} iş · bugüne kadar planlanan ${o.bugunPlanlanan}`, yol: "/uretim", dikkat: false },
    { ad: "Gönderilmeyi bekleyen sevkiyat", deger: o && o.hazirlananSevkiyat + o.kargodaSevkiyat, alt: o && `${o.hazirlananSevkiyat} kargoya verilecek · ${o.kargodaSevkiyat} kargoda`, yol: "/sevkiyat", dikkat: false },
    { ad: "Depodaki ürün", deger: o?.depodaUrun, yol: "/stok?gorunum=urun", dikkat: false },
    { ad: "Minimum stok altındaki parça", deger: o?.minAltindaParca, yol: "/stok?gorunum=parca", dikkat: true },
    { ad: "Açık geri çağırma", deger: o?.acikGeriCagirma, yol: "#geri-cagirmalar", dikkat: true },
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
              placeholder="Lot no, seri no ya da parça kodu ara"
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
                    <span className="cip-tur">{a.tur === "lot" ? "lot" : a.tur === "seri" ? "seri"  : "parça"}</span> {a.deger}
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
        {ozet.map((o) => {
          const sinif = `ozet-kutu${o.dikkat && o.deger ? " ozet-kutu--dikkat" : ""}`;
          const icerik = (
            <>
              <span>{o.ad}</span>
              <b>{o.deger ?? "…"}</b>
              {o.alt && <small className="yardim">{o.alt}</small>}
            </>
          );
          return o.yol === "#geri-cagirmalar" ? (
            <button key={o.ad} type="button" className={`${sinif} ozet-kutu--dugme`} aria-expanded={geriAcik}
              onClick={() => setGeriAcik(!geriAcik)}>
              {icerik}
            </button>
          ) : (
            <Link key={o.ad} href={o.yol} className={sinif}>{icerik}</Link>
          );
        })}
      </section>
      {geriAcik && <GeriCagirmalar kapat={() => setGeriAcik(false)} />}

      {o && <UretimUyarilari ozet={o} />}
      <div className="iki-sutun">
        <SiradakiIsler />
        {o && <YaklasanTeslimler ozet={o} />}
      </div>
      <OnumuzdekiGunler />
    </main>
  );
}

