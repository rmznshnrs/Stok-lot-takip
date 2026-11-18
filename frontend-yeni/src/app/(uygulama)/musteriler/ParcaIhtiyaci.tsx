"use client";

import { uretim } from "@/lib/api";
import { useGecikmeli, useVeri } from "@/lib/kancalar";

/**
 * Ürün ağacı: adet kadar ürün üretmek için gereken parçalar, serbest stok (sıradaki işlere ayrılanlar düşülmüş)
 * ve eksikler. Adet 0 ise hiçbir şey göstermez.
 */
export default function ParcaIhtiyaci({ urunId, adet }: { urunId: number; adet: number }) {
  const gecikmeli = useGecikmeli(adet, 300);
  const { veri, hata } = useVeri(
    () => (urunId && gecikmeli > 0 ? uretim.onizle(urunId, gecikmeli) : Promise.resolve(null)),
    [urunId, gecikmeli],
  );
  if (!urunId || adet <= 0) return null;
  if (hata) return <div className="mesaj mesaj--warning">{hata}</div>;
  if (!veri) return <p className="yukleniyor">Ürün ağacı yükleniyor…</p>;
  return (
    <div className="form-dikey">
      <span className="not">
        Ürün ağacı · {veri.adet} adet üretmek için
        {veri.yeterli ? <span className="durum-yeterli"> · parçalar yeterli</span> : <span className="uyari-metin"> · eksik parça var</span>}
      </span>
      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Parça</th>
              <th className="sag">Birim</th>
              <th className="sag">Gereken</th>
              <th className="sag">Serbest Stok</th>
              <th>Durum</th>
            </tr>
          </thead>
          <tbody>
            {veri.parcalar.map((p) => (
              <tr key={p.parca.id} className={p.eksik ? "uyari-satir" : undefined}>
                <td>{p.parca.ad} <span className="mono yardim">{p.parca.kod}</span></td>
                <td className="sag">{p.birimAdet}</td>
                <td className="sag">{p.gereken}</td>
                <td className="sag">{p.kullanilabilir}</td>
                {p.eksik ? <td className="uyari-metin">{p.eksik} eksik</td> : <td className="durum-yeterli">Yeterli</td>}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
