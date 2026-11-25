"use client";

import Link from "next/link";
import GeriIleri from "@/components/GeriIleri";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, sevkiyat, siparisler, type Sevkiyat } from "@/lib/api";
import { seriAdresi } from "@/lib/adres";
import { tarih, tarihSaat } from "@/lib/bicim";
import { KARGO_FIRMALARI, takipAdresi } from "@/lib/kargo";
import SecimVeyaYeni from "@/components/SecimVeyaYeni";
import { useVeri } from "@/lib/kancalar";
import { siparisAdresi } from "../musteriler/MusteriSiparisleri";
import { sevkiyatRozeti } from "./ortak";
import NumaraUyarisi from "@/components/NumaraUyarisi";

export default function SevkiyatDetay({ id }: { id: number }) {
  const { veri: s, hata, yenile } = useVeri(() => sevkiyat.getir(id), [id]);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [secilmeyen, setSecilmeyen] = useState<string[]>([]);

  async function teslim() {
    if (!s || !confirm(`${s.no ?? "Bu sevkiyat"} teslim edildi olarak işaretlensin mi?`)) return;
    setMesaj(null);
    try {
      await sevkiyat.teslim(s.id);
      setMesaj({ tur: "success", metin: "Teslim edildi olarak işaretlendi." });
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      yenile();
    }
  }

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir">
        <GeriIleri />
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      {s === null && !hata && <p className="yukleniyor">Yükleniyor…</p>}
      {s && (
        <div className="akis">
          <section className="kart kart--iz">
            <div className="adim-baslik">Sevkiyat</div>
            {s.durum === "Hazirlaniyor" ? (
              <div className="bas-satir">
                {s.no ? <div className="buyuk-numara">{s.no}</div> : <span className="soluk">Sevkiyat no kargoya verirken girilir</span>}
              </div>
            ) : (
              <SevkiyatNoBilgisi key={`sv-${s.no ?? ""}`} id={s.id} no={s.no} degisti={yenile} />
            )}
            <div className="iz-ad">{sevkiyatRozeti(s)}</div>
            <div className="bilgi-liste">
              <div className="bilgi"><span>Müşteri</span><Link href={`/musteriler?id=${s.musteriId}`}>{s.musteriAd}</Link></div>
              {s.siparisId && <div className="bilgi"><span>Talep</span><Link href={siparisAdresi(s.siparisId)}>Talebi Aç</Link></div>}
              {s.aciklama && <div className="bilgi"><span>Açıklama</span><span>{s.aciklama}</span></div>}
              <div className="bilgi"><span>Kaydeden</span><span>{s.kullaniciAd}</span></div>
            </div>
            {s.siparisId && <SiparisNoBilgisi key={`sp-${s.siparisNo ?? ""}`} siparisId={s.siparisId} siparisNo={s.siparisNo} degisti={yenile} />}
            <Adimlar s={s} />
            <Mesaj mesaj={mesaj} />
            <KargoBilgisi s={s} secili={s.kalemler.map((k) => k.seriNo).filter((x) => !secilmeyen.includes(x))}
              degisti={() => { setSecilmeyen([]); yenile(); }} />
            {s.durum !== "TeslimEdildi" && (
              <button type="button" className="btn btn--ikincil btn--tam" onClick={teslim}>Teslim Edildi</button>
            )}
          </section>

          <section className="kart kart--genis kart--iz-sutun">
            {(() => {
              const secimli = s.durum === "Hazirlaniyor" && s.kalemler.length > 1;
              const seciliSayi = s.kalemler.filter((k) => !secilmeyen.includes(k.seriNo)).length;
              const degistir = (seri: string) =>
                setSecilmeyen(secilmeyen.includes(seri) ? secilmeyen.filter((x) => x !== seri) : [...secilmeyen, seri]);
              return (
                <>
                  <div className="adim-baslik">
                    {s.durum === "Hazirlaniyor" ? "Kargoya Verilecek Ürünler" : "Gönderilen Ürünler"} · {s.kalemler.length}
                    {secimli && <span className="adim-ek"> · {seciliSayi} seçili</span>}
                  </div>
                  {secimli && <p className="not">Kargoya verilecekleri işaretleyin; seçilmeyenler kargoya verileceklerde bekler.</p>}
                  <div className="tablo-kap">
                    <table className="tablo">
                      <thead>
                        <tr>
                          {secimli && (
                            <th className="secim-sutun">
                              <input type="checkbox" aria-label="Hepsini seç" checked={seciliSayi === s.kalemler.length}
                                onChange={(e) => setSecilmeyen(e.target.checked ? [] : s.kalemler.map((k) => k.seriNo))} />
                            </th>
                          )}
                          <th>Seri No</th>
                          <th>Ürün</th>
                        </tr>
                      </thead>
                      <tbody>
                        {s.kalemler.map((k) => {
                          const secili = !secilmeyen.includes(k.seriNo);
                          return (
                            <tr key={k.seriNo} className={secimli ? `satir-tikla${secili ? " satir-secili" : ""}` : undefined}
                              onClick={secimli ? (e) => { if (!(e.target as HTMLElement).closest("a, input")) degistir(k.seriNo); } : undefined}>
                              {secimli && (
                                <td className="secim-sutun">
                                  <input type="checkbox" aria-label={`${k.seriNo} seç`} checked={secili} onChange={() => degistir(k.seriNo)} />
                                </td>
                              )}
                              <td className="mono"><Link href={seriAdresi(k.seriNo)}>{k.seriNo}</Link></td>
                              <td>{k.urunAd}</td>
                            </tr>
                          );
                        })}
                        {s.iadeler.map((seri) => (
                          <tr key={`iade-${seri}`} className="soluk">
                            {secimli && <td />}
                            <td className="mono"><Link href={seriAdresi(seri)}>{seri}</Link></td>
                            <td>İade alındı</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </>
              );
            })()}
          </section>
        </div>
      )}
    </main>
  );
}

function Adimlar({ s }: { s: Sevkiyat }) {
  const adimlar = [
    { ad: "Hazırlandı", zaman: s.zaman ?? s.olusturmaZamani, tamam: true },
    { ad: "Kargoya Verildi", zaman: s.kargoZamani, tamam: !!s.kargoZamani },
    { ad: "Teslim Edildi", zaman: s.teslimZamani, tamam: s.durum === "TeslimEdildi" },
  ];
  return (
    <ol className="kargo-adimlari">
      {adimlar.map((a) => (
        <li key={a.ad} className={a.tamam ? "kargo-adim kargo-adim--tamam" : "kargo-adim"}>
          <span className="kargo-adim-ad">{a.ad}</span>
          <span className="yardim">{a.zaman ? tarihSaat(a.zaman) : a.tamam ? "" : "Bekleniyor"}</span>
        </li>
      ))}
    </ol>
  );
}

function KargoBilgisi({ s, secili, degisti }: { s: Sevkiyat; secili: string[]; degisti: () => void }) {
  const [duzenle, setDuzenle] = useState(!s.takipNo && s.durum !== "TeslimEdildi");
  const [firma, setFirma] = useState(s.kargoFirmasi);
  const [takipNo, setTakipNo] = useState(s.takipNo);
  const hazirlaniyor = s.durum === "Hazirlaniyor";
  const tumu = s.kalemler.map((k) => k.seriNo);
  const [no, setNo] = useState(s.no ?? "");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const link = takipAdresi(s.kargoFirmasi, s.takipNo);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setHata("");
    if (hazirlaniyor && secili.length === 0) return setHata("Kargoya verilecek ürünleri seçin.");
    setBekliyor(true);
    try {
      await sevkiyat.kargo(s.id, firma.trim(), takipNo.trim(), hazirlaniyor ? secili : undefined, hazirlaniyor ? no.trim() : undefined);
      setDuzenle(false);
      degisti();
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  if (!duzenle) {
    return (
      <div className="form-dikey">
        {s.takipNo ? (
          <div className="bilgi-liste">
            <div className="bilgi"><span>Kargo Firması</span><span>{s.kargoFirmasi}</span></div>
            <div className="bilgi"><span>Takip No</span><span className="mono">{s.takipNo}</span></div>
          </div>
        ) : (
          <p className="not">Kargo bilgisi girilmedi{s.durum === "TeslimEdildi" && s.teslimZamani ? ` · teslim ${tarih(s.teslimZamani)}` : ""}.</p>
        )}
        <div className="dugme-grup">
          {link && <a href={link} target="_blank" rel="noopener noreferrer" className="btn btn--kucuk">Kargoda Takip Et ↗</a>}
          <button type="button" className="btn-baglanti" onClick={() => setDuzenle(true)}>
            {s.takipNo ? "Kargo Bilgisini Düzelt" : "Kargo Bilgisi Gir"}
          </button>
        </div>
      </div>
    );
  }
  return (
    <form className="form-dikey" onSubmit={kaydet}>
      {hazirlaniyor && tumu.length > 1 && (
        <p className="not">
          <b>{secili.length}/{tumu.length}</b> ürün kargoya verilecek. Seçimi sağdaki listeden değiştirin.
        </p>
      )}
      {hazirlaniyor && (
        <div className="alan">
          <label htmlFor="kargo-sv-no">Sevkiyat No</label>
          <input id="kargo-sv-no" className="girdi girdi--mono" maxLength={20} value={no} onChange={(e) => setNo(e.target.value)} />
          <NumaraUyarisi tur="sevkiyat" deger={no} haric={s.id} />
        </div>
      )}
      <div className="alan">
        <label htmlFor="kargo-firma">Kargo Firması</label>
        <KargoFirmaSecimi id="kargo-firma" required value={firma} onChange={setFirma} />
      </div>
      <div className="alan">
        <label htmlFor="kargo-takip">Takip No</label>
        <input id="kargo-takip" className="girdi girdi--mono" maxLength={100} required value={takipNo} onChange={(e) => setTakipNo(e.target.value)} />
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
      <div className="dugme-grup">
        {s.takipNo && <button type="button" className="btn btn--ikincil btn--kucuk" onClick={() => setDuzenle(false)}>Vazgeç</button>}
        <button type="submit" className="btn btn--kucuk" disabled={bekliyor}>{s.durum === "Hazirlaniyor" ? "Kargoya Verildi" : "Kaydet"}</button>
      </div>
    </form>
  );
}

function SevkiyatNoBilgisi({ id, no, degisti }: { id: number; no: string | null; degisti: () => void }) {
  const [duzenle, setDuzenle] = useState(false);
  const [deger, setDeger] = useState(no ?? "");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      await sevkiyat.no(id, deger.trim());
      setDuzenle(false);
      degisti();
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  if (!duzenle) {
    return (
      <div className="bas-satir">
        {no ? <div className="buyuk-numara">{no}</div> : <span className="soluk">Sevkiyat no girilmedi</span>}
        <button type="button" className="btn-baglanti" onClick={() => setDuzenle(true)}>{no ? "Düzelt" : "Sevkiyat No Gir"}</button>
      </div>
    );
  }
  return (
    <form className="form-dikey" onSubmit={kaydet}>
      <div className="alan">
        <label htmlFor="sevkiyat-no">Sevkiyat No</label>
        <input id="sevkiyat-no" className="girdi girdi--mono" maxLength={20} autoFocus value={deger} onChange={(e) => setDeger(e.target.value)} />
        <NumaraUyarisi tur="sevkiyat" deger={deger} haric={id} />
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={() => setDuzenle(false)}>Vazgeç</button>
        <button type="submit" className="btn btn--kucuk" disabled={bekliyor}>Kaydet</button>
      </div>
    </form>
  );
}

export function SiparisNoBilgisi({ siparisId, siparisNo, degisti }: { siparisId: number; siparisNo: string | null; degisti: () => void }) {
  const [duzenle, setDuzenle] = useState(false);
  const [no, setNo] = useState(siparisNo ?? "");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      await siparisler.siparisNo(siparisId, no.trim());
      setDuzenle(false);
      degisti();
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  if (!duzenle) {
    return (
      <div className="bas-satir siparis-no-satiri">
        <span>Sipariş No: {siparisNo ? <b className="mono">{siparisNo}</b> : <span className="soluk">Girilmedi</span>}</span>
        <button type="button" className="btn-baglanti" onClick={() => setDuzenle(true)}>{siparisNo ? "Düzelt" : "Sipariş No Gir"}</button>
      </div>
    );
  }
  return (
    <form className="form-dikey" onSubmit={kaydet}>
      <div className="alan">
        <label htmlFor="siparis-no">Sipariş No</label>
        <input id="siparis-no" className="girdi girdi--mono" maxLength={50} autoFocus value={no} onChange={(e) => setNo(e.target.value)} />
        <NumaraUyarisi tur="siparis" deger={no} haric={siparisId} />
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={() => setDuzenle(false)}>Vazgeç</button>
        <button type="submit" className="btn btn--kucuk" disabled={bekliyor}>Kaydet</button>
      </div>
    </form>
  );
}

export function KargoFirmaSecimi({ id, value, onChange, required }: { id: string; value: string; onChange: (v: string) => void; required?: boolean }) {
  return <SecimVeyaYeni id={id} secenekler={KARGO_FIRMALARI.map((k) => k.ad)} value={value} onChange={onChange} required={required} bosEtiketi="Kargo firması seçin" />;
}
