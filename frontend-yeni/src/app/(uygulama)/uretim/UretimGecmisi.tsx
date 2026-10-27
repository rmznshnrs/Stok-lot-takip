"use client";

import Link from "next/link";
import { useState } from "react";
import { hurdaAdi, uretim, urunler } from "@/lib/api";
import { tarih, tarihSaat } from "@/lib/bicim";
import { useSayfalama } from "@/components/Sayfalama";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { seriAdresi, urunLotAdresi } from "@/lib/adres";

/** Üretilen seri numaraları: ürün, tarih, depoda / satıldı / hurda, geri çağırma ve hatalı lot uyarısı. */
export default function UretimGecmisi({ ilkArama = "" }: { ilkArama?: string }) {
  const [arama, setArama] = useState(ilkArama);
  const [urunId, setUrunId] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata } = useVeri(() => uretim.gecmis(aranan, Number(urunId) || undefined), [aranan, urunId]);
  const { veri: tumUrunler } = useVeri(() => urunler.listele(), []);
  const { gorunen, cubuk } = useSayfalama(liste, `${aranan}|${urunId}`);
  const depoda = liste?.filter((u) => !u.musteriAd && !u.hurda).length ?? 0;
  const ayrilan = liste?.filter((u) => u.hurda).length ?? 0;

  return (
    <main className="icerik icerik--dikey">
      <section className="panel">
        <div className="panel-bas">
          <div className="baslik-grup">
            <h2>Üretim geçmişi</h2>
            {liste && <span className="ozet">{liste.length} ürün · {depoda} depoda · {liste.length - depoda - ayrilan} satıldı{ayrilan > 0 && ` · ${ayrilan} hurda / kayıp`}</span>}
          </div>
          <div className="arama-satiri">
            <select className="girdi" aria-label="Ürün" value={urunId} onChange={(e) => setUrunId(e.target.value)}>
              <option value="">Tüm ürünler</option>
              {tumUrunler?.map((u) => <option key={u.id} value={u.id}>{u.ad}</option>)}
            </select>
            <input
              type="search"
              className="girdi girdi--ara"
              aria-label="Üretimlerde ara"
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
                <th>Lot No</th>
                <th>Ürün</th>
                <th>Üretim tarihi</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !hata && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && (
                <tr className="bos">
                  <td colSpan={5}>{aranan || urunId ? "Aramaya uyan üretim bulunamadı." : "Henüz üretim yok."}</td>
                </tr>
              )}
              {gorunen?.map((u) => (
                <tr key={u.uretimId}>
                  <td className="mono">
                    <Link href={seriAdresi(u.seriNo)}>{u.seriNo}</Link>
                  </td>
                  <td className="mono">{u.lotNo && <Link href={urunLotAdresi(u.lotNo)} replace>{u.lotNo}</Link>}</td>
                  <td>{u.urunAd}</td>
                  <td>{tarihSaat(u.tarih)}</td>
                  <td>
                    {u.hurda ? (
                      <span title={`${u.hurda.kullaniciAd} · ${tarihSaat(u.hurda.zaman)}`}>
                        <span className="rozet rozet--kucuk rozet--uyari">{hurdaAdi(u.hurda.tur)}</span>{" "}
                        <span className="not">{u.hurda.aciklama}</span>
                      </span>
                    ) : u.musteriAd ? (
                      <>
                        <span className="rozet rozet--kucuk rozet--mavi">Satıldı</span> {u.musteriAd}
                        {u.satisTarihi && <span className="not"> · {tarih(u.satisTarihi)}</span>}
                      </>
                    ) : (
                      <span className="rozet rozet--kucuk rozet--basari">Depoda</span>
                    )}
                    {u.geriCagirmaDurumu && <> <span className="rozet rozet--kucuk rozet--uyari">Geri çağrıldı</span></>}
                    {u.geriCagrilanLotlar.length > 0 && (
                      <div className="yardim uyari-metin">
                        Hatalı lot içeriyor: <span className="mono">{u.geriCagrilanLotlar.join(", ")}</span>
                      </div>
                    )}
                  </td>
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
