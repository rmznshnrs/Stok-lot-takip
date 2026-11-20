"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Fragment, useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, sira, type UretimEmri, type UretimSonucu } from "@/lib/api";
import { seriAdresi } from "@/lib/adres";
import { ETIKET_TURLERI, etiketTuruKaydet, kayitliEtiketTuru, type EtiketTuru } from "@/lib/etiket";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import UretimSekmeleri from "./UretimSekmeleri";
import { siparisAdresi, talepAdi } from "../musteriler/MusteriSiparisleri";
import SayiGirdi from "@/components/SayiGirdi";

export default function UretimSirasi({ uretimde }: { uretimde: boolean }) {
  const params = useSearchParams();
  const eklendi = Number(params.get("eklendi")) || null;
  const { veri: tumu, hata, yenile } = useVeri(() => sira.liste(), []);
  const basladi = (e: UretimEmri) => !!e.baslamaZamani || e.uretilen > 0;
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim().toLocaleLowerCase("tr"));
  const liste = tumu && tumu.filter((e) => basladi(e) === uretimde
    && (!aranan || `${e.urunAd} ${e.urunKod} ${e.musteriAd ?? ""} ${e.aciklama}`.toLocaleLowerCase("tr").includes(aranan)));
  const [acik, setAcik] = useState<number | null>(null);
  const [sonuc, setSonuc] = useState<UretimSonucu | null>(null);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [surukle, setSurukle] = useState<number | null>(null);
  const [hedef, setHedef] = useState<number | null>(null);
  const tasi = (id: number, hedefId: number | undefined) => hedefId !== undefined && hedefId !== id && calistir(() => sira.konum(id, hedefId));

  async function calistir(is_: () => Promise<unknown>, basari?: string) {
    setMesaj(null);
    try {
      await is_();
      if (basari) setMesaj({ tur: "success", metin: basari });
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "İşlem yapılamadı." });
    } finally {
      yenile();
    }
  }

  const sutun = 7;
  return (
    <main className="icerik icerik--dikey">
      {sonuc && <UretimSonucKutusu sonuc={sonuc} kapat={() => setSonuc(null)} />}
      <section className="panel">
        <div className="panel-bas">
          <div className="baslik-grup baslik-grup--yan">
            <UretimSekmeleri />
            {liste && (
              <span className="sayac-grubu">
                <span className="sayac"><b>{liste.length}</b> İş</span>
                <span className="sayac"><b>{liste.reduce((t, e) => t + e.adet - e.uretilen, 0)}</b> Adet</span>
              </span>
            )}
          </div>
          <div className="arama-satiri">
            <input type="search" className="girdi girdi--ara" aria-label="Üretimde ara" placeholder="Ürün ya da müşteri"
              value={arama} onChange={(e) => setArama(e.target.value)} />
            <Link href="/uretim?yeni=1" className="btn btn--kucuk">+ Sıraya Ekle</Link>
          </div>
        </div>
        <Mesaj mesaj={mesaj} />
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th className="hucre-dar">Sıra</th>
                <th>Ürün</th>
                <th className="sag">Adet</th>
                <th>Talep</th>
                <th>Planlanan Gün</th>
                <th>Ayrılan Parçalar</th>
                <th><span className="gorunmez">İşlem</span></th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !hata && <tr className="bos"><td colSpan={sutun}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && (
                <tr className="bos"><td colSpan={sutun}>{uretimde ? "Üretimde iş yok." : "Sırada iş yok."}</td></tr>
              )}
              {liste?.map((e, i) => (
                <Fragment key={e.id}>
                  <tr className={[e.id === eklendi && "secili-satir", !uretimde && "surukle-satir", hedef === e.id && surukle !== e.id && (liste.findIndex((x) => x.id === surukle) < i ? "surukle-hedef surukle-hedef--alt" : "surukle-hedef"), surukle === e.id && "surukleniyor"].filter(Boolean).join(" ") || undefined}
                    draggable={!uretimde}
                    onDragStart={(ev) => { setSurukle(e.id); ev.dataTransfer.effectAllowed = "move"; }}
                    onDragOver={(ev) => { if (surukle !== null) { ev.preventDefault(); setHedef(e.id); } }}
                    onDrop={(ev) => { ev.preventDefault(); if (surukle !== null) tasi(surukle, e.id); setSurukle(null); setHedef(null); }}
                    onDragEnd={() => { setSurukle(null); setHedef(null); }}>
                    <td className="hucre-dar">{!uretimde && <span className="surukle-tutamak" title="Sürükleyip istediğin yere bırak" aria-hidden="true">⠿</span>}{i + 1}</td>
                    <td>
                      {e.urunAd} <span className="mono yardim">{e.urunKod}</span>
                      {e.aciklama && <div className="yardim">{e.aciklama}</div>}
                    </td>
                    <td className="sag">
                      {e.uretilen > 0 && e.durum === "Bekliyor" ? <>{e.uretilen} / {e.adet}</> : e.adet}
                    </td>
                    <td>
                      {e.siparisId ? (
                        <Link href={siparisAdresi(e.siparisId)}>{talepAdi(e.musteriAd, e.talepTarihi)}</Link>
                      ) : (
                        <span className="soluk">Stok için</span>
                      )}
                    </td>
                    {(
                      <>
                        <td>
                          <input type="date" className="girdi girdi--kucuk" aria-label="Planlanan gün" defaultValue={e.planlananTarih}
                            onBlur={(ev) => ev.target.value && ev.target.value !== e.planlananTarih
                              && calistir(() => sira.planla(e.id, ev.target.value))} />
                        </td>
                        <td className="yardim">
                          {e.parcalar.map((p) => `${p.parca.kod} ${p.ayrilan}`).join(" · ")}
                        </td>
                        <td className="hucre-dar sag">
                          <div className="dugme-grup dugme-grup--sik">
                            {uretimde ? (
                              <button type="button" className="btn btn--kucuk" aria-expanded={acik === e.id}
                                onClick={() => setAcik(acik === e.id ? null : e.id)}>Üretildi</button>
                            ) : (<>
                              <span className="sira-dugmeleri" role="group" aria-label="Sırayı değiştir">
                                <button type="button" className="sira-dugme" title="En üste al" aria-label="En üste al" disabled={i === 0}
                                  onClick={() => tasi(e.id, liste[0]?.id)}>⇈</button>
                                <button type="button" className="sira-dugme" title="Bir yukarı" aria-label="Bir yukarı" disabled={i === 0}
                                  onClick={() => tasi(e.id, liste[i - 1]?.id)}>↑</button>
                                <button type="button" className="sira-dugme" title="Bir aşağı" aria-label="Bir aşağı" disabled={i === liste.length - 1}
                                  onClick={() => tasi(e.id, liste[i + 1]?.id)}>↓</button>
                                <button type="button" className="sira-dugme" title="En alta al" aria-label="En alta al" disabled={i === liste.length - 1}
                                  onClick={() => tasi(e.id, liste[liste.length - 1]?.id)}>⇊</button>
                              </span>
                              <button type="button" className="btn btn--kucuk"
                                onClick={() => calistir(() => sira.basla(e.id), `${e.urunAd} üretime alındı; Üretimde sekmesinde.`)}>Üretime Başla</button>
                            </>)}
                            <button type="button" className="btn btn--ikincil btn--kucuk"
                              onClick={() => confirm(`${e.urunAd} işi sıradan çıkarılsın mı? Ayrılan parçalar bırakılır.`)
                                && calistir(() => sira.iptal(e.id), "İş sıradan çıkarıldı.")}>İptal</button>
                          </div>
                        </td>
                      </>
                    )}
                  </tr>
                  {acik === e.id && (
                    <tr>
                      <td colSpan={sutun}>
                        <UretFormu
                          emir={e}
                          kapat={() => setAcik(null)}
                          uretildi={(s) => {
                            setAcik(null);
                            setSonuc(s);
                            setMesaj(null);
                            yenile();
                          }}
                        />
                      </td>
                    </tr>
                  )}
                </Fragment>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}

function UretFormu({ emir, kapat, uretildi }: { emir: UretimEmri; kapat: () => void; uretildi: (s: UretimSonucu) => void }) {
  const kalan = emir.adet - emir.uretilen;
  const [adet, setAdet] = useState(String(kalan));
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      uretildi(await sira.uret(emir.id, Number(adet)));
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Üretim kaydedilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <form className="satir-ekle" onSubmit={kaydet}>
      <label htmlFor={`uret-${emir.id}`}>Üretilen adet (kalan {kalan})</label>
      <SayiGirdi id={`uret-${emir.id}`} min={1} max={kalan} required kucuk value={adet} onChange={setAdet} />
      <button type="submit" className="btn btn--kucuk" disabled={bekliyor}>{bekliyor ? "Kaydediliyor…" : "Kaydet"}</button>
      <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Vazgeç</button>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
    </form>
  );
}

function UretimSonucKutusu({ sonuc, kapat }: { sonuc: UretimSonucu; kapat: () => void }) {
  const [etiketTuru, setEtiketTuru] = useState<EtiketTuru>(kayitliEtiketTuru);
  return (
    <div className="sonuc-kutu" role="status">
      <div className="bas-satir">
        <div className="sonuc-baslik">Üretildi, stoğa girdi: {sonuc.seriNolar.length} adet {sonuc.urunAd}</div>
        <button type="button" className="btn-baglanti" onClick={kapat}>Kapat</button>
      </div>
      <div className="seri-listesi">
        {sonuc.seriNolar.map((s) => (
          <Link key={s} href={seriAdresi(s)} className="seri-cip">{s}</Link>
        ))}
      </div>
      <div className="etiket-tercih">
        <span className="etiket-metni">Etiket</span>
        <div className="sekmeler" role="radiogroup" aria-label="Etikette barkod">
          {ETIKET_TURLERI.map((t) => (
            <button
              key={t.deger}
              type="button"
              role="radio"
              aria-checked={etiketTuru === t.deger}
              className={`sekme${etiketTuru === t.deger ? " sekme--aktif" : ""}`}
              onClick={() => {
                setEtiketTuru(t.deger);
                etiketTuruKaydet(t.deger);
              }}
            >
              {t.ad}
            </button>
          ))}
        </div>
        <Link
          href={`/etiket?seri=${sonuc.seriNolar.map(encodeURIComponent).join(",")}&tur=${etiketTuru}`}
          className="btn btn--kucuk"
        >
          Etiket yazdır
        </Link>
      </div>
    </div>
  );
}
