"use client";

import Link from "next/link";
import { useState } from "react";
import GeriCagirmaEylemleri, { DURUM_ADI, acikMi } from "@/components/GeriCagirmaEylemleri";
import { useAdminMi } from "@/components/Oturum";
import { geriCagirma } from "@/lib/api";
import { tarihSaat } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { lotAdresi, seriAdresi } from "@/lib/adres";

/** Geri çağrılan ürünler (seri no): müşteride / geri alındı / depoda / hurda / satışa açıldı. */
export default function GeriCagirmalar() {
  const admin = useAdminMi();
  const [hepsi, setHepsi] = useState(false);
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata, yenile } = useVeri(() => geriCagirma.liste(!hepsi, aranan), [hepsi, aranan]);
  const musteride = liste?.filter((g) => g.durum === "musteride").length ?? 0;
  const sutun = admin ? 6 : 5;

  return (
    <main className="icerik">
      <section className="kart kart--genis">
        <div className="bas-satir">
          <div className="baslik-grup">
            <h2>Geri çağrılan ürünler</h2>
            {liste && !hepsi && <span className="ozet">{liste.length} açık · {musteride} hâlâ müşteride</span>}
          </div>
          <div className="arama-satiri">
            <nav className="sekmeler" aria-label="Durum">
              <button type="button" className={`sekme${hepsi ? "" : " sekme--aktif"}`} onClick={() => setHepsi(false)}>Açık olanlar</button>
              <button type="button" className={`sekme${hepsi ? " sekme--aktif" : ""}`} onClick={() => setHepsi(true)}>Hepsi</button>
            </nav>
            <input
              type="search"
              className="girdi girdi--ara"
              aria-label="Geri çağırmalarda ara"
              value={arama}
              onChange={(e) => setArama(e.target.value)}
            />
          </div>
        </div>
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Seri No</th>
                <th>Ürün</th>
                <th>Durum</th>
                <th>Müşteri</th>
                <th>Neden</th>
                {admin && <th><span className="gorunmez">İşlem</span></th>}
              </tr>
            </thead>
            <tbody>
              {liste === null && !hata && <tr className="bos"><td colSpan={sutun}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && (
                <tr className="bos"><td colSpan={sutun}>{hepsi || aranan ? "Kayıt yok." : "Açık geri çağırma yok."}</td></tr>
              )}
              {liste?.map((g) => (
                <tr key={g.id} className={g.durum === "musteride" ? "uyari-satir" : undefined}>
                  <td className="mono">
                    <Link href={seriAdresi(g.seriNo)}>{g.seriNo}</Link>
                  </td>
                  <td>{g.urunAd}</td>
                  <td>
                    <span className={`rozet rozet--kucuk ${acikMi(g) ? "rozet--uyari" : "rozet--basari"}`}>{DURUM_ADI[g.durum]}</span>
                  </td>
                  <td>
                    {g.musteriAd ?? <span className="soluk">—</span>}
                    {g.musteriIletisim && <div className="yardim">{g.musteriIletisim}</div>}
                  </td>
                  <td>
                    {g.aciklama}
                    <div className="yardim">
                      {g.lotNo && <><Link href={lotAdresi(g.lotNo)} className="mono">{g.lotNo}</Link> · </>}
                      {tarihSaat(g.zaman)} · {g.kullaniciAd}
                    </div>
                  </td>
                  {admin && <td><GeriCagirmaEylemleri g={g} degisti={yenile} /></td>}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  );
}
