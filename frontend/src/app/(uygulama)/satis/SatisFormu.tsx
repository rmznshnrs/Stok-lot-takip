"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, musteriler, satis, type Satilabilir } from "@/lib/api";
import { bugun } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

/** Seri no'ları ekleyip kontrol eder (bulunamayan, satılmış olan eklenmez); satışı kaydeder. */
export default function SatisFormu({ kaydedildi }: { kaydedildi: () => void }) {
  const { veri: musteriListesi, yenile: musterileriYenile } = useVeri(() => musteriler(), []);
  const [musteri, setMusteri] = useState("");
  const [satisTarihi, setSatisTarihi] = useState(bugun);
  const [yeniSeri, setYeniSeri] = useState("");
  const [sepet, setSepet] = useState<Satilabilir[]>([]);
  const [seriHatalari, setSeriHatalari] = useState<string[]>([]);
  const [onay, setOnay] = useState(false);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [ekleniyor, setEkleniyor] = useState(false);
  const [bekliyor, setBekliyor] = useState(false);

  const uyariVar = sepet.some((k) => k.geriCagrilanLotlar.length > 0);

  async function ekle() {
    const seriler = yeniSeri.split(/[\s,;]+/).map((s) => s.trim()).filter(Boolean);
    if (!seriler.length) return;
    setEkleniyor(true);
    const hatalar: string[] = [];
    const eklenen: Satilabilir[] = [];
    for (const seri of seriler) {
      const ayni = (x: Satilabilir) => x.seriNo.toLocaleUpperCase("tr") === seri.toLocaleUpperCase("tr");
      if (sepet.some(ayni) || eklenen.some(ayni)) {
        hatalar.push(`${seri} zaten listede.`);
        continue;
      }
      try {
        eklenen.push(await satis.kontrol(seri));
      } catch (err) {
        hatalar.push(err instanceof ApiHatasi ? err.message : `${seri} kontrol edilemedi.`);
      }
    }
    setSepet([...sepet, ...eklenen]);
    setSeriHatalari(hatalar);
    setYeniSeri(hatalar.length ? seriler.filter((s) => !eklenen.some((e) => e.seriNo.toLocaleUpperCase("tr") === s.toLocaleUpperCase("tr"))).join(" ") : "");
    setEkleniyor(false);
  }

  function kaldir(seriNo: string) {
    const kalan = sepet.filter((k) => k.seriNo !== seriNo);
    setSepet(kalan);
    if (!kalan.some((k) => k.geriCagrilanLotlar.length)) setOnay(false);
  }

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    if (!musteri.trim()) return setMesaj({ tur: "error", metin: "Müşteri adını yazın." });
    if (!sepet.length) return setMesaj({ tur: "error", metin: "En az bir seri numarası ekleyin." });
    if (uyariVar && !onay) {
      return setMesaj({ tur: "warning", metin: "Geri çağrılan lot içeren ürünler var. Satmak için onay kutusunu işaretleyin." });
    }
    setBekliyor(true);
    try {
      const s = await satis.sat({
        musteriAdi: musteri.trim(),
        seriNolar: sepet.map((k) => k.seriNo),
        tarih: satisTarihi,
        geriCagrilanOnayi: onay,
      });
      const ek = s.yeniMusteri ? ` ${s.musteriAd} yeni müşteri olarak eklendi.` : "";
      setMesaj({ tur: "success", metin: `${s.seriNolar.length} ürün ${s.musteriAd} müşterisine satıldı.${ek}` });
      setSepet([]);
      setOnay(false);
      setMusteri("");
      setSeriHatalari([]);
      if (s.yeniMusteri) musterileriYenile();
      kaydedildi();
    } catch (err) {
      if (err instanceof ApiHatasi && err.uyarilar.length) {
        // Bu arada bir lot geri çağrıldıysa sepetteki uyarıları güncelle
        setSepet(sepet.map((k) => ({ ...k, geriCagrilanLotlar: err.uyarilar.find((u) => u.seriNo === k.seriNo)?.lotlar ?? k.geriCagrilanLotlar })));
        setOnay(false);
      }
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Satış kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <section className="kart kart--form kart--form-satis">
      <h2>Yeni satış</h2>
      <form className="form-dikey" onSubmit={kaydet}>
        <Mesaj mesaj={mesaj} />
        <div className="alan">
          <label htmlFor="musteri">Müşteri</label>
          <input
            id="musteri"
            className="girdi"
            list="musteri-listesi"
            placeholder="Müşteri seçin veya yazın"
            autoComplete="off"
            maxLength={200}
            value={musteri}
            onChange={(e) => setMusteri(e.target.value)}
          />
        </div>
        <datalist id="musteri-listesi">
          {musteriListesi?.map((m) => <option key={m.id} value={m.ad} />)}
        </datalist>
        <div className="alan">
          <label htmlFor="tarih">Satış tarihi</label>
          <input id="tarih" type="date" className="girdi" required value={satisTarihi} onChange={(e) => setSatisTarihi(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor="yeni-seri">Satılan seri numaraları</label>
          <div className="satir-ekle">
            <input
              id="yeni-seri"
              className="girdi girdi--esnek girdi--mono"
              placeholder="SN-…"
              autoComplete="off"
              value={yeniSeri}
              onChange={(e) => setYeniSeri(e.target.value)}
              onKeyDown={(e) => {
                if (e.key === "Enter") {
                  e.preventDefault();
                  ekle();
                }
              }}
            />
            <button type="button" className="btn btn--ikincil" onClick={ekle} disabled={ekleniyor}>Ekle</button>
          </div>
          {seriHatalari.map((h) => <span key={h} className="hata">{h}</span>)}
        </div>

        {sepet.length > 0 && (
          <div className="sepet">
            {sepet.map((k) => (
              <div key={k.seriNo} className={`sepet-oge${k.geriCagrilanLotlar.length ? " sepet-oge--uyari" : ""}`}>
                <div className="baslik-grup">
                  <span className="mono akis-numara">{k.seriNo}</span>
                  {k.geriCagrilanLotlar.length ? (
                    <span className="yardim uyari-metin">
                      Geri çağrılan lot içeriyor ({k.geriCagrilanLotlar.join(", ")}) — satışa uygun değil
                    </span>
                  ) : (
                    <span className="yardim">{k.urunAd}</span>
                  )}
                </div>
                <button
                  type="button"
                  className="btn-kaldir"
                  aria-label={`${k.seriNo} listeden kaldır`}
                  title="Kaldır"
                  onClick={() => kaldir(k.seriNo)}
                >
                  ×
                </button>
              </div>
            ))}
          </div>
        )}
        {uyariVar && (
          <label className="onay-etiket onay-etiket--uyari">
            <input type="checkbox" checked={onay} onChange={(e) => setOnay(e.target.checked)} /> Geri çağrılan lot içeren
            ürünleri yine de sat
          </label>
        )}

        <div className="toplam-satir"><span>Toplam</span><strong>{sepet.length} ürün</strong></div>
        <button type="submit" className="btn btn--tam-alt" disabled={bekliyor}>Satışı kaydet</button>
      </form>
    </section>
  );
}
