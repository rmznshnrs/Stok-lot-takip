"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiHatasi, geriCagirma, izleme, parcalar, uretim, type Aday, type NumaraArama, type NumaraTuru } from "@/lib/api";
import { lotAdresi, seriAdresi } from "@/lib/adres";
import { tarih } from "@/lib/bicim";
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
  { yol: "/uretim", baslik: "Üretim" },
  { yol: "/satis?yeni=1", baslik: "Satış" },
];

const HAREKET_ADI = { giris: "Mal girişi", uretim: "Üretim", satis: "Satış" } as const;


export default function AnaSayfaEkrani() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [sonuc, setSonuc] = useState<{ q: string; arama: NumaraArama } | null>(null);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const { veri: hareketler, hata: hareketHatasi } = useVeri(() => izleme.hareketler(8), []);
  const { veri: parcaListesi } = useVeri(() => parcalar.listele(), []);
  const { veri: acikGeri } = useVeri(() => geriCagirma.liste(true), []);
  const { veri: uretimler } = useVeri(() => uretim.gecmis(), []);
  const ozet = [
    { ad: "Minimum stok altındaki parça", deger: parcaListesi?.filter((p) => p.minAltinda).length, yol: "/stok?gorunum=parca", dikkat: true },
    { ad: "Açık geri çağırma", deger: acikGeri?.length, yol: "/uretim?sekme=geri", dikkat: true },
    { ad: "Depodaki ürün", deger: uretimler?.filter((u) => !u.musteriAd && !u.hurda).length, yol: "/stok?gorunum=urun", dikkat: false },
    { ad: "Toplam üretilen", deger: uretimler?.length, yol: "/uretim?sekme=gecmis", dikkat: false },
  ];

  async function ara(e: FormEvent) {
    e.preventDefault();
    const metin = numaraAyikla(q);
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
              <div className="not">“{sonuc.q}” birebir eşleşmedi. Şunlardan birini mi arıyordunuz?</div>
              <div className="seri-listesi">
                {sonuc.arama.adaylar.map((a: Aday) => (
                  <Link key={`${a.tur}-${a.deger}`} href={izAdresi(a.tur, a.deger)} className="seri-cip seri-cip--mavi">
                    <span className="cip-tur">{a.tur === "lot" ? "lot" : a.tur === "seri" ? "seri" : "parça"}</span> {a.deger}
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

      <section className="ozet-kutular">
        {ozet.map((o) => (
          <Link key={o.ad} href={o.yol} className={`ozet-kutu${o.dikkat && o.deger ? " ozet-kutu--dikkat" : ""}`}>
            <span>{o.ad}</span>
            <b>{o.deger ?? "…"}</b>
          </Link>
        ))}
      </section>

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
                  <td>{HAREKET_ADI[h.tur]}</td>
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
