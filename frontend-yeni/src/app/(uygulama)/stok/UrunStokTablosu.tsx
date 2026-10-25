"use client";

import Link from "next/link";
import { Fragment, useState } from "react";
import { seriAdresi } from "@/lib/adres";
import { uretim, urunler, type UretimGecmisi } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";

/** Üretilmiş ürünlerin stoğu: ürün başına depoda (satılabilir), geri çağrılmış, satılan, hurda / kayıp ve toplam. */
export default function UrunStokTablosu({ aranan }: { aranan: string }) {
  const { veri: liste, hata } = useVeri(() => urunler.listele(), []);
  const { veri: gecmis, hata: gecmisHatasi } = useVeri(() => uretim.gecmis(), []);
  const [acik, setAcik] = useState<number | null>(null);

  const q = aranan.toLocaleLowerCase("tr");
  const satirlar = (liste ?? [])
    .map((u) => {
      const seriler = (gecmis ?? []).filter((g) => g.urunId === u.id);
      const depoda = seriler.filter((g) => !g.musteriAd && !g.hurda);
      return {
        urun: u,
        satilabilir: depoda.filter((g) => !g.geriCagirmaDurumu),
        geriCagrilmis: depoda.filter((g) => g.geriCagirmaDurumu),
        satilan: seriler.filter((g) => g.musteriAd && !g.hurda).length,
        hurda: seriler.filter((g) => g.hurda).length,
        toplam: seriler.length,
      };
    })
    .filter(({ urun, satilabilir, geriCagrilmis }) =>
      !q ||
      urun.ad.toLocaleLowerCase("tr").includes(q) ||
      urun.kod.toLocaleLowerCase("tr").includes(q) ||
      [...satilabilir, ...geriCagrilmis].some((g) => g.seriNo.toLocaleLowerCase("tr").includes(q)));

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
          <tr className="bos"><td colSpan={6}>{aranan ? `“${aranan}” için ürün bulunamadı.` : "Henüz ürün tanımlanmadı."}</td></tr>
        )}
        {satirlar.map((s) => {
          const depodakiler = [...s.satilabilir, ...s.geriCagrilmis];
          const acikMi = acik === s.urun.id;
          return (
            <Fragment key={s.urun.id}>
              <tr>
                <td>
                  {depodakiler.length > 0 ? (
                    <button type="button" className="btn-baglanti" aria-expanded={acikMi} onClick={() => setAcik(acikMi ? null : s.urun.id)}>
                      {s.urun.ad}
                    </button>
                  ) : (
                    s.urun.ad
                  )}{" "}
                  <span className="mono yardim">{s.urun.kod}</span>
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
                    <div className="not">Depodaki seri numaraları</div>
                    <div className="seri-listesi">
                      {depodakiler.map((g: UretimGecmisi) => (
                        <Link key={g.seriNo} href={seriAdresi(g.seriNo)} className={`seri-cip${g.geriCagirmaDurumu ? " seri-cip--uyari" : " seri-cip--mavi"}`}>
                          {g.seriNo}{g.geriCagirmaDurumu ? " · geri çağrıldı" : ""}
                        </Link>
                      ))}
                    </div>
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
