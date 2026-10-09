"use client";

import Link from "next/link";
import { useState } from "react";
import { satis } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import SatisFormu from "./SatisFormu";

export default function SatisEkrani() {
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: gecmis, hata, yenile } = useVeri(() => satis.gecmis(aranan), [aranan]);

  return (
    <main className="icerik">
      <SatisFormu kaydedildi={yenile} />

      <section className="kart kart--genis">
        <div className="bas-satir">
          <h2>Satış geçmişi</h2>
          <input
            type="search"
            className="girdi girdi--ara"
            aria-label="Satışlarda ara"
            placeholder="Müşteri veya seri no ara…"
            value={arama}
            onChange={(e) => setArama(e.target.value)}
          />
        </div>
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Tarih</th>
                <th>Müşteri</th>
                <th>Ürün</th>
                <th>Seri No</th>
                <th className="sag">Adet</th>
              </tr>
            </thead>
            <tbody>
              {gecmis === null && !hata && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
              {gecmis?.length === 0 && (
                <tr className="bos">
                  <td colSpan={5}>{aranan ? `“${aranan}” için satış bulunamadı.` : "Henüz satış yok."}</td>
                </tr>
              )}
              {gecmis?.map((s) => (
                <tr key={`${s.satisId}-${s.urunAd}`}>
                  <td>{tarih(s.tarih)}</td>
                  <td>{s.musteriAd}</td>
                  <td>{s.urunAd}</td>
                  <td className="mono">
                    {s.seriNolar.map((seri, i) => (
                      <span key={seri}>
                        {i > 0 && ", "}
                        <Link href={`/izlenebilirlik?seri=${encodeURIComponent(seri)}`}>{seri}</Link>
                      </span>
                    ))}
                  </td>
                  <td className="sag">{s.seriNolar.length}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="not alt-not">Seri numarasına tıklayınca o ürünün içindeki parçalar ve lotları açılır.</div>
      </section>
    </main>
  );
}
