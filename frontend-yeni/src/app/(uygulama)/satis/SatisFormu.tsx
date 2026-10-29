"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import SeriSecimi from "@/components/SeriSecimi";
import { ApiHatasi, musteriler, satis, uretim, urunler } from "@/lib/api";
import { istanbulZamani, simdi } from "@/lib/bicim";
import { numaraAyikla } from "@/lib/gs1";
import { useVeri } from "@/lib/kancalar";

/** Sepetteki bir ürün: hangi ürün, hangi ürün lotu; uyari: içindeki hatalı lotlar (varsa). */
interface Kalem { seriNo: string; urunAd: string; lotNo: string; uyari: string[] }

/**
 * Satış: ürün ve lot seçilir, o lottan depodaki ürünler adetle ya da tek tek seçilerek eklenir.
 * Seri numarası elle yazılmaz.
 */
export default function SatisFormu({ kaydedildi }: { kaydedildi: () => void }) {
  const { veri: musteriListesi, yenile: musterileriYenile } = useVeri(() => musteriler(), []);
  const { veri: urunListesi } = useVeri(() => urunler.listele(), []);
  const [musteri, setMusteri] = useState("");
  const [satisZamani, setSatisZamani] = useState(simdi);
  const [urunId, setUrunId] = useState("");
  const [lotNo, setLotNo] = useState("");
  const [adet, setAdet] = useState("1");
  const [tekTek, setTekTek] = useState(false);
  const [secili, setSecili] = useState<string[]>([]);
  const [sepet, setSepet] = useState<Kalem[]>([]);
  const [onay, setOnay] = useState(false);
  const [okunan, setOkunan] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  // Seçili ürünün satılabilir seri numaraları: depoda, hurda değil, geri çağrılmamış, sepette değil
  const { veri: gecmis, yenile: gecmisiYenile } = useVeri(
    () => (urunId ? uretim.gecmis(undefined, Number(urunId)) : Promise.resolve([])),
    [urunId],
  );
  const satilabilir = (gecmis ?? [])
    .filter((g) => !g.musteriAd && !g.hurda && !g.geriCagirmaDurumu && !sepet.some((k) => k.seriNo === g.seriNo))
    .sort((a, b) => a.seriNo.localeCompare(b.seriNo, "tr", { numeric: true }));
  const lotlar = [...new Set(satilabilir.map((g) => g.lotNo))].map((l) => ({
    lotNo: l, adet: satilabilir.filter((g) => g.lotNo === l).length,
  }));
  const lottakiler = satilabilir.filter((g) => g.lotNo === lotNo);
  const urunAd = urunListesi?.find((u) => String(u.id) === urunId)?.ad ?? "";
  const uyariVar = sepet.some((k) => k.uyari.length > 0);

  async function ekle(seriler: string[], ad = urunAd, lot = lotNo) {
    if (!seriler.length) return;
    setMesaj(null);
    let uyarilar: { seriNo: string; lotlar: string[] }[] = [];
    try {
      uyarilar = await satis.uyarilar(seriler);
    } catch (err) {
      return setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Eklenemedi." });
    }
    setSepet([...sepet, ...seriler.map((s) => ({
      seriNo: s, urunAd: ad, lotNo: lot, uyari: uyarilar.find((u) => u.seriNo === s)?.lotlar ?? [],
    }))]);
    setSecili([]);
    setAdet("1");
  }

  /** Okutulan ya da yazılan seri no (GS1 karekodsa içinden ayıklanır) depodaysa sepete eklenir. */
  async function seriOkut(ham: string) {
    const seri = numaraAyikla(ham, "seri");
    setOkunan("");
    if (!seri) return;
    setMesaj(null);
    if (sepet.some((k) => k.seriNo.toLocaleUpperCase("tr") === seri.toLocaleUpperCase("tr")))
      return setMesaj({ tur: "warning", metin: `${seri} zaten listede.` });
    let u;
    try {
      u = (await uretim.gecmis(seri)).find((g) => g.seriNo.toLocaleUpperCase("tr") === seri.toLocaleUpperCase("tr"));
    } catch (err) {
      return setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Seri no aranamadı." });
    }
    if (!u) return setMesaj({ tur: "error", metin: `${seri} bulunamadı.` });
    if (u.musteriAd) return setMesaj({ tur: "error", metin: `${u.seriNo} satılmış: ${u.musteriAd}.` });
    if (u.hurda) return setMesaj({ tur: "error", metin: `${u.seriNo} hurdaya / kayba ayrılmış.` });
    if (u.geriCagirmaDurumu) return setMesaj({ tur: "error", metin: `${u.seriNo} geri çağrılmış, satılamaz.` });
    await ekle([u.seriNo], u.urunAd, u.lotNo);
  }

  function adetleEkle() {
    const n = Number(adet);
    if (!Number.isInteger(n) || n < 1) return setMesaj({ tur: "error", metin: "Adet en az 1 olmalı." });
    if (n > lottakiler.length) return setMesaj({ tur: "error", metin: `Bu lotta ${lottakiler.length} ürün var.` });
    ekle(lottakiler.slice(0, n).map((g) => g.seriNo));
  }

  function kaldir(seriNo: string) {
    const kalan = sepet.filter((k) => k.seriNo !== seriNo);
    setSepet(kalan);
    if (!kalan.some((k) => k.uyari.length)) setOnay(false);
  }

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    if (!musteri.trim()) return setMesaj({ tur: "error", metin: "Müşteri girin." });
    if (!sepet.length) return setMesaj({ tur: "error", metin: "Ürün ekleyin." });
    if (uyariVar && !onay) {
      return setMesaj({ tur: "warning", metin: "Hatalı lotlu ürün var; onay kutusunu işaretleyin." });
    }
    setBekliyor(true);
    try {
      const s = await satis.sat({
        musteriAdi: musteri.trim(),
        seriNolar: sepet.map((k) => k.seriNo),
        zaman: istanbulZamani(satisZamani),
        geriCagrilanOnayi: onay,
      });
      setMesaj({ tur: "success", metin: `Satış kaydedildi: ${s.seriNolar.length} ürün.` });
      setSepet([]);
      setOnay(false);
      setSatisZamani(simdi());
      setMusteri("");
      if (s.yeniMusteri) musterileriYenile();
      gecmisiYenile();
      kaydedildi();
    } catch (err) {
      if (err instanceof ApiHatasi && err.uyarilar.length) {
        // Bu arada bir lot hatalı işaretlendiyse sepetteki uyarıları güncelle
        setSepet(sepet.map((k) => ({ ...k, uyari: err.uyarilar.find((u) => u.seriNo === k.seriNo)?.lotlar ?? k.uyari })));
        setOnay(false);
      }
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  // Sepet ürün + lot gruplarıyla gösterilir
  const gruplar = [...new Set(sepet.map((k) => `${k.urunAd}|${k.lotNo}`))].map((a) => {
    const [ad, lot] = a.split("|");
    return { ad, lot, kalemler: sepet.filter((k) => k.urunAd === ad && k.lotNo === lot) };
  });

  return (
    <section className="acilir-form">
      <h3>Yeni satış</h3>
      <form className="form-dikey" onSubmit={kaydet}>
        <Mesaj mesaj={mesaj} />
        <div className="izgara-2">
          <div className="alan">
            <label htmlFor="musteri">Müşteri</label>
            <input id="musteri" className="girdi" list="musteri-listesi" autoComplete="off" maxLength={200}
              value={musteri} onChange={(e) => setMusteri(e.target.value)} />
            <datalist id="musteri-listesi">
              {musteriListesi?.map((m) => <option key={m.id} value={m.ad} />)}
            </datalist>
          </div>
          <div className="alan">
            <label htmlFor="tarih">Satış zamanı</label>
            <input id="tarih" type="datetime-local" className="girdi" required value={satisZamani} onChange={(e) => setSatisZamani(e.target.value)} />
          </div>
        </div>

        <div className="satis-secim">
          <div className="alan">
            <label htmlFor="seri-okut">Seri no okut</label>
            <div className="satir-ekle">
              <input id="seri-okut" className="girdi girdi--mono girdi--esnek" autoComplete="off" maxLength={200}
                value={okunan} onChange={(e) => setOkunan(e.target.value)}
                onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); seriOkut(okunan); } }} />
              <button type="button" className="btn btn--ikincil" disabled={!okunan.trim()} onClick={() => seriOkut(okunan)}>Ekle</button>
            </div>
          </div>
          <div className="izgara-form">
            <div className="alan">
              <label htmlFor="satis-urun">Ürün</label>
              <select id="satis-urun" className="girdi" value={urunId}
                onChange={(e) => { setUrunId(e.target.value); setLotNo(""); setSecili([]); }}>
                <option value="">Seçin</option>
                {urunListesi?.map((u) => <option key={u.id} value={u.id}>{u.ad}</option>)}
              </select>
            </div>
            <div className="alan">
              <label htmlFor="satis-lot">Lot</label>
              <select id="satis-lot" className="girdi girdi--mono" value={lotNo} disabled={!urunId}
                onChange={(e) => { setLotNo(e.target.value); setSecili([]); }}>
                <option value="">{urunId && gecmis && !lotlar.length ? "Depoda ürün yok" : "Seçin"}</option>
                {lotlar.map((l) => <option key={l.lotNo} value={l.lotNo}>{l.lotNo} · {l.adet} adet depoda</option>)}
              </select>
            </div>
            {!tekTek && (
              <div className="alan">
                <label htmlFor="satis-adet">Adet</label>
                <div className="satir-ekle">
                  <input id="satis-adet" type="number" min={1} max={lottakiler.length || undefined} className="girdi girdi--esnek"
                    disabled={!lotNo} value={adet} onChange={(e) => setAdet(e.target.value)} />
                  <button type="button" className="btn btn--ikincil" disabled={!lotNo} onClick={adetleEkle}>Ekle</button>
                </div>
              </div>
            )}
          </div>
          {lotNo && (
            <label className="onay-etiket">
              <input type="checkbox" checked={tekTek} onChange={(e) => { setTekTek(e.target.checked); setSecili([]); }} /> Seri no ile seç
            </label>
          )}
          {lotNo && tekTek && (
            <>
              <SeriSecimi
                adaylar={lottakiler.map((g) => ({ seriNo: g.seriNo, alt: "depoda" }))}
                secili={secili}
                setSecili={setSecili}
                bos="Ürün yok."
              />
              <div className="dugme-grup">
                <button type="button" className="btn btn--ikincil" disabled={!secili.length} onClick={() => ekle(secili)}>
                  {secili.length ? `${secili.length} ürünü ekle` : "Seçin"}
                </button>
              </div>
            </>
          )}
        </div>

        {gruplar.length > 0 && (
          <div className="sepet">
            {gruplar.map((g) => (
              <div key={`${g.ad}-${g.lot}`} className={`sepet-oge sepet-oge--grup${g.kalemler.some((k) => k.uyari.length) ? " sepet-oge--uyari" : ""}`}>
                <div className="baslik-grup">
                  <span><strong>{g.ad}</strong> · <span className="mono">{g.lot}</span> · {g.kalemler.length} adet</span>
                  <span className="seri-listesi">
                    {g.kalemler.map((k) => (
                      <span key={k.seriNo} className={`seri-cip${k.uyari.length ? " seri-cip--uyari" : ""}`}
                        title={k.uyari.length ? `Hatalı lot: ${k.uyari.join(", ")}` : undefined}>
                        {k.seriNo}
                        <button type="button" className="cip-kaldir" aria-label={`${k.seriNo} kaldır`} onClick={() => kaldir(k.seriNo)}>×</button>
                      </span>
                    ))}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
        {uyariVar && (
          <label className="onay-etiket onay-etiket--uyari">
            <input type="checkbox" checked={onay} onChange={(e) => setOnay(e.target.checked)} /> Hatalı lotlu ürünleri yine de sat
          </label>
        )}

        <div className="dugme-grup">
          <button type="submit" className="btn" disabled={bekliyor}>Satışı kaydet</button>
          <span className="not">Toplam <strong>{sepet.length} ürün</strong></span>
        </div>
      </form>
    </section>
  );
}
