"use client";

import GeriIleri from "@/components/GeriIleri";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import SeriSecimi from "@/components/SeriSecimi";
import { ApiHatasi, firmalar, parcalar, sira, siparisler, urunler } from "@/lib/api";
import { bugun, tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import { siparisAdresi } from "./MusteriSiparisleri";

const YENI = "yeni";

interface AgacSatiri { anahtar: number; parcaId: string; adet: string }
interface Kalem {
  anahtar: number; urunId: string; adet: string; seriler: string[]; uretim: boolean;
  yeniKod: string; yeniAd: string; agac: AgacSatiri[];
}

let sayac = 0;
const yeniAgacSatiri = (): AgacSatiri => ({ anahtar: ++sayac, parcaId: "", adet: "1" });
const yeniKalem = (): Kalem => ({ anahtar: ++sayac, urunId: "", adet: "1", seriler: [], uretim: false, yeniKod: "", yeniAd: "", agac: [yeniAgacSatiri()] });

export default function SiparisFormu({ musteriId }: { musteriId: number }) {
  const router = useRouter();
  const { veri: musteriListesi } = useVeri(() => firmalar.musteriler(), []);
  const musteri = musteriListesi?.find((m) => m.id === musteriId);
  const [kalemler, setKalemler] = useState<Kalem[]>(() => [yeniKalem()]);
  const [istenen, setIstenen] = useState("");
  const [aciklama, setAciklama] = useState("");
  const [siraya, setSiraya] = useState(true);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  const degistir = (anahtar: number, d: Partial<Kalem>) =>
    setKalemler((ks) => ks.map((k) => (k.anahtar === anahtar ? { ...k, ...d } : k)));

  async function urunleriHazirla(dolu: Kalem[]) {
    const idler = new Map<number, number>();
    for (const k of dolu) {
      if (k.urunId !== YENI) {
        idler.set(k.anahtar, Number(k.urunId));
        continue;
      }
      const agac = k.agac.filter((a) => a.parcaId).map((a) => ({ parcaId: Number(a.parcaId), adet: Number(a.adet) }));
      if (!k.yeniKod.trim() || !k.yeniAd.trim()) throw new Error("Yeni ürünün kodunu ve adını girin.");
      if (!agac.length) throw new Error(`${k.yeniAd.trim()} için ürün ağacına en az bir parça ekleyin.`);
      if (new Set(agac.map((a) => a.parcaId)).size !== agac.length) throw new Error("Ürün ağacında aynı parça iki kez var.");
      const u = await urunler.olustur({ kod: k.yeniKod.trim(), ad: k.yeniAd.trim() });
      await urunler.agacKaydet(u.id, agac);
      idler.set(k.anahtar, u.id);
      degistir(k.anahtar, { urunId: String(u.id) });
    }
    return idler;
  }

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    const dolu = kalemler.filter((k) => k.urunId);
    if (!dolu.length) return setMesaj({ tur: "error", metin: "Ürün seçin." });
    setBekliyor(true);
    try {
      const idler = await urunleriHazirla(dolu);
      const s = await siparisler.olustur({
        musteriId,
        kalemler: dolu.map((k) => ({ urunId: idler.get(k.anahtar)!, adet: Number(k.adet), seriNolar: k.urunId === YENI || k.uretim ? [] : k.seriler })),
        tarih: bugun(),
        istenenTarih: istenen || undefined,
        aciklama: aciklama.trim(),
      });
      const uyarilar: string[] = [];
      if (siraya) {
        for (const k of s.kalemler.filter((x) => x.eksik > 0)) {
          try {
            await sira.ekle({ urunId: k.urunId, siparisKalemiId: k.id, planlananTarih: istenen || bugun() });
          } catch (err) {
            uyarilar.push(`${k.urunAd}: ${err instanceof ApiHatasi ? err.message : "sıraya eklenemedi"}`);
          }
        }
      }
      router.push(`${siparisAdresi(s.id)}${uyarilar.length ? `&uyari=${encodeURIComponent(uyarilar.join(" | "))}` : ""}`);
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof Error ? err.message : "Talep kaydedilemedi." });
      setBekliyor(false);
    }
  }

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir">
        <GeriIleri />
      </div>
      <form className="kart kart--genis kart--bosluklu" onSubmit={kaydet}>
        <h2>Yeni Talep{musteri && ` · ${musteri.ad}`}</h2>
        <Mesaj mesaj={mesaj} />
        <div className="izgara-2">
          <div className="alan">
            <label htmlFor="sip-istenen">İstenen Teslim Tarihi</label>
            <input id="sip-istenen" type="date" className="girdi" min={bugun()} value={istenen} onChange={(e) => setIstenen(e.target.value)} />
          </div>
          <div className="alan">
            <label htmlFor="sip-aciklama">Açıklama</label>
            <input id="sip-aciklama" className="girdi" maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
          </div>
        </div>

        {kalemler.map((k, i) => (
          <KalemSatiri
            key={k.anahtar}
            kalem={k}
            sira={i + 1}
            digerUrunler={kalemler.filter((x) => x.anahtar !== k.anahtar).map((x) => x.urunId)}
            degistir={(d) => degistir(k.anahtar, d)}
            kaldir={kalemler.length > 1 ? () => setKalemler((ks) => ks.filter((x) => x.anahtar !== k.anahtar)) : undefined}
          />
        ))}
        <div>
          <button type="button" className="btn btn--ikincil btn--kucuk" onClick={() => setKalemler((ks) => [...ks, yeniKalem()])}>
            + Ürün Ekle
          </button>
        </div>

        <label className="onay-etiket">
          <input type="checkbox" checked={siraya} onChange={(e) => setSiraya(e.target.checked)} /> Depoda olmayanları kaydedince üretime gönder
        </label>
        <div className="kart-alt">
          <span />
          <button type="submit" className="btn" disabled={bekliyor}>{bekliyor ? "Kaydediliyor…" : "Talebi Kaydet"}</button>
        </div>
      </form>
    </main>
  );
}

function KalemSatiri({ kalem, sira, digerUrunler, degistir, kaldir }: {
  kalem: Kalem;
  sira: number;
  digerUrunler: string[];
  degistir: (d: Partial<Kalem>) => void;
  kaldir?: () => void;
}) {
  const { veri: urunListesi } = useVeri(() => urunler.listele(), [kalem.urunId === YENI]);
  const yeni = kalem.urunId === YENI;
  const urunId = yeni ? 0 : Number(kalem.urunId) || 0;
  const { veri: hazir } = useVeri(() => (urunId ? siparisler.hazir(urunId) : Promise.resolve([])), [urunId]);
  const adet = Math.max(0, Math.floor(Number(kalem.adet)) || 0);
  const secili = kalem.uretim ? [] : kalem.seriler.slice(0, adet);
  const uretilecek = Math.max(0, adet - secili.length);

  return (
    <fieldset className="kutu form-dikey">
      <div className="bas-satir">
        <strong>{sira}. Ürün</strong>
        {kaldir && <button type="button" className="btn-baglanti" onClick={kaldir}>Kaldır</button>}
      </div>
      <div className="izgara-2">
        <div className="alan">
          <label htmlFor={`k-urun-${kalem.anahtar}`}>Ürün</label>
          <select id={`k-urun-${kalem.anahtar}`} className="girdi" value={kalem.urunId}
            onChange={(e) => degistir({ urunId: e.target.value, seriler: [] })}>
            <option value="">Seçin</option>
            {urunListesi?.filter((u) => !digerUrunler.includes(String(u.id))).map((u) => (
              <option key={u.id} value={u.id}>{u.ad}</option>
            ))}
            <option value={YENI}>+ Yeni Ürün</option>
          </select>
        </div>
        <div className="alan">
          <label htmlFor={`k-adet-${kalem.anahtar}`}>Adet</label>
          <input id={`k-adet-${kalem.anahtar}`} type="number" min={1} max={999} required className="girdi"
            value={kalem.adet} onChange={(e) => degistir({ adet: e.target.value })} />
        </div>
      </div>
      {yeni && <YeniUrun kalem={kalem} degistir={degistir} />}
      {urunId > 0 && (
        <>
          <div className="sekmeler kaynak-secimi" role="radiogroup" aria-label="Ürünler nereden">
            <button type="button" role="radio" aria-checked={!kalem.uretim} className={`sekme${kalem.uretim ? "" : " sekme--aktif"}`}
              onClick={() => degistir({ uretim: false })}>Depodan Ver</button>
            <button type="button" role="radio" aria-checked={kalem.uretim} className={`sekme${kalem.uretim ? " sekme--aktif" : ""}`}
              onClick={() => degistir({ uretim: true, seriler: [] })}>Üretime Gönder</button>
          </div>
          <div className="bas-satir">
            <span className="not">
              {kalem.uretim ? `${adet} adet üretilecek` : `Depoda hazır: ${hazir?.length ?? "…"} · seçilen ${secili.length} · üretilecek ${uretilecek}`}
            </span>
            {!kalem.uretim && hazir && hazir.length > 0 && (
              <button type="button" className="btn-baglanti"
                onClick={() => degistir({ seriler: hazir.slice(0, adet).map((h) => h.seriNo) })}>
                Hazırdan {Math.min(adet, hazir.length)} Tane Seç
              </button>
            )}
          </div>
          {!kalem.uretim && hazir && hazir.length > 0 && (
            <SeriSecimi
              adaylar={hazir.map((h) => ({ seriNo: h.seriNo, alt: tarih(h.tarih) }))}
              secili={secili}
              setSecili={(s) => degistir({ seriler: s.slice(0, adet) })}
              bos="Depoda hazır ürün yok."
            />
          )}
        </>
      )}
    </fieldset>
  );
}

function YeniUrun({ kalem, degistir }: { kalem: Kalem; degistir: (d: Partial<Kalem>) => void }) {
  const { veri: parcaListesi, yenile: parcalariYenile } = useVeri(() => parcalar.listele(), []);
  const [yeniParca, setYeniParca] = useState(false);
  const agacDegistir = (anahtar: number, d: Partial<AgacSatiri>) =>
    degistir({ agac: kalem.agac.map((a) => (a.anahtar === anahtar ? { ...a, ...d } : a)) });
  return (
    <div className="form-dikey yeni-urun">
      <div className="izgara-2">
        <div className="alan">
          <label htmlFor={`y-kod-${kalem.anahtar}`}>Ürün Kodu</label>
          <input id={`y-kod-${kalem.anahtar}`} className="girdi girdi--mono" maxLength={50} required value={kalem.yeniKod}
            onChange={(e) => degistir({ yeniKod: e.target.value })} />
        </div>
        <div className="alan">
          <label htmlFor={`y-ad-${kalem.anahtar}`}>Ürün Adı</label>
          <input id={`y-ad-${kalem.anahtar}`} className="girdi" maxLength={200} required value={kalem.yeniAd}
            onChange={(e) => degistir({ yeniAd: e.target.value })} />
        </div>
      </div>
      <strong className="not">Ürün Ağacı</strong>
      {kalem.agac.map((a) => (
        <div key={a.anahtar} className="satir-ekle satir-ekle--alt">
          <div className="alan alan--genis">
            <label htmlFor={`y-parca-${a.anahtar}`}>Parça</label>
            <select id={`y-parca-${a.anahtar}`} className="girdi" value={a.parcaId} onChange={(e) => agacDegistir(a.anahtar, { parcaId: e.target.value })}>
              <option value="">Seçin</option>
              {parcaListesi?.map((p) => <option key={p.id} value={p.id}>{p.kod} · {p.ad} ({p.serbest} adet)</option>)}
            </select>
          </div>
          <div className="alan">
            <label htmlFor={`y-adet-${a.anahtar}`}>Adet</label>
            <input id={`y-adet-${a.anahtar}`} type="number" min={1} className="girdi girdi--kucuk" value={a.adet}
              onChange={(e) => agacDegistir(a.anahtar, { adet: e.target.value })} />
          </div>
          {kalem.agac.length > 1 && (
            <button type="button" className="btn-baglanti" onClick={() => degistir({ agac: kalem.agac.filter((x) => x.anahtar !== a.anahtar) })}>
              Kaldır
            </button>
          )}
        </div>
      ))}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={() => degistir({ agac: [...kalem.agac, yeniAgacSatiri()] })}>
          + Parça Ekle
        </button>
        <button type="button" className="btn btn--ikincil btn--kucuk" aria-expanded={yeniParca} onClick={() => setYeniParca(!yeniParca)}>
          + Listede Olmayan Parça
        </button>
      </div>
      {yeniParca && (
        <YeniParca
          kapat={() => setYeniParca(false)}
          eklendi={(id) => {
            setYeniParca(false);
            parcalariYenile();
            const bos = kalem.agac.find((a) => !a.parcaId);
            degistir({
              agac: bos
                ? kalem.agac.map((a) => (a.anahtar === bos.anahtar ? { ...a, parcaId: String(id) } : a))
                : [...kalem.agac, { ...yeniAgacSatiri(), parcaId: String(id) }],
            });
          }}
        />
      )}
    </div>
  );
}

function YeniParca({ kapat, eklendi }: { kapat: () => void; eklendi: (parcaId: number) => void }) {
  const [kod, setKod] = useState("");
  const [ad, setAd] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet() {
    setHata("");
    if (!kod.trim() || !ad.trim()) return setHata("Parça kodunu ve adını girin.");
    setBekliyor(true);
    try {
      const p = await parcalar.olustur({ kod: kod.trim(), ad: ad.trim() });
      eklendi(p.id);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Parça eklenemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <div className="kutu kutu--ince form-dikey"
      onKeyDown={(e) => { if (e.key === "Enter") { e.preventDefault(); kaydet(); } }}>
      <strong className="not">Yeni Parça</strong>
      <div className="satir-ekle satir-ekle--alt">
        <div className="alan">
          <label htmlFor="yp-kod">Parça Kodu</label>
          <input id="yp-kod" className="girdi girdi--mono" maxLength={50} value={kod} onChange={(e) => setKod(e.target.value)} />
        </div>
        <div className="alan alan--genis">
          <label htmlFor="yp-ad">Parça Adı</label>
          <input id="yp-ad" className="girdi" maxLength={200} value={ad} onChange={(e) => setAd(e.target.value)} />
        </div>
        <button type="button" className="btn btn--kucuk" disabled={bekliyor} onClick={kaydet}>Parçayı Ekle</button>
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Vazgeç</button>
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
    </div>
  );
}
