"use client";

import GeriIleri from "@/components/GeriIleri";
import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import SeriSecimi from "@/components/SeriSecimi";
import { ApiHatasi, sevkiyat, siparisler, type SatisUyarisi } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import { KargoFirmaListesi } from "./SevkiyatDetay";
import { sevkiyatAdresi } from "./ortak";

export default function SevkiyatFormu({ ilkSiparis }: { ilkSiparis: number | null }) {
  const router = useRouter();
  const { veri: acikSiparisler, hata } = useVeri(() => siparisler.liste(undefined, true), []);
  const [siparisId, setSiparisId] = useState(ilkSiparis ? String(ilkSiparis) : "");
  const siparis = acikSiparisler?.find((s) => String(s.id) === siparisId) ?? null;
  const bekleyenler = siparis?.kalemler.flatMap((k) =>
    k.ayrilanlar.filter((seri) => !k.gonderilenler.includes(seri)).map((seri) => ({ seriNo: seri, alt: k.urunAd }))) ?? [];
  const [secim, setSecim] = useState<string[] | null>(null);
  const secili = secim ?? bekleyenler.map((b) => b.seriNo);
  const [firma, setFirma] = useState("");
  const [takipNo, setTakipNo] = useState("");
  const [aciklama, setAciklama] = useState("");
  const [uyarilar, setUyarilar] = useState<SatisUyarisi[]>([]);
  const [onay, setOnay] = useState(false);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    if (!siparis) return setMesaj({ tur: "error", metin: "Talep seçin." });
    if (!secili.length) return setMesaj({ tur: "error", metin: "Gönderilecek ürünleri seçin." });
    setBekliyor(true);
    try {
      const s = await sevkiyat.olustur({
        siparisId: siparis.id, seriNolar: secili, kargoFirmasi: firma.trim(), takipNo: takipNo.trim(),
        aciklama: aciklama.trim(), geriCagrilanOnayi: onay,
      });
      router.push(sevkiyatAdresi(s.id));
    } catch (err) {
      if (err instanceof ApiHatasi && err.uyarilar.length) setUyarilar(err.uyarilar);
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Sevkiyat kaydedilemedi." });
      setBekliyor(false);
    }
  }

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir">
        <GeriIleri />
      </div>
      <form className="kart kart--genis kart--bosluklu" onSubmit={kaydet}>
        <h2>Yeni Sevkiyat</h2>
        <Mesaj mesaj={mesaj} />
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="alan">
          <label htmlFor="sv-siparis">Talep</label>
          <select id="sv-siparis" className="girdi" value={siparisId}
            onChange={(e) => { setSiparisId(e.target.value); setSecim(null); setUyarilar([]); setOnay(false); }}>
            <option value="">{acikSiparisler?.length === 0 ? "Açık talep yok" : "Seçin"}</option>
            {acikSiparisler?.map((s) => {
              const hazir = s.kalemler.reduce((t, k) => t + k.ayrilanlar.length - k.gonderilenler.length, 0);
              return (
                <option key={s.id} value={s.id} disabled={hazir === 0}>
                  {s.musteriAd} · {s.siparisNo ? `Sipariş ${s.siparisNo}` : `${tarih(s.tarih)} talebi`} · {hazir} ürün gönderime hazır{s.istenenTarih ? ` · teslim ${tarih(s.istenenTarih)}` : ""}
                </option>
              );
            })}
          </select>
        </div>
        {siparis && (
          <>
            <span className="not">
              {siparis.kalemler.map((k) => `${k.urunAd}: ${k.adet} istendi, ${k.gonderilenler.length} gönderildi`).join(" · ")}
            </span>
            <SeriSecimi adaylar={bekleyenler} secili={secili} setSecili={setSecim} bos="Bu talepte gönderime hazır ürün yok." />
          </>
        )}
        <div className="izgara-2">
          <div className="alan">
            <label htmlFor="sv-firma">Kargo Firması</label>
            <input id="sv-firma" className="girdi" list="kargo-firmalari" maxLength={100} value={firma} onChange={(e) => setFirma(e.target.value)} />
            <KargoFirmaListesi />
          </div>
          <div className="alan">
            <label htmlFor="sv-takip">Takip No</label>
            <input id="sv-takip" className="girdi girdi--mono" maxLength={100} value={takipNo} onChange={(e) => setTakipNo(e.target.value)} />
          </div>
        </div>
        <div className="alan">
          <label htmlFor="sv-aciklama">Açıklama</label>
          <input id="sv-aciklama" className="girdi" maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
        </div>
        {uyarilar.length > 0 && (
          <label className="onay-etiket uyari-metin">
            <input type="checkbox" checked={onay} onChange={(e) => setOnay(e.target.checked)} />
            Hatalı lotlu ürünleri yine de gönder ({uyarilar.map((u) => u.seriNo).join(", ")})
          </label>
        )}
        <div className="kart-alt">
          <span />
          <button type="submit" className="btn" disabled={bekliyor || !siparis}>
            {bekliyor ? "Kaydediliyor…" : `${secili.length} Ürünü Gönder`}
          </button>
        </div>
      </form>
    </main>
  );
}
