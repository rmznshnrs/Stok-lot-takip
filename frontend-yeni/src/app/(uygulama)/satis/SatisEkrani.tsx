"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Fragment, useState } from "react";
import { useSayfalama } from "@/components/Sayfalama";
import { satis, type SatisGecmisi } from "@/lib/api";
import { tarih, tarihSaat } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { seriAdresi, urunLotAdresi } from "@/lib/adres";
import SatisFormu from "./SatisFormu";


export default function SatisEkrani() {
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: gecmis, hata, yenile } = useVeri(() => satis.gecmis(aranan), [aranan]);
  const { gorunen, cubuk } = useSayfalama(gecmis, aranan);
  const sutun = 5;
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
        {hata && <div className="panel-ic"><div className="mesaj mesaj--error">{hata}</div></div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Müşteri</th>
                <th>Ürün</th>
                <th>Lot / seri no</th>
                <th className="sag">Adet</th>
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
                      <td>{s.zaman ? tarihSaat(s.zaman) : tarih(s.tarih)}</td>
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
                    </tr>
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

/** Satılan seri numaraları ürün lotuna göre gruplanır (lot sırası korunur). */
function lotGruplari(s: SatisGecmisi): [string, string[]][] {
  const kalemler = s.kalemler ?? s.seriNolar.map((seriNo) => ({ seriNo, lotNo: "" }));
  const gruplar = new Map<string, string[]>();
  for (const k of kalemler) gruplar.set(k.lotNo, [...(gruplar.get(k.lotNo) ?? []), k.seriNo]);
  return [...gruplar.entries()];
}
