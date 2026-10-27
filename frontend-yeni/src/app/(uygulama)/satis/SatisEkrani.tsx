"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Fragment, useState, type FormEvent } from "react";
import { useAdminMi } from "@/components/Oturum";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useSayfalama } from "@/components/Sayfalama";
import { ApiHatasi, satis, type SatisGecmisi } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { seriAdresi, urunLotAdresi } from "@/lib/adres";
import SatisFormu from "./SatisFormu";


export default function SatisEkrani() {
  const admin = useAdminMi();
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: gecmis, hata, yenile } = useVeri(() => satis.gecmis(aranan), [aranan]);
  const { gorunen, cubuk } = useSayfalama(gecmis, aranan);
  const [iadeSatiri, setIadeSatiri] = useState<string | null>(null);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const sutun = admin ? 6 : 5;
  const [formAcik, setFormAcik] = useState(useSearchParams().get("yeni") === "1");

  return (
    <main className="icerik icerik--dikey">
      <div className="sayfa-bas">
        <input
          type="search"
          className="girdi girdi--ara"
          aria-label="Satışlarda ara"
          value={arama}
          onChange={(e) => setArama(e.target.value)}
        />
        <button type="button" className="btn" aria-expanded={formAcik} onClick={() => setFormAcik(!formAcik)}>
          {formAcik ? "Formu kapat" : "+ Yeni satış"}
        </button>
      </div>

      {formAcik && <SatisFormu kaydedildi={yenile} />}

      <section className="panel">
        <div className="panel-bas"><h3>Satış geçmişi</h3></div>
        {(hata || mesaj) && (
          <div className="panel-ic">
            {hata && <div className="mesaj mesaj--error">{hata}</div>}
            <Mesaj mesaj={mesaj} />
          </div>
        )}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Müşteri</th>
                <th>Ürün</th>
                <th>Lot / seri no</th>
                <th className="sag">Adet</th>
                {admin && <th className="sutun-ikon"><span className="gorunmez">İade</span></th>}
              </tr>
            </thead>
            <tbody>
              {gecmis === null && !hata && <tr className="bos"><td colSpan={sutun}>Yükleniyor…</td></tr>}
              {gecmis?.length === 0 && (
                <tr className="bos">
                  <td colSpan={sutun}>{aranan ? `“${aranan}” için satış bulunamadı.` : "Henüz satış yok."}</td>
                </tr>
              )}
              {gorunen?.map((s) => {
                const anahtar = `${s.satisId}-${s.urunAd}`;
                return (
                  <Fragment key={anahtar}>
                    <tr>
                      <td>{tarih(s.tarih)}</td>
                      <td>{s.musteriAd}</td>
                      <td>{s.urunAd}</td>
                      <td className="mono">
                        {lotGruplari(s).map(([lot, seriler]) => (
                          <div key={lot} className="satis-lot">
                            {lot && <><Link href={urunLotAdresi(lot)} className="satis-lot-no">{lot}</Link>: </>}
                            {seriler.map((seri, i) => (
                              <span key={seri}>
                                {i > 0 && ", "}
                                <Link href={seriAdresi(seri)}>{seri}</Link>
                              </span>
                            ))}
                          </div>
                        ))}
                        {s.iadeEdilenler.length > 0 && (
                          <div className="yardim">
                            İade:{" "}
                            {s.iadeEdilenler.map((seri, i) => (
                              <span key={seri}>
                                {i > 0 && ", "}
                                <Link href={seriAdresi(seri)} className="iade-seri">{seri}</Link>
                              </span>
                            ))}
                          </div>
                        )}
                      </td>
                      <td className="sag">{s.seriNolar.length}</td>
                      {admin && (
                        <td>
                          {s.seriNolar.length > 0 && (
                            <button
                              type="button"
                              className="btn-baglanti"
                              aria-expanded={iadeSatiri === anahtar}
                              onClick={() => {
                                setMesaj(null);
                                setIadeSatiri(iadeSatiri === anahtar ? null : anahtar);
                              }}
                            >
                              İade al
                            </button>
                          )}
                        </td>
                      )}
                    </tr>
                    {iadeSatiri === anahtar && (
                      <tr>
                        <td colSpan={sutun}>
                          <IadeFormu
                            s={s}
                            kapat={() => setIadeSatiri(null)}
                            alindi={(metin) => {
                              setIadeSatiri(null);
                              setMesaj({ tur: "success", metin });
                              yenile();
                            }}
                          />
                        </td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
        {cubuk}
      </section>
    </main>
  );
}

/** Satıştan seçilen seri numaralarını açıklamayla iade alır; ürünler depoya döner. */
function IadeFormu({ s, kapat, alindi }: { s: SatisGecmisi; kapat: () => void; alindi: (metin: string) => void }) {
  const [secili, setSecili] = useState<string[]>(s.seriNolar.length === 1 ? s.seriNolar : []);
  const [aciklama, setAciklama] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    if (!secili.length) return setHata("Seri no seçin.");
    if (!window.confirm(`${secili.join(", ")} iade alınsın mı?`)) return;
    setBekliyor(true);
    setHata("");
    try {
      const r = await satis.iade(s.satisId, secili, aciklama.trim());
      alindi(`${r.seriNolar.join(", ")} iade alındı.`);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
      setBekliyor(false);
    }
  }

  return (
    <form className="iade-paneli" onSubmit={kaydet}>
      <strong>{s.musteriAd} · {tarih(s.tarih)} satışından iade</strong>
      <div className="iade-secim" role="group" aria-label="İade alınacak seri numaraları">
        {s.seriNolar.map((seri) => (
          <label key={seri}>
            <input
              type="checkbox"
              checked={secili.includes(seri)}
              onChange={() => setSecili(secili.includes(seri) ? secili.filter((x) => x !== seri) : [...secili, seri])}
            />
            <span className="mono">{seri}</span>
          </label>
        ))}
      </div>
      <div className="satir-ekle satir-ekle--alt">
        <div className="alan girdi--esnek">
          <label htmlFor={`iade-${s.satisId}`}>Açıklama</label>
          <input id={`iade-${s.satisId}`} className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
        </div>
        <button type="button" className="btn btn--ikincil" onClick={kapat}>Vazgeç</button>
        <button type="submit" className="btn" disabled={bekliyor || !secili.length}>
          {secili.length ? `${secili.length} ürünü iade al` : "Seri no seçin"}
        </button>
      </div>
      {hata && <span className="hata">{hata}</span>}
    </form>
  );
}

/** Satılan seri numaraları ürün lotuna göre gruplanır (lot sırası korunur). */
function lotGruplari(s: SatisGecmisi): [string, string[]][] {
  const kalemler = s.kalemler ?? s.seriNolar.map((seriNo) => ({ seriNo, lotNo: "" }));
  const gruplar = new Map<string, string[]>();
  for (const k of kalemler) gruplar.set(k.lotNo, [...(gruplar.get(k.lotNo) ?? []), k.seriNo]);
  return [...gruplar.entries()];
}
