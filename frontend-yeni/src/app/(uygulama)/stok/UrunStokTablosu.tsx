"use client";

import Link from "next/link";
import { Fragment, useState } from "react";
import { useAdminMi } from "@/components/Oturum";
import { urunLotAdresi } from "@/lib/adres";
import { uretim, urunLot, urunler, type UretimGecmisi, type UrunLot } from "@/lib/api";
import { tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import UrunDepo from "./UrunDepo";
import UrunLotHataFormu from "./UrunLotHataFormu";

/**
 * Üretilmiş ürünlerin stoğu: ürün başına depoda (satılabilir), geri çağrılmış, satılan, hurda / kayıp ve toplam.
 * Ürüne tıklayınca lotları (hatalı işaretleme) ve depodaki seri numaraları (stoktan düşme) açılır.
 */
export default function UrunStokTablosu({ aranan, bildir }: { aranan: string; bildir: (mesaj: string) => void }) {
  const { veri: liste, hata } = useVeri(() => urunler.listele(), []);
  const { veri: gecmis, hata: gecmisHatasi, yenile } = useVeri(() => uretim.gecmis(), []);
  const { veri: lotlar, yenile: lotlariYenile } = useVeri(() => urunLot.liste(), []);
  const [acik, setAcik] = useState<number | null>(null);

  const q = aranan.toLocaleLowerCase("tr");
  const satirlar = (liste ?? [])
    .map((u) => {
      const seriler = (gecmis ?? []).filter((g) => g.urunId === u.id);
      const depoda = seriler.filter((g) => !g.musteriAd && !g.hurda);
      return {
        urun: u,
        seriler,
        satilabilir: depoda.filter((g) => !g.geriCagirmaDurumu),
        geriCagrilmis: depoda.filter((g) => g.geriCagirmaDurumu),
        satilan: seriler.filter((g) => g.musteriAd && !g.hurda).length,
        hurda: seriler.filter((g) => g.hurda).length,
        toplam: seriler.length,
      };
    })
    .filter(({ urun, seriler }) =>
      !q ||
      urun.ad.toLocaleLowerCase("tr").includes(q) ||
      urun.kod.toLocaleLowerCase("tr").includes(q) ||
      seriler.some((g) => g.seriNo.toLocaleLowerCase("tr").includes(q) || g.lotNo.toLocaleLowerCase("tr").includes(q)));

  const yukleniyor = (liste === null || gecmis === null) && !hata && !gecmisHatasi;

  return (
    <table className="tablo">
      <thead>
        <tr>
          <th>Ürün</th>
          <th className="sag">Depoda (satılabilir)</th>
          <th className="sag">Geri çağrılmış</th>
          <th className="sag">Satılan</th>
          <th className="sag">Hurda / kayıp</th>
          <th className="sag">Toplam üretilen</th>
        </tr>
      </thead>
      <tbody>
        {(hata || gecmisHatasi) && <tr className="bos"><td colSpan={6}>{hata ?? gecmisHatasi}</td></tr>}
        {yukleniyor && <tr className="bos"><td colSpan={6}>Yükleniyor…</td></tr>}
        {!yukleniyor && satirlar.length === 0 && (
          <tr className="bos"><td colSpan={6}>{aranan ? "Sonuç yok." : "Ürün yok."}</td></tr>
        )}
        {satirlar.map((s) => {
          const acikMi = acik === s.urun.id;
          const hataliLot = (lotlar ?? []).some((l) => l.urunId === s.urun.id && l.sorunlu);
          return (
            <Fragment key={s.urun.id}>
              <tr>
                <td>
                  {s.toplam > 0 ? (
                    <button type="button" className="btn-baglanti" aria-expanded={acikMi} onClick={() => setAcik(acikMi ? null : s.urun.id)}>
                      {s.urun.ad}
                    </button>
                  ) : (
                    s.urun.ad
                  )}{" "}
                  <span className="mono yardim">{s.urun.kod}</span>
                  {hataliLot && <> <span className="lot-etiket lot-etiket--uyari">· hatalı lot var</span></>}
                </td>
                <td className="sag">{s.satilabilir.length}</td>
                <td className={`sag${s.geriCagrilmis.length ? " uyari-metin" : " soluk"}`}>{s.geriCagrilmis.length}</td>
                <td className="sag">{s.satilan}</td>
                <td className={`sag${s.hurda ? "" : " soluk"}`}>{s.hurda}</td>
                <td className="sag">{s.toplam}</td>
              </tr>
              {acikMi && (
                <tr>
                  <td colSpan={6}>
                    <UrunAyrintisi
                      urunId={s.urun.id}
                      lotlar={(lotlar ?? []).filter((l) => l.urunId === s.urun.id)}
                      seriler={s.seriler}
                      degisti={(metin) => {
                        yenile();
                        lotlariYenile();
                        if (metin) bildir(metin);
                      }}
                    />
                  </td>
                </tr>
              )}
            </Fragment>
          );
        })}
      </tbody>
    </table>
  );
}

function UrunAyrintisi({ urunId, lotlar, seriler, degisti }: {
  urunId: number;
  lotlar: UrunLot[];
  seriler: UretimGecmisi[];
  degisti: (mesaj?: string) => void;
}) {
  const admin = useAdminMi();
  const [acikLot, setAcikLot] = useState<string | null>(null);
  const sutun = admin ? 8 : 7;
  return (
    <div className="form-dikey">
      <h3>Ürün lotları</h3>
      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Lot No</th>
              <th>Üretim tarihi</th>
              <th className="sag">Toplam</th>
              <th className="sag">Depoda</th>
              <th className="sag">Satılan</th>
              <th className="sag">Hurda / kayıp</th>
              <th>Durum</th>
              {admin && <th><span className="gorunmez">Hatalı lot</span></th>}
            </tr>
          </thead>
          <tbody>
            {lotlar.length === 0 && <tr className="bos"><td colSpan={sutun}>Lot yok.</td></tr>}
            {lotlar.map((l) => (
              <Fragment key={l.lotNo}>
                <tr className={l.sorunlu ? "uyari-satir" : undefined}>
                  <td className="mono">
                    <Link href={urunLotAdresi(l.lotNo)} className={l.sorunlu ? "uyari-metin" : undefined}>{l.lotNo}</Link>
                  </td>
                  <td>{tarihSaat(l.tarih)}</td>
                  <td className="sag">{l.toplam}</td>
                  <td className="sag">{l.depoda}</td>
                  <td className="sag">{l.satilan}</td>
                  <td className={`sag${l.hurdaKayip ? "" : " soluk"}`}>{l.hurdaKayip}</td>
                  <td className={l.sorunlu || l.acikGeriCagirma ? "uyari-metin" : "soluk"}>
                    {[
                      l.sorunlu && `Hatalı${l.hataliAdet ? ` · ${l.hataliAdet} hatalı` : ""} · ${l.sorunAciklama}`,
                      l.acikGeriCagirma > 0 && `${l.acikGeriCagirma} geri çağrıldı`,
                    ].filter(Boolean).join(" · ")}
                  </td>
                  {admin && (
                    <td className="hucre-dar sag">
                      <button
                        type="button"
                        className="btn-baglanti"
                        aria-expanded={acikLot === l.lotNo}
                        onClick={() => setAcikLot(acikLot === l.lotNo ? null : l.lotNo)}
                      >
                        {l.sorunlu ? "Hatayı düzenle" : "Hatalı işaretle"}
                      </button>
                    </td>
                  )}
                </tr>
                {acikLot === l.lotNo && (
                  <tr>
                    <td colSpan={sutun}>
                      <UrunLotHataFormu
                        lot={l}
                        seriler={seriler}
                        kapat={() => setAcikLot(null)}
                        kaydedildi={(metin) => {
                          setAcikLot(null);
                          degisti(metin);
                        }}
                      />
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
          </tbody>
        </table>
      </div>
      <UrunDepo urunId={urunId} degisti={() => degisti()} />
    </div>
  );
}
