"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import GeriCagirmaEylemleri, { DURUM_ADI, acikMi } from "@/components/GeriCagirmaEylemleri";
import { useAdminMi } from "@/components/Oturum";
import { ApiHatasi, geriCagirma, hurdaAdi, izleme, type SeriIz } from "@/lib/api";
import { lotAdresi, urunLotAdresi } from "@/lib/adres";
import { tarih, tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

/** Seri no sayfası (Üretim geçmişi altında): ürün bilgisi, satış / iade / geri çağırma, içindeki parçalar ve lotları. */
export default function SeriSayfasi({ seriNo, geri }: { seriNo: string; geri: string }) {
  const { veri: iz, hata, yenile } = useVeri(async () => {
    try {
      return await izleme.seri(seriNo);
    } catch (e) {
      if (e instanceof ApiHatasi && e.durum === 404) return undefined;
      throw e;
    }
  }, [seriNo]);

  return (
    <main className="icerik icerik--dikey">
      <div className="bas-satir">
        <Link href={geri} className="not--buyuk">← Üretim geçmişi</Link>
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      {iz === null && !hata && <p className="yukleniyor">Yükleniyor…</p>}
      {iz === undefined && (
        <section className="kart kart--genis"><p className="not not--buyuk"><span className="mono">{seriNo}</span> bulunamadı.</p></section>
      )}
      {iz && <SeriDetayi iz={iz} degisti={yenile} />}
    </main>
  );
}

function SeriDetayi({ iz, degisti }: { iz: SeriIz; degisti: () => void }) {
  const admin = useAdminMi();
  const g = iz.geriCagirma && acikMi(iz.geriCagirma) ? iz.geriCagirma : null;
  return (
    <div className="akis">
      <section className="kart kart--iz">
        <div className="adim-baslik">Ürün</div>
        <div className="buyuk-numara">{iz.seriNo}</div>
        <div className="iz-ad">{iz.urunAd}</div>
        <div className="bilgi-liste">
          {iz.lotNo && (
            <div className="bilgi"><span>Ürün lot no</span><Link href={urunLotAdresi(iz.lotNo)} className="mono">{iz.lotNo}</Link></div>
          )}
          <div className="bilgi"><span>Üretim tarihi</span><span>{tarihSaat(iz.tarih)}</span></div>
          <div className="bilgi"><span>Durum</span><span>{iz.satis ? "Satıldı" : iz.hurda ? hurdaAdi(iz.hurda.tur) : "Depoda"}</span></div>
          {iz.hurda && (
            <div className="bilgi">
              <span>{hurdaAdi(iz.hurda.tur)} nedeni</span>
              <span>{iz.hurda.aciklama} · {iz.hurda.kullaniciAd}, {tarihSaat(iz.hurda.zaman)}</span>
            </div>
          )}
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
        {iz.iadeler.map((i) => (
          <div key={i.iadeZamani} className="bilgi-kutu alt-not">
            <strong>İade alındı</strong> · {i.musteriAd} ({tarih(i.satisTarihi)} satışı) · {tarihSaat(i.iadeZamani)}
            <br />
            {i.aciklama} · {i.kullaniciAd}
          </div>
        ))}
        {g && (
          <div className="uyari-kutu alt-not">
            <strong>Geri çağrıldı</strong> · {DURUM_ADI[g.durum]}
            <br />
            {g.aciklama} · {tarihSaat(g.zaman)}
            {admin && <div className="alt-not"><GeriCagirmaEylemleri g={g} degisti={degisti} /></div>}
          </div>
        )}
        {iz.geriCagrilanLotlar.length > 0 && (
          <div className="uyari-kutu alt-not">Hatalı lot: <span className="mono">{iz.geriCagrilanLotlar.join(", ")}</span></div>
        )}
        {admin && !g && !iz.hurda && <TekGeriCagir seriNo={iz.seriNo} degisti={degisti} />}
        <Link href={`/etiket?seri=${encodeURIComponent(iz.seriNo)}`} className="btn btn--ikincil btn--tam">Etiket yazdır</Link>
      </section>

      <section className="kart kart--genis kart--iz-sutun">
        <div className="adim-baslik">İçindeki parçalar ve lotları</div>
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Parça</th>
                <th className="sag">Adet</th>
                <th>Stok giriş no</th>
                <th>Tedarikçi</th>
                <th>Sipariş tarihi</th>
              </tr>
            </thead>
            <tbody>
              {iz.parcalar.map((p) => {
                const geri = p.lot.sorunlu;
                return (
                  <tr key={p.lot.id} className={geri ? "uyari-satir" : undefined}>
                    <td className={geri ? "kalin" : undefined}>{p.parca.ad} <span className="mono yardim">{p.parca.kod}</span></td>
                    <td className="sag">{p.adet}</td>
                    <td className="mono">
                      <Link href={lotAdresi(p.lot.lotNo)} className={geri ? "uyari-metin" : undefined}>
                        {p.lot.lotNo}
                      </Link>
                      {geri && <> <span className="lot-etiket lot-etiket--uyari">· hatalı</span></>}
                    </td>
                    <td>{p.lot.tedarikciAd}</td>
                    <td>{tarih(p.lot.siparisTarihi)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </section>
    </div>
  );
}

/** Yalnız yönetici: ürünü lot olmadan tek başına geri çağırır (ör. müşteri şikâyeti). */
function TekGeriCagir({ seriNo, degisti }: { seriNo: string; degisti: () => void }) {
  const [acik, setAcik] = useState(false);
  const [aciklama, setAciklama] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      await geriCagirma.geriCagir([seriNo], aciklama.trim());
      setAcik(false);
      degisti();
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Geri çağrılamadı.");
    } finally {
      setBekliyor(false);
    }
  }

  if (!acik) {
    return <button type="button" className="btn btn--ikincil btn--tam" onClick={() => setAcik(true)}>Bu ürünü geri çağır</button>;
  }
  return (
    <form className="form-dikey" onSubmit={kaydet}>
      <div className="alan">
        <label htmlFor="tek-geri">Geri çağırma nedeni</label>
        <input id="tek-geri" className="girdi" required maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
      <div className="dugme-grup">
        <button type="button" className="btn btn--ikincil" onClick={() => setAcik(false)}>Vazgeç</button>
        <button type="submit" className="btn btn--geri" disabled={bekliyor}>Geri çağır</button>
      </div>
    </form>
  );
}
