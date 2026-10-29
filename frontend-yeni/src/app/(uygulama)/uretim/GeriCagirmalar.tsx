"use client";

import Link from "next/link";
import { useState } from "react";
import GeriCagirmaEylemleri, { DURUM_ADI, acikMi } from "@/components/GeriCagirmaEylemleri";
import { useAdminMi } from "@/components/Oturum";
import { geriCagirma } from "@/lib/api";
import { tarihSaat } from "@/lib/bicim";
import { useSayfalama } from "@/components/Sayfalama";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { lotAdresi, seriAdresi } from "@/lib/adres";

/** Geri çağrılan ürünler (seri no): müşteride / geri alındı / depoda / hurda / satışa açıldı. */
export default function GeriCagirmalar() {
  const admin = useAdminMi();
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: hamListe, hata, yenile } = useVeri(() => geriCagirma.liste(false, aranan), [aranan]);
  // Tek liste: açık olanlar üstte, sonuçlananlar altta (her grup kendi içinde yeniden eskiye)
  const liste = hamListe && [...hamListe.filter(acikMi), ...hamListe.filter((g) => !acikMi(g))];
  const { gorunen, cubuk } = useSayfalama(liste, aranan);
  const acik = hamListe?.filter(acikMi).length ?? 0;
  const musteride = hamListe?.filter((g) => g.durum === "musteride").length ?? 0;
  const sutun = admin ? 6 : 5;

  return (
    <main className="icerik icerik--dikey">
      <section className="panel">
        <div className="panel-bas">
          <div className="baslik-grup">
            <h2>Geri çağrılan ürünler</h2>
            {liste && <span className="ozet">{acik} açık · {musteride} müşteriden geri istendi · {liste.length - acik} kapandı</span>}
          </div>
          <div className="arama-satiri">
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
                <tr className="bos"><td colSpan={sutun}>{aranan ? "Aramaya uyan kayıt yok." : "Henüz geri çağırma yok."}</td></tr>
              )}
              {gorunen?.map((g) => (
                <tr key={g.id} className={g.durum === "musteride" ? "uyari-satir" : undefined}>
                  <td className="mono">
                    <Link href={seriAdresi(g.seriNo)}>{g.seriNo}</Link>
                  </td>
                  <td>{g.urunAd}</td>
                  <td>
                    <span className={`rozet rozet--kucuk ${acikMi(g) ? "rozet--uyari" : "rozet--basari"}`}>{DURUM_ADI[g.durum]}</span>
                    {g.bilgiVerildiZamani && <div className="yardim">Bilgi verildi · {tarihSaat(g.bilgiVerildiZamani)}</div>}
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
        {cubuk}
      </section>
    </main>
  );
}
