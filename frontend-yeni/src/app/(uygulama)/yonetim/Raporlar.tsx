"use client";

const SURELER = [1, 3, 6, 12];
const SAYFALAR = ["Özet", "Siparişler", "Üretim Sırası", "Üretim", "Sevkiyat", "Mal Girişi", "Parça Stoğu", "Geri Çağırmalar"];

/** Excel raporu: seçilen sürenin kayıtları, uygulamadaki sayfaların düzeninde ayrı Excel sayfalarında. */
export default function Raporlar() {
  return (
    <section className="panel">
      <div className="panel-bas"><h3>Excel Raporu</h3></div>
      <div className="panel-ic form-dikey">
        <p className="not">Dosyada şu sayfalar var: {SAYFALAR.join(", ")}.</p>
        <div className="rapor-dugmeleri">
          {SURELER.map((ay) => (
            <a key={ay} href={`/api/ozet/excel?ay=${ay}`} download className="rapor-dugmesi">
              <b>Son {ay} Ay</b>
              <span>Excel İndir</span>
            </a>
          ))}
        </div>
      </div>
    </section>
  );
}
