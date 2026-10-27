"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, firmalar, type Iletisim } from "@/lib/api";
import { icerir } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

type Tur = "musteri" | "tedarikci";

/**
 * Müşteri veya tedarikçi listesi ve düzenleme formu. kok: bu listenin sayfa adresi
 * (ör. "/parcalar?sekme=tedarikciler"); seçim &id= ve &yeni=1 ile eklenir.
 */
export default function FirmaListesi({ tur, kok }: { tur: Tur; kok: string }) {
  const router = useRouter();
  const params = useSearchParams();
  const yeni = params.get("yeni") === "1";
  const seciliId = Number(params.get("id")) || null;
  const ad = tur === "musteri" ? "Müşteri" : "Tedarikçi";

  const { veri: liste, hata, yenile } = useVeri(() => (tur === "musteri" ? firmalar.musteriler() : firmalar.tedarikciler()), [tur]);
  const [arama, setArama] = useState("");
  const gorunen = liste?.filter((f) => icerir(`${f.ad} ${f.iletisim}`, arama.trim())) ?? [];
  const secili = liste?.find((f) => f.id === seciliId) ?? null;
  const adres = (ek = "") => `${kok}${ek}`;

  return (
    <main className="icerik">
      <section className="kart kart--liste">
        <div className="bas-satir bas-satir--liste">
          <h2>{tur === "musteri" ? "Müşteriler" : "Tedarikçiler"}</h2>
          <Link href={adres("&yeni=1")} className="btn btn--kucuk">+ Yeni</Link>
        </div>
        <input
          type="search"
          className="girdi girdi--liste-ara"
          aria-label={`${ad} ara`}
          value={arama}
          onChange={(e) => setArama(e.target.value)}
        />
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        {gorunen.map((f) => {
          const aktif = !yeni && f.id === seciliId;
          return (
            <Link
              key={f.id}
              href={adres(`&id=${f.id}`)}
              className={`liste-oge${aktif ? " liste-oge--secili" : ""}`}
              aria-current={aktif ? "page" : undefined}
            >
              <span className="liste-oge-ad">{f.ad}</span>
              <span className="liste-oge-alt">{f.telefon || f.eposta || "—"}</span>
            </Link>
          );
        })}
        {liste && gorunen.length === 0 && (
          <p className="not">{arama.trim() ? "Sonuç yok." : `${ad} yok.`}</p>
        )}
      </section>

      <section className="kart kart--genis kart--bosluklu">
        {yeni || secili ? (
          <FirmaFormu
            key={`${tur}-${yeni ? "yeni" : secili?.id}`}
            tur={tur}
            firma={yeni ? null : secili}
            kaydedildi={(f) => {
              yenile();
              router.replace(adres(`&id=${f.id}`));
            }}
          />
        ) : (
          <p className="not not--buyuk">
            Soldan seçin.
          </p>
        )}
      </section>
    </main>
  );
}

function FirmaFormu({ tur, firma, kaydedildi }: { tur: Tur; firma: Iletisim | null; kaydedildi: (f: Iletisim) => void }) {
  const [ad, setAd] = useState(firma?.ad ?? "");
  const [telefon, setTelefon] = useState(firma?.telefon ?? "");
  const [eposta, setEposta] = useState(firma?.eposta ?? "");
  const [adresMetni, setAdresMetni] = useState(firma?.adres ?? "");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);
  const baslik = tur === "musteri" ? "müşteri" : "tedarikçi";

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    setBekliyor(true);
    try {
      const istek = { ad: ad.trim(), telefon: telefon.trim(), eposta: eposta.trim(), adres: adresMetni.trim() };
      const f =
        tur === "musteri"
          ? await (firma ? firmalar.musteriGuncelle(firma.id, istek) : firmalar.musteriEkle(istek))
          : await (firma ? firmalar.tedarikciGuncelle(firma.id, istek) : firmalar.tedarikciEkle(istek));
      setMesaj({ tur: "success", metin: `${f.ad} kaydedildi.` });
      kaydedildi(f);
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <form className="form-dikey" onSubmit={kaydet}>
      <h2>{firma ? firma.ad : `Yeni ${baslik}`}</h2>
      <Mesaj mesaj={mesaj} />
      <div className="alan">
        <label htmlFor="firma-ad">Ad</label>
        <input id="firma-ad" className="girdi" required maxLength={200} value={ad} onChange={(e) => setAd(e.target.value)} />
      </div>
      <div className="izgara-2">
        <div className="alan">
          <label htmlFor="firma-telefon">Telefon</label>
          <input id="firma-telefon" type="tel" className="girdi" maxLength={50} autoComplete="off"
            value={telefon} onChange={(e) => setTelefon(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor="firma-eposta">E-posta</label>
          <input id="firma-eposta" type="email" className="girdi" maxLength={200} autoComplete="off"
            value={eposta} onChange={(e) => setEposta(e.target.value)} />
        </div>
      </div>
      <div className="alan">
        <label htmlFor="firma-adres">Adres</label>
        <textarea id="firma-adres" className="girdi girdi--cok-satir" maxLength={500} rows={3}
          value={adresMetni} onChange={(e) => setAdresMetni(e.target.value)} />
      </div>
      <div className="kart-alt">
        <span />
        <button type="submit" className="btn" disabled={bekliyor}>Kaydet</button>
      </div>
    </form>
  );
}
