"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiHatasi, izleme, type Aday, type NumaraArama, type NumaraTuru } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

function izAdresi(tur: NumaraTuru, deger: string) {
  const d = encodeURIComponent(deger);
  if (tur === "lot") return `/izlenebilirlik?lot=${d}`;
  if (tur === "seri") return `/izlenebilirlik?sekme=seri&seri=${d}`;
  return `/stok?parca=${d}`;
}

const HIZLI = [
  { yol: "/stok", baslik: "Mal girişi yap", aciklama: "Gelen parçayı lot numarası, tedarikçi ve tarihiyle stoğa ekle" },
  { yol: "/uretim", baslik: "Ürün üret", aciklama: "Ürün seç, adet gir; parçalar ilk gelen lottan düşülür, seri no verilir" },
  { yol: "/satis", baslik: "Satış kaydet", aciklama: "Satılan seri numaralarını müşteriye ve tarihe bağla" },
];

const HAREKET_ROZETI = {
  giris: { ad: "Giriş", sinif: "rozet--mavi" },
  uretim: { ad: "Üretim", sinif: "rozet--basari" },
  satis: { ad: "Satış", sinif: "rozet--uyari" },
} as const;

export default function AnaSayfaEkrani() {
  const router = useRouter();
  const [q, setQ] = useState("");
  const [sonuc, setSonuc] = useState<{ q: string; arama: NumaraArama } | null>(null);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const { veri: hareketler, hata: hareketHatasi } = useVeri(() => izleme.hareketler(8), []);

  async function ara(e: FormEvent) {
    e.preventDefault();
    const metin = q.trim();
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
    <main className="icerik icerik--dikey icerik--genis">
      <section className="arama-bolumu">
        <h1>Bir lot veya seri numarasının izini sür</h1>
        <form className="arama-buyuk" role="search" onSubmit={ara}>
          <input
            type="search"
            className="girdi girdi--buyuk"
            aria-label="Lot, seri no veya parça kodu"
            placeholder="Lot no, seri no veya parça kodu yazın — örn. L-D-2601 veya SN-IP-0001"
            autoFocus
            value={q}
            onChange={(e) => setQ(e.target.value)}
          />
          <button type="submit" className="btn btn--buyuk" disabled={bekliyor}>İzini sür</button>
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
        ) : (
          <div className="not">Sistem numaranın lot mu seri no mu olduğunu kendisi anlar ve ilgili zinciri gösterir.</div>
        )}
      </section>

      <section className="hizli-kartlar">
        {HIZLI.map((h, i) => (
          <Link key={h.yol} href={h.yol} className="hizli-kart">
            <div className="hizli-no">{i + 1}</div>
            <div className="hizli-baslik">{h.baslik}</div>
            <div className="not not--buyuk">{h.aciklama}</div>
          </Link>
        ))}
      </section>

      <section className="kart kart--genis kart--hareketler">
        <h3>Son hareketler</h3>
        {hareketHatasi && <div className="mesaj mesaj--error">{hareketHatasi}</div>}
        {hareketler === null && !hareketHatasi && <p className="yukleniyor">Yükleniyor…</p>}
        {hareketler?.map((h, i) => {
          const rozet = HAREKET_ROZETI[h.tur];
          return (
            <div key={i} className="hareket">
              <span className="hareket-tarih">{tarih(h.tarih)}</span>
              <span className={`hareket-rozet ${rozet.sinif}`}>{rozet.ad}</span>
              <span className="hareket-aciklama">{h.aciklama}</span>
              <Link className="mono hareket-numara" href={izAdresi(h.numaraTuru, h.numaraIlk)}>{h.numara}</Link>
            </div>
          );
        })}
        {hareketler?.length === 0 && (
          <p className="not">Henüz hareket yok. Parça tanımlayıp mal girişi yaparak başlayın.</p>
        )}
      </section>
    </main>
  );
}
