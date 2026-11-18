"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import SeriSecimi from "@/components/SeriSecimi";
import { ApiHatasi, firmalar, sira, siparisler, urunler } from "@/lib/api";
import { bugun, tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import ParcaIhtiyaci from "./ParcaIhtiyaci";
import { siparisAdresi } from "./MusteriSiparisleri";

interface Kalem { anahtar: number; urunId: string; adet: string; seriler: string[] }

let sayac = 0;
const yeniKalem = (): Kalem => ({ anahtar: ++sayac, urunId: "", adet: "1", seriler: [] });

/**
 * Yeni sipariş: ürün ve adet; depoda hazır ürün varsa seri no seçilip siparişe ayrılır, kalan için ürün ağacı
 * (parça ihtiyacı) gösterilir. İstenirse eksikler kayıttan sonra üretim sırasına eklenir.
 */
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

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    const dolu = kalemler.filter((k) => k.urunId);
    if (!dolu.length) return setMesaj({ tur: "error", metin: "Ürün seçin." });
    setBekliyor(true);
    try {
      const s = await siparisler.olustur({
        musteriId,
        kalemler: dolu.map((k) => ({ urunId: Number(k.urunId), adet: Number(k.adet), seriNolar: k.seriler })),
        tarih: bugun(),
        istenenTarih: istenen || undefined,
        aciklama: aciklama.trim(),
      });
      // Eksikler sıraya: parça yetmeyen kalem siparişte "eksik" olarak kalır, sonradan eklenebilir
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
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Sipariş kaydedilemedi." });
      setBekliyor(false);
    }
  }

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir">
        <Link href={`/musteriler?id=${musteriId}`} className="not--buyuk">← {musteri?.ad ?? "Müşteri"}</Link>
      </div>
      <form className="kart kart--genis kart--bosluklu" onSubmit={kaydet}>
        <h2>Yeni Sipariş{musteri && ` · ${musteri.ad}`}</h2>
        <Mesaj mesaj={mesaj} />
        <div className="izgara-2">
          <div className="alan">
            <label htmlFor="sip-istenen">İstenen Teslim Tarihi <span className="soluk">(İsteğe bağlı)</span></label>
            <input id="sip-istenen" type="date" className="girdi" min={bugun()} value={istenen} onChange={(e) => setIstenen(e.target.value)} />
          </div>
          <div className="alan">
            <label htmlFor="sip-aciklama">Açıklama <span className="soluk">(İsteğe bağlı)</span></label>
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
          <input type="checkbox" checked={siraya} onChange={(e) => setSiraya(e.target.checked)} /> Depoda olmayanları üretim sırasına ekle
          {istenen && <span className="soluk"> (planlanan gün {tarih(istenen)})</span>}
        </label>
        <div className="kart-alt">
          <span />
          <button type="submit" className="btn" disabled={bekliyor}>{bekliyor ? "Kaydediliyor…" : "Siparişi Kaydet"}</button>
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
  const { veri: urunListesi } = useVeri(() => urunler.listele(), []);
  const urunId = Number(kalem.urunId) || 0;
  const { veri: hazir } = useVeri(() => (urunId ? siparisler.hazir(urunId) : Promise.resolve([])), [urunId]);
  const adet = Math.max(0, Math.floor(Number(kalem.adet)) || 0);
  const secili = kalem.seriler.slice(0, adet);
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
          </select>
        </div>
        <div className="alan">
          <label htmlFor={`k-adet-${kalem.anahtar}`}>Adet</label>
          <input id={`k-adet-${kalem.anahtar}`} type="number" min={1} max={999} required className="girdi"
            value={kalem.adet} onChange={(e) => degistir({ adet: e.target.value })} />
        </div>
      </div>
      {urunId > 0 && (
        <>
          <div className="bas-satir">
            <span className="not">
              Depoda hazır: {hazir?.length ?? "…"} · seçilen {secili.length} · üretilecek {uretilecek}
            </span>
            {hazir && hazir.length > 0 && (
              <button type="button" className="btn-baglanti"
                onClick={() => degistir({ seriler: hazir.slice(0, adet).map((h) => h.seriNo) })}>
                Hazırdan {Math.min(adet, hazir.length)} Tane Seç
              </button>
            )}
          </div>
          {hazir && hazir.length > 0 && (
            <SeriSecimi
              adaylar={hazir.map((h) => ({ seriNo: h.seriNo, alt: tarih(h.tarih) }))}
              secili={secili}
              setSecili={(s) => degistir({ seriler: s.slice(0, adet) })}
              bos="Depoda hazır ürün yok."
            />
          )}
          <ParcaIhtiyaci urunId={urunId} adet={uretilecek} />
        </>
      )}
    </fieldset>
  );
}
