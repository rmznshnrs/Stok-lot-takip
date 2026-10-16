"use client";

import { useSearchParams } from "next/navigation";
import { useState } from "react";
import Barkod, { Karekod } from "@/components/Barkod";
import { izleme } from "@/lib/api";
import { tarihSaat } from "@/lib/bicim";
import { ETIKET_TURLERI, etiketTuruKaydet, etiketTuruMu, kayitliEtiketTuru, type EtiketTuru } from "@/lib/etiket";
import { gs1Tarih, type Gs1Alan } from "@/lib/gs1";
import { useVeri } from "@/lib/kancalar";

interface Etiket {
  numara: string;
  baslik: string;
  alt: string;
  /** Karekodun içindeki bilgiler (GS1) */
  gs1: Gs1Alan[];
}

type Boyut = "kucuk" | "a4";

const BOYUTLAR: { deger: Boyut; ad: string }[] = [
  { deger: "kucuk", ad: "Etiket yazıcı" },
  { deger: "a4", ad: "A4 etiket kağıdı" },
];

function liste(deger: string | null) {
  return (deger ?? "").split(",").map((s) => s.trim()).filter(Boolean);
}

/** Adres: /etiket?seri=SN-IP-0001,SN-IP-0002 veya /etiket?lot=L-D-2601; &tur=yok|cizgili|karekod */
export default function EtiketEkrani() {
  const params = useSearchParams();
  const seriler = liste(params.get("seri"));
  const lotlar = liste(params.get("lot"));
  const [kopya, setKopya] = useState(1);
  const [boyut, setBoyut] = useState<Boyut>("kucuk");
  // Adreste tür verildiyse o (üretimde seçilen), yoksa son seçilen.
  // Bu ekran yalnız tarayıcıda çizilir (oturum gelince), localStorage burada okunabilir
  const adrestekiTur = params.get("tur");
  const [tur, setTur] = useState<EtiketTuru>(() => (etiketTuruMu(adrestekiTur) ? adrestekiTur : kayitliEtiketTuru()));

  function turSec(t: EtiketTuru) {
    setTur(t);
    etiketTuruKaydet(t);
  }

  const anahtar = `${seriler.join(",")}|${lotlar.join(",")}`;
  const { veri: etiketler, hata } = useVeri(async (): Promise<Etiket[]> => {
    const s = await Promise.all(
      seriler.map(async (n) => {
        const iz = await izleme.seri(n);
        return {
          numara: iz.seriNo,
          baslik: iz.urunAd,
          alt: `Üretim ${tarihSaat(iz.tarih).slice(0, 10)}`,
          gs1: [
            { ai: "240", deger: iz.urunKod },
            { ai: "21", deger: iz.seriNo },
            { ai: "11", deger: gs1Tarih(iz.tarih) },
          ],
        };
      }),
    );
    const l = await Promise.all(
      lotlar.map(async (n) => {
        const { lot } = await izleme.lot(n);
        return {
          numara: lot.lotNo,
          baslik: `${lot.parcaKod} · ${lot.parcaAd}`,
          alt: `${lot.tedarikciAd} · ${lot.girisAdet} adet`,
          gs1: [
            { ai: "240", deger: lot.parcaKod },
            { ai: "10", deger: lot.lotNo },
          ],
        };
      }),
    );
    return [...s, ...l];
  }, [anahtar]);

  const basilacak = (etiketler ?? []).flatMap((e) => Array.from({ length: kopya }, () => e));

  return (
    <main className="icerik icerik--dikey">
      <section className="kart etiket-ayar">
        <div className="bas-satir">
          <h2>Etiket yazdır</h2>
          <button type="button" className="btn" disabled={!basilacak.length} onClick={() => window.print()}>
            Yazdır
          </button>
        </div>
        {!seriler.length && !lotlar.length && <p className="not">Yazdırılacak seri veya lot numarası seçilmedi.</p>}
        <div className="izgara-3">
          <div className="alan">
            <label htmlFor="tur">Barkod</label>
            <select id="tur" className="girdi" value={tur} onChange={(e) => turSec(e.target.value as EtiketTuru)}>
              {ETIKET_TURLERI.map((b) => <option key={b.deger} value={b.deger}>{b.ad}</option>)}
            </select>
          </div>
          <div className="alan">
            <label htmlFor="boyut">Etiket boyutu</label>
            <select id="boyut" className="girdi" value={boyut} onChange={(e) => setBoyut(e.target.value as Boyut)}>
              {BOYUTLAR.map((b) => <option key={b.deger} value={b.deger}>{b.ad}</option>)}
            </select>
          </div>
          <div className="alan">
            <label htmlFor="kopya">Her numaradan kaç adet</label>
            <input
              id="kopya"
              type="number"
              min={1}
              max={50}
              className="girdi"
              value={kopya}
              onChange={(e) => setKopya(Math.min(50, Math.max(1, Number(e.target.value) || 1)))}
            />
          </div>
        </div>
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
      </section>

      {etiketler === null && !hata && (seriler.length > 0 || lotlar.length > 0) && <p className="yukleniyor">Yükleniyor…</p>}

      <div className={`etiket-alani etiket-alani--${boyut}`}>
        {basilacak.map((e, i) => (
          tur === "yok" ? (
            <div key={`${e.numara}-${i}`} className="etiket">
              <div className="etiket-firma">MKC BİLİŞİM</div>
              <div className="etiket-baslik">{e.baslik}</div>
              <div className="etiket-numara">{e.numara}</div>
              <div className="etiket-alt">{e.alt}</div>
            </div>
          ) : tur === "karekod" ? (
            <div key={`${e.numara}-${i}`} className="etiket etiket--karekod">
              <Karekod alanlar={e.gs1} />
              <div className="etiket-yazilar">
                <div className="etiket-firma">MKC BİLİŞİM</div>
                {e.gs1.filter((a) => a.ai !== "11").map((a) => (
                  <div key={a.ai} className="etiket-gs1">({a.ai}) {a.deger}</div>
                ))}
                <div className="etiket-alt">{e.alt}</div>
              </div>
            </div>
          ) : (
            <div key={`${e.numara}-${i}`} className="etiket">
              <div className="etiket-firma">MKC BİLİŞİM</div>
              <div className="etiket-baslik">{e.baslik}</div>
              <Barkod deger={e.numara} yukseklik={boyut === "a4" ? 30 : 26} />
              <div className="etiket-alt">{e.alt}</div>
            </div>
          )
        ))}
      </div>
    </main>
  );
}
