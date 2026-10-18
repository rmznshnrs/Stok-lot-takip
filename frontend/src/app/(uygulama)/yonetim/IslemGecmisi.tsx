"use client";

import Link from "next/link";
import { useState } from "react";
import { ApiHatasi, islemler, kullanicilar, type Islem, type IslemTuru } from "@/lib/api";
import { tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import { lotAdresi, seriAdresi } from "@/lib/adres";

const TURLER: { deger: IslemTuru; ad: string }[] = [
  { deger: "giris", ad: "Mal girişi" },
  { deger: "uretim", ad: "Üretim" },
  { deger: "satis", ad: "Satış" },
  { deger: "iade", ad: "İade" },
  { deger: "duzeltme", ad: "Stok düzeltme" },
  { deger: "hurda", ad: "Hurda / kayıp" },
  { deger: "sorun", ad: "Hatalı lot" },
  { deger: "geri", ad: "Geri çağırma" },
];
const turAdi = (t: IslemTuru) => TURLER.find((x) => x.deger === t)?.ad ?? t;
const UYARI_TURLERI: IslemTuru[] = ["iade", "duzeltme", "hurda", "sorun", "geri"];
const SAYFA = 50;

/** Yalnız Admin: kim, ne zaman, ne yaptı. Türe ve kişiye göre süzülür; "Daha eski" ile geriye gidilir. */
export default function IslemGecmisi() {
  const [tur, setTur] = useState<IslemTuru | "">("");
  const [kisi, setKisi] = useState("");
  const { veri: kisiler } = useVeri(() => kullanicilar.listele(), []);
  const { veri: ilk, hata: ilkHata } = useVeri(
    () => islemler.liste({ tur: tur || undefined, kullaniciId: Number(kisi) || undefined, adet: SAYFA }),
    [tur, kisi],
  );
  // "Daha eski" ile eklenen sayfalar; süzgeç değişince sıfırlanır
  const [ekler, setEkler] = useState<Islem[]>([]);
  const [sonDolu, setSonDolu] = useState<boolean | null>(null);
  const [ekHata, setEkHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const liste = ilk ? [...ilk, ...ekler] : null;
  const devami = (sonDolu ?? ilk?.length === SAYFA) && liste !== null && liste.length > 0;
  const hata = ilkHata || ekHata;

  function suz(yeniTur: IslemTuru | "", yeniKisi: string) {
    setTur(yeniTur);
    setKisi(yeniKisi);
    setEkler([]);
    setSonDolu(null);
    setEkHata("");
  }

  async function dahaEski() {
    if (!liste?.length) return;
    setBekliyor(true);
    setEkHata("");
    try {
      const yeni = await islemler.liste({
        tur: tur || undefined, kullaniciId: Number(kisi) || undefined, once: liste[liste.length - 1].zaman, adet: SAYFA,
      });
      setEkler([...ekler, ...yeni]);
      setSonDolu(yeni.length === SAYFA);
    } catch (err) {
      setEkHata(err instanceof ApiHatasi ? err.message : "İşlem geçmişi alınamadı.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <section className="kart kart--genis">
      <div className="bas-satir">
        <h2>İşlem geçmişi</h2>
        <div className="arama-satiri">
          <select className="girdi" aria-label="İşlem türü" value={tur} onChange={(e) => suz(e.target.value as IslemTuru | "", kisi)}>
            <option value="">Bütün işlemler</option>
            {TURLER.map((t) => <option key={t.deger} value={t.deger}>{t.ad}</option>)}
          </select>
          <select className="girdi" aria-label="Kişi" value={kisi} onChange={(e) => suz(tur, e.target.value)}>
            <option value="">Herkes</option>
            {kisiler?.map((k) => <option key={k.id} value={k.id}>{k.ad} {k.soyad}</option>)}
          </select>
        </div>
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      <div className="tablo-kap">
        <table className="tablo">
          <thead>
            <tr>
              <th>Zaman</th>
              <th>İşlem</th>
              <th>Kim</th>
              <th>Ne</th>
              <th>Numara</th>
            </tr>
          </thead>
          <tbody>
            {liste === null && !hata && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
            {liste?.length === 0 && <tr className="bos"><td colSpan={5}>Kayıt yok.</td></tr>}
            {liste?.map((i, n) => (
              <tr key={`${i.zaman}-${i.tur}-${n}`}>
                <td className="hucre-dar">{tarihSaat(i.zaman)}</td>
                <td>
                  <span className={`rozet rozet--kucuk ${UYARI_TURLERI.includes(i.tur) ? "rozet--uyari" : "rozet--mavi"}`}>
                    {turAdi(i.tur)}
                  </span>
                </td>
                <td>{i.kullaniciAd}</td>
                <td>{i.aciklama}</td>
                <td className="mono">
                  {i.numara && (
                    <Link href={i.numaraTuru === "lot" ? lotAdresi(i.numara) : seriAdresi(i.numara)}>
                      {i.numara}
                    </Link>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {devami && liste && (
        <div className="kart-alt">
          <span />
          <button type="button" className="btn btn--ikincil" disabled={bekliyor} onClick={dahaEski}>
            Daha eski
          </button>
        </div>
      )}
    </section>
  );
}
