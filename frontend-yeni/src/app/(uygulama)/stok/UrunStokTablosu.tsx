"use client";

import { Fragment, useState } from "react";
import { uretim, urunler } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";
import UrunDepo from "./UrunDepo";
import { useSiralama } from "@/lib/siralama";

export default function UrunStokTablosu({ aranan }: { aranan: string }) {
  const { veri: liste, hata } = useVeri(() => urunler.listele(), []);
  const { veri: gecmis, hata: gecmisHatasi, yenile } = useVeri(() => uretim.gecmis(), []);
  const [acik, setAcik] = useState<number | null>(null);

  const q = aranan.toLocaleLowerCase("tr");
  const satirlar = (liste ?? [])
    .map((u) => {
      const seriler = (gecmis ?? []).filter((g) => g.urunId === u.id);
      const depoda = seriler.filter((g) => (!g.musteriAd || g.kargoyaVerilecek) && !g.hurda);
      return {
        urun: u,
        seriler,
        satilabilir: depoda.filter((g) => !g.geriCagirmaDurumu),
        geriCagrilmis: depoda.filter((g) => g.geriCagirmaDurumu),
        satilan: seriler.filter((g) => g.musteriAd && !g.kargoyaVerilecek && !g.hurda).length,
        hurda: seriler.filter((g) => g.hurda).length,
        toplam: seriler.length,
      };
    })
    .filter(({ urun, seriler }) =>
      !q ||
      urun.ad.toLocaleLowerCase("tr").includes(q) ||
      urun.kod.toLocaleLowerCase("tr").includes(q) ||
      seriler.some((g) => g.seriNo.toLocaleLowerCase("tr").includes(q)));

  const { sirali, baslik } = useSiralama(satirlar, {
    ad: (s) => s.urun.ad, depoda: (s) => s.satilabilir.length, geri: (s) => s.geriCagrilmis.length,
    giden: (s) => s.satilan, hurda: (s) => s.hurda, toplam: (s) => s.toplam,
  });
  const yukleniyor = (liste === null || gecmis === null) && !hata && !gecmisHatasi;

  return (
    <table className="tablo">
      <thead>
        <tr>
          {baslik("ad", "Ürün")}
          {baslik("depoda", "Depoda (gönderilebilir)", "sag")}
          {baslik("geri", "Geri çağrılmış", "sag")}
          {baslik("giden", "Gönderilen", "sag")}
          {baslik("hurda", "Hurda / kayıp", "sag")}
          {baslik("toplam", "Toplam üretilen", "sag")}
        </tr>
      </thead>
      <tbody>
        {(hata || gecmisHatasi) && <tr className="bos"><td colSpan={6}>{hata ?? gecmisHatasi}</td></tr>}
        {yukleniyor && <tr className="bos"><td colSpan={6}>Yükleniyor…</td></tr>}
        {!yukleniyor && satirlar.length === 0 && (
          <tr className="bos"><td colSpan={6}>{aranan ? "Sonuç yok." : "Ürün yok."}</td></tr>
        )}
        {(sirali ?? satirlar).map((s) => {
          const acikMi = acik === s.urun.id;
          return (
            <Fragment key={s.urun.id}>
              <tr className={s.toplam > 0 ? "satir-tikla" : undefined}>
                <td>
                  {s.toplam > 0 ? (
                    <button type="button" className="btn-baglanti satir-bag" aria-expanded={acikMi} onClick={() => setAcik(acikMi ? null : s.urun.id)}>
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
                    <UrunDepo urunId={s.urun.id} degisti={yenile} />
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
