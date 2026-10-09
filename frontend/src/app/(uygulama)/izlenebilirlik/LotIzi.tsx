"use client";

import Link from "next/link";
import { useState } from "react";
import { useAdminMi } from "@/components/Oturum";
import { ApiHatasi, izleme, type LotIz } from "@/lib/api";
import { tarih, tarihSaat } from "@/lib/bicim";

const seriAdresi = (s: string) => `/izlenebilirlik?sekme=seri&seri=${encodeURIComponent(s)}`;

/** Lot → kullanıldığı ürünler → müşteriler (3 sütun). Geri çağırma yalnız Admin. */
export default function LotIzi({ iz, degisti }: { iz: LotIz; degisti: () => void }) {
  const admin = useAdminMi();
  const { lot } = iz;
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function geriCagir() {
    const soru = lot.geriCagrildi
      ? `${lot.lotNo} için geri çağırma kaldırılsın mı? Lot yeniden üretimde kullanılabilir olacak.`
      : `${lot.lotNo} geri çağrılsın mı? Lot üretimde kullanılmayacak; ${iz.uretimler.length} ürün ve ${iz.musteriler.length} müşteri etkileniyor.`;
    if (!window.confirm(soru)) return;
    setHata("");
    setBekliyor(true);
    try {
      await izleme.geriCagir(lot.lotNo, !lot.geriCagrildi);
      degisti();
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "İşlem yapılamadı.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <div className="akis">
      <section className={`kart kart--iz${lot.geriCagrildi ? " kart--geri" : ""}`}>
        <div className="adim-baslik">Parça lotu</div>
        <div className="buyuk-numara">{lot.lotNo}</div>
        <div className="iz-ad">{lot.parcaAd} <span className="mono yardim">{lot.parcaKod}</span></div>
        {lot.geriCagrildi && <span className="rozet rozet--uyari rozet--hizali">Geri çağrıldı</span>}
        <div className="bilgi-izgara">
          <Bilgi ad="Tedarikçi">{lot.tedarikciAd}</Bilgi>
          <Bilgi ad="Sipariş tarihi">{tarih(lot.siparisTarihi)}</Bilgi>
          <Bilgi ad="Giriş zamanı">{tarihSaat(lot.girisZamani)}</Bilgi>
          <Bilgi ad="Gelen adet">{lot.girisAdet}</Bilgi>
          <Bilgi ad="Stokta kalan">{lot.kalanAdet}</Bilgi>
          {lot.siparisNo && <Bilgi ad="Sipariş no">{lot.siparisNo}</Bilgi>}
          <Bilgi ad="Kullanılan">{iz.kullanilanAdet}</Bilgi>
        </div>
        {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
        {admin && (
          <div className="geri-cagir-formu">
            <button
              type="button"
              className={`btn btn--tam ${lot.geriCagrildi ? "btn--ikincil" : "btn--geri"}`}
              disabled={bekliyor}
              onClick={geriCagir}
            >
              {lot.geriCagrildi ? "Geri çağırmayı kaldır" : "Bu lotu geri çağır"}
            </button>
          </div>
        )}
      </section>

      <div className="akis-ok" aria-hidden="true">→</div>

      <section className="kart kart--genis kart--iz-sutun">
        <div className="adim-baslik">Kullanıldığı ürünler</div>
        {iz.uretimler.map((u) => (
          <Link key={u.uretimId} href={seriAdresi(u.seriNo)} className="akis-oge">
            <div className="baslik-grup">
              <span className="mono akis-numara">{u.seriNo}</span>
              <span className="not">{u.urunAd} · {u.adet} adet kullanıldı</span>
            </div>
            {u.satis ? <span className="rozet rozet--uyari">Satıldı</span> : <span className="rozet rozet--basari">Depoda</span>}
          </Link>
        ))}
        {!iz.uretimler.length && <p className="not">Bu lot henüz hiçbir üretimde kullanılmadı.</p>}
      </section>

      <div className="akis-ok" aria-hidden="true">→</div>

      <section className="kart kart--genis kart--iz-sutun">
        <div className="adim-baslik">Etkilenen müşteriler</div>
        {iz.musteriler.map((m) => (
          <div key={m.id} className="akis-oge akis-oge--dikey">
            <span className="iz-musteri">{m.ad}</span>
            {m.satislar.map((s) => (
              <span key={s.seriNo} className="not">
                <Link href={seriAdresi(s.seriNo)} className="mono">{s.seriNo}</Link> · {tarih(s.tarih)}
              </span>
            ))}
            {m.iletisim && <span className="not">{m.iletisim}</span>}
          </div>
        ))}
        {!iz.musteriler.length && <p className="not">Bu lottan yapılan ürünlerden henüz satılan yok.</p>}
        {iz.stoktakiUretimler.length > 0 && (
          <div className={`${lot.geriCagrildi ? "uyari-kutu" : "bilgi-kutu"} alt-not`}>
            Depodaki{" "}
            {iz.stoktakiUretimler.map((u, i) => (
              <span key={u.uretimId}>{i > 0 && ", "}<span className="mono">{u.seriNo}</span></span>
            ))}{" "}
            {lot.geriCagrildi ? "sevkiyattan önce ayrılmalı." : "bu lottan parça içeriyor."}
          </div>
        )}
      </section>
    </div>
  );
}

function Bilgi({ ad, children }: { ad: string; children: React.ReactNode }) {
  return (
    <div className="bilgi">
      <span>{ad}</span>
      <span>{children}</span>
    </div>
  );
}
