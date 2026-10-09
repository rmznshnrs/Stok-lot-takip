import Link from "next/link";
import type { SeriIz } from "@/lib/api";
import { tarih, tarihSaat } from "@/lib/bicim";

/** Seri no → içindeki parçalar ve lotları. */
export default function SeriIzi({ iz }: { iz: SeriIz }) {
  return (
    <div className="akis">
      <section className="kart kart--iz">
        <div className="adim-baslik">Ürün</div>
        <div className="buyuk-numara">{iz.seriNo}</div>
        <div className="iz-ad">{iz.urunAd}</div>
        <div className="bilgi-liste">
          <div className="bilgi"><span>Üretim tarihi</span><span>{tarihSaat(iz.tarih)}</span></div>
          <div className="bilgi"><span>Durum</span><span>{iz.satis ? "Satıldı" : "Depoda"}</span></div>
          <div className="bilgi">
            <span>Satış</span>
            {iz.satis ? (
              <span>{iz.satis.musteriAd} · {tarih(iz.satis.tarih)}</span>
            ) : (
              <span className="soluk">Henüz satılmadı</span>
            )}
          </div>
          {iz.satis?.musteriIletisim && (
            <div className="bilgi"><span>Müşteri iletişim</span><span>{iz.satis.musteriIletisim}</span></div>
          )}
        </div>
        {iz.geriCagrilanLotlar.length > 0 && (
          <div className="uyari-kutu alt-not">Bu üründe geri çağrılan {iz.geriCagrilanLotlar.length} lot var.</div>
        )}
      </section>

      <section className="kart kart--genis kart--iz-sutun">
        <div className="adim-baslik">İçindeki parçalar ve lotları</div>
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Parça</th>
                <th className="sag">Adet</th>
                <th>Lot No</th>
                <th>Tedarikçi</th>
                <th>Sipariş tarihi</th>
              </tr>
            </thead>
            <tbody>
              {iz.parcalar.map((p) => {
                const geri = p.lot.geriCagrildi;
                return (
                  <tr key={p.lot.id} className={geri ? "uyari-satir" : undefined}>
                    <td className={geri ? "kalin" : undefined}>{p.parca.ad} <span className="mono yardim">{p.parca.kod}</span></td>
                    <td className="sag">{p.adet}</td>
                    <td className="mono">
                      <Link href={`/izlenebilirlik?lot=${encodeURIComponent(p.lot.lotNo)}`} className={geri ? "uyari-metin" : undefined}>
                        {p.lot.lotNo}
                      </Link>
                      {geri && <> <span className="lot-etiket lot-etiket--uyari">· geri çağrıldı</span></>}
                    </td>
                    <td>{p.lot.tedarikciAd}</td>
                    <td>{tarih(p.lot.siparisTarihi)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        <div className="not alt-not">Bir lot numarasına tıklayınca o lotun kullanıldığı diğer ürünler ve müşteriler açılır.</div>
      </section>
    </div>
  );
}
