"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import SeriSecimi from "@/components/SeriSecimi";
import { ApiHatasi, sevkiyat, sira, siparisler, type Siparis, type SiparisKalemi } from "@/lib/api";
import { seriAdresi } from "@/lib/adres";
import { bugun, tarih, tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import ParcaIhtiyaci from "./ParcaIhtiyaci";
import { durumRozeti } from "./MusteriSiparisleri";
import { sevkiyatAdresi, sevkiyatRozeti, urunOzeti } from "../sevkiyat/ortak";

/**
 * Sipariş ayrıntısı: her ürün için istenen, ayrılan (depodan / üretimden), üretimde, eksik ve gönderilen.
 * Depodan ürün ayrılabilir, ayrım kaldırılabilir, eksik üretim sırasına eklenebilir; sipariş iptal edilebilir.
 */
export default function SiparisDetay({ id }: { id: number }) {
  const params = useSearchParams();
  const ilkUyari = params.get("uyari");
  const { veri: s, hata, yenile } = useVeri(() => siparisler.getir(id), [id]);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(
    ilkUyari ? { tur: "warning", metin: `Sipariş kaydedildi; sıraya eklenemeyenler: ${ilkUyari}` } : null,
  );

  async function calistir(is_: () => Promise<unknown>, basari: string) {
    setMesaj(null);
    try {
      await is_();
      setMesaj({ tur: "success", metin: basari });
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "İşlem yapılamadı." });
    } finally {
      yenile();
    }
  }

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir">
        {s && <Link href={`/musteriler?id=${s.musteriId}`} className="not--buyuk">← {s.musteriAd}</Link>}
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      {s === null && !hata && <p className="yukleniyor">Yükleniyor…</p>}
      {s && (
        <section className="kart kart--genis kart--bosluklu">
          <div className="bas-satir">
            <h2><span className="mono">{s.no}</span> · {s.musteriAd} {durumRozeti(s)}</h2>
            {!s.iptalZamani && s.durum !== "tamamlandi" && (
              <button type="button" className="btn btn--ikincil btn--kucuk"
                onClick={() => confirm(`${s.no} iptal edilsin mi? Ayrılan ürünler serbest kalır, sıradaki üretimi kapatılır.`)
                  && calistir(() => siparisler.iptal(s.id), "Sipariş iptal edildi.")}>
                Siparişi İptal Et
              </button>
            )}
          </div>
          <div className="bilgi-liste">
            <div className="bilgi"><span>Sipariş Tarihi</span><span>{tarih(s.tarih)}</span></div>
            <div className="bilgi"><span>İstenen Teslim</span><span>{s.istenenTarih ? tarih(s.istenenTarih) : "—"}</span></div>
            {s.aciklama && <div className="bilgi"><span>Açıklama</span><span>{s.aciklama}</span></div>}
            <div className="bilgi"><span>Kaydeden</span><span>{s.kullaniciAd} · {tarihSaat(s.olusturmaZamani)}</span></div>
            {s.iptalZamani && <div className="bilgi"><span>İptal</span><span>{tarihSaat(s.iptalZamani)}</span></div>}
          </div>
          <Mesaj mesaj={mesaj} />
          <Sevkiyatlar siparis={s} />
          {s.kalemler.map((k) => (
            <KalemKutusu key={k.id} siparis={s} kalem={k} calistir={calistir} />
          ))}
        </section>
      )}
    </main>
  );
}

function KalemKutusu({ siparis, kalem: k, calistir }: {
  siparis: Siparis;
  kalem: SiparisKalemi;
  calistir: (is_: () => Promise<unknown>, basari: string) => Promise<void>;
}) {
  const acik = !siparis.iptalZamani;
  const bosYer = k.adet - k.ayrilanlar.length - k.uretimde;
  const [ayir, setAyir] = useState(false);
  const [secili, setSecili] = useState<string[]>([]);
  const { veri: hazir } = useVeri(() => (ayir ? siparisler.hazir(k.urunId) : Promise.resolve(null)), [ayir, k.urunId]);

  return (
    <div className="kutu form-dikey">
      <div className="bas-satir">
        <strong>{k.urunAd} <span className="mono yardim">{k.urunKod}</span> · {k.adet} adet</strong>
        <span className="not">
          Ayrılan {k.ayrilanlar.length} · üretimde {k.uretimde} · gönderilen {k.gonderilenler.length}
          {k.eksik > 0 && <span className="uyari-metin"> · {k.eksik} eksik</span>}
        </span>
      </div>
      {k.ayrilanlar.length > 0 && (
        <div className="seri-listesi">
          {k.ayrilanlar.map((seri) => {
            const gitti = k.gonderilenler.includes(seri);
            return (
              <span key={seri} className={`seri-cip${gitti ? " seri-cip--mavi" : ""}`} title={gitti ? "Gönderildi" : "Depoda, siparişe ayrılmış"}>
                <Link href={seriAdresi(seri)}>{seri}</Link>
                {acik && !gitti && (
                  <button type="button" className="btn-baglanti" aria-label={`${seri} ayrımını kaldır`}
                    onClick={() => calistir(() => siparisler.birak(k.id, seri), `${seri} serbest bırakıldı.`)}> ×</button>
                )}
              </span>
            );
          })}
        </div>
      )}
      {acik && bosYer > 0 && (
        <div className="dugme-grup">
          <button type="button" className="btn btn--ikincil btn--kucuk" aria-expanded={ayir} onClick={() => { setAyir(!ayir); setSecili([]); }}>
            Depodan Ayır
          </button>
          {k.eksik > 0 && (
            <button type="button" className="btn btn--kucuk"
              onClick={() => calistir(() => sira.ekle({ urunId: k.urunId, siparisKalemiId: k.id, planlananTarih: siparis.istenenTarih ?? bugun() }),
                `${k.eksik} adet ${k.urunAd} üretim sırasına eklendi.`)}>
              Eksik {k.eksik} Adedi Sıraya Ekle
            </button>
          )}
        </div>
      )}
      {ayir && (
        <>
          <SeriSecimi
            adaylar={hazir && hazir.map((h) => ({ seriNo: h.seriNo, alt: tarih(h.tarih) }))}
            secili={secili}
            setSecili={(x) => setSecili(x.slice(0, bosYer))}
            bos="Depoda serbest ürün yok."
          />
          {secili.length > 0 && (
            <div>
              <button type="button" className="btn btn--kucuk"
                onClick={() => calistir(async () => { await siparisler.ayir(k.id, secili); setAyir(false); }, `${secili.length} ürün siparişe ayrıldı.`)}>
                Seçilen {secili.length} Ürünü Ayır
              </button>
            </div>
          )}
        </>
      )}
      {acik && k.eksik > 0 && <ParcaIhtiyaci urunId={k.urunId} adet={k.eksik} />}
    </div>
  );
}

/** Siparişin sevkiyatları ve gönderime hazır ürün varsa yeni sevkiyat bağlantısı. */
function Sevkiyatlar({ siparis: s }: { siparis: Siparis }) {
  const { veri } = useVeri(() => sevkiyat.liste(undefined, undefined, s.musteriId), [s.musteriId, s.kalemler]);
  const liste = veri?.filter((x) => x.siparisId === s.id) ?? [];
  const hazir = s.kalemler.reduce((t, k) => t + k.ayrilanlar.length - k.gonderilenler.length, 0);
  if (!liste.length && (hazir === 0 || s.iptalZamani)) return null;
  return (
    <div className="kutu form-dikey">
      <div className="bas-satir">
        <strong>Sevkiyatlar</strong>
        {hazir > 0 && !s.iptalZamani && (
          <Link href={`/sevkiyat?yeni=1&siparis=${s.id}`} className="btn btn--kucuk">Sevkiyat Oluştur · {hazir} Hazır</Link>
        )}
      </div>
      {liste.map((x) => (
        <div key={x.id} className="bas-satir">
          <span>
            <Link href={sevkiyatAdresi(x.id)} className="mono">{x.no}</Link> · {x.zaman ? tarihSaat(x.zaman) : tarih(x.tarih)} · {urunOzeti(x)}
            {x.takipNo && <> · {x.kargoFirmasi} <span className="mono">{x.takipNo}</span></>}
          </span>
          {sevkiyatRozeti(x)}
        </div>
      ))}
    </div>
  );
}
