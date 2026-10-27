"use client";

import Link from "next/link";
import { useState } from "react";
import HataliLotFormu from "@/components/HataliLotFormu";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useAdminMi } from "@/components/Oturum";
import { ApiHatasi, hurdaAdi, izleme, type LotIz, type LotIzUretim } from "@/lib/api";
import { seriAdresi } from "@/lib/adres";
import { tarih, tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

/** Lot sayfası (Stok altında): lot bilgisi → kullanıldığı ürünler → ürünlerin gittiği müşteriler. */
export default function LotSayfasi({ lotNo, geri }: { lotNo: string; geri: string }) {
  const { veri: iz, hata, yenile } = useVeri(async () => {
    try {
      return await izleme.lot(lotNo);
    } catch (e) {
      if (e instanceof ApiHatasi && e.durum === 404) return undefined;
      throw e;
    }
  }, [lotNo]);

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir">
        <Link href={geri} className="not--buyuk">← Stok listesi</Link>
        {iz && (
          <div className="ozet">
            {iz.uretimler.length} üründe kullanıldı ·{" "}
            <strong className={iz.musteriler.length ? "uyari-metin" : undefined}>{iz.musteriler.length} müşteriye ulaştı</strong>
            {" "}· {iz.stoktakiUretimler.length} ürün depoda
          </div>
        )}
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      {iz === null && !hata && <p className="yukleniyor">Yükleniyor…</p>}
      {iz === undefined && (
        <section className="kart kart--genis"><p className="not not--buyuk"><span className="mono">{lotNo}</span> bulunamadı.</p></section>
      )}
      {iz && <LotDetayi iz={iz} degisti={yenile} />}
    </main>
  );
}

function LotDetayi({ iz, degisti }: { iz: LotIz; degisti: () => void }) {
  const admin = useAdminMi();
  const { lot } = iz;
  const [form, setForm] = useState(false);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);

  return (
    <div className="akis">
      <section className={`kart kart--iz${lot.sorunlu ? " kart--geri" : ""}`}>
        <div className="adim-baslik">Stok girişi</div>
        <div className="buyuk-numara">{lot.lotNo}</div>
        <div className="iz-ad">{lot.parcaAd} <span className="mono yardim">{lot.parcaKod}</span></div>
        {lot.sorunlu && (
          <div className="uyari-kutu">
            <strong>Hatalı lot</strong> · {lot.sorunAciklama}
            <br />
            Kalan parçalar: {lot.geriCagrildi ? "üretimde kullanılmıyor" : "üretimde kullanılabilir"}
          </div>
        )}
        <Mesaj mesaj={mesaj} />
        <div className="bilgi-izgara">
          <Bilgi ad="Tedarikçi">{lot.tedarikciAd}</Bilgi>
          {lot.tedarikciLotNo && <Bilgi ad="Tedarikçi lot no"><span className="mono">{lot.tedarikciLotNo}</span></Bilgi>}
          <Bilgi ad="Sipariş tarihi">{tarih(lot.siparisTarihi)}</Bilgi>
          <Bilgi ad="Giriş zamanı">{tarihSaat(lot.girisZamani)}</Bilgi>
          <Bilgi ad="Gelen adet">{lot.girisAdet}</Bilgi>
          <Bilgi ad="Stokta kalan">{lot.kalanAdet}</Bilgi>
          {lot.siparisNo && <Bilgi ad="Sipariş no">{lot.siparisNo}</Bilgi>}
          <Bilgi ad="Kullanılan">{iz.kullanilanAdet}</Bilgi>
        </div>
        <Link href={`/etiket?lot=${encodeURIComponent(lot.lotNo)}`} className="btn btn--ikincil btn--tam">Etiket yazdır</Link>
        {admin && (
          <div className="geri-cagir-formu">
            {form ? (
              <HataliLotFormu
                lot={lot}
                kapat={() => setForm(false)}
                kaydedildi={(metin) => {
                  setForm(false);
                  setMesaj({ tur: "success", metin });
                  degisti();
                }}
              />
            ) : (
              <button type="button" className={`btn btn--tam ${lot.sorunlu ? "btn--ikincil" : "btn--geri"}`} onClick={() => setForm(true)}>
                {lot.sorunlu ? "Hatayı düzenle" : "Hatalı lot olarak işaretle"}
              </button>
            )}
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
            <UrunDurumu u={u} />
          </Link>
        ))}
        {!iz.uretimler.length && <p className="not">Kullanılmadı.</p>}
      </section>

      <div className="akis-ok" aria-hidden="true">→</div>

      <section className="kart kart--genis kart--iz-sutun">
        <div className="adim-baslik">Ürünlerin gittiği müşteriler</div>
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
        {!iz.musteriler.length && <p className="not">Müşteride ürün yok.</p>}
        {iz.stoktakiUretimler.length > 0 && (
          <div className={`${lot.sorunlu ? "uyari-kutu" : "bilgi-kutu"} alt-not`}>
            Depodaki{" "}
            {iz.stoktakiUretimler.map((u, i) => (
              <span key={u.uretimId}>{i > 0 && ", "}<span className="mono">{u.seriNo}</span></span>
            ))}{" "}
            bu lottan parça içeriyor.
          </div>
        )}
      </section>
    </div>
  );
}

function UrunDurumu({ u }: { u: LotIzUretim }) {
  if (u.hurdaTuru) return <span className="rozet">{hurdaAdi(u.hurdaTuru)}</span>;
  return (
    <span className="rozet-grup">
      {u.geriCagirmaDurumu && <span className="rozet rozet--uyari">Geri çağrıldı</span>}
      {u.satis ? <span className="rozet rozet--mavi">Müşteride</span> : <span className="rozet rozet--basari">Depoda</span>}
    </span>
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
