import Link from "next/link";
import type { UrunDetay } from "@/lib/api";
import UrunDepo from "./UrunDepo";

/** Seçili ürünün salt okunur görünümü; değiştirmek için "Düzenle". */
export default function UrunBilgisi({ urun, uretimSayisi }: { urun: UrunDetay; uretimSayisi?: number }) {
  return (
    <>
      <div className="bas-satir">
        <h2>{urun.ad}</h2>
        <Link href={`/urunler?urun=${urun.id}&duzenle=1`} className="btn btn--ikincil btn--kucuk">Düzenle</Link>
      </div>

      <dl className="urun-bilgi">
        <div><dt>Ürün kodu</dt><dd className="mono">{urun.kod}</dd></div>
        <div><dt>Seri no öneki</dt><dd className="mono">{urun.seriOneki}</dd></div>
        {uretimSayisi !== undefined && <div><dt>Üretilen</dt><dd>{uretimSayisi} adet</dd></div>}
        <div><dt>Parça çeşidi</dt><dd>{urun.agac.length}</dd></div>
      </dl>

      <h3>Ürün ağacı (1 ürün için)</h3>

      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Parça</th>
              <th>Parça kodu</th>
              <th className="sag">Adet / ürün</th>
              <th className="sag">Stokta</th>
            </tr>
          </thead>
          <tbody>
            {urun.agac.length === 0 && (
              <tr className="bos"><td colSpan={4}>Ürün ağacı boş. Parça eklemek için Düzenle&apos;ye basın.</td></tr>
            )}
            {urun.agac.map((s) => (
              <tr key={s.parcaId}>
                <td>{s.parcaAd}</td>
                <td className="mono">{s.parcaKod}</td>
                <td className="sag">{s.adet} {s.birim}</td>
                <td className={`sag${s.minAltinda ? " uyari-metin" : ""}`}>{s.kullanilabilirStok}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <UrunDepo urunId={urun.id} />

      <div className="kart-alt">
        <Link href={`/uretim?urun=${urun.id}`} className="not--buyuk">Bu üründen üret →</Link>
      </div>
    </>
  );
}
