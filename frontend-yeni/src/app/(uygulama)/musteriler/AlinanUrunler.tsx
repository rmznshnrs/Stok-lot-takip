"use client";

import Link from "next/link";
import { DURUM_ADI } from "@/components/GeriCagirmaEylemleri";
import { useSayfalama } from "@/components/Sayfalama";
import { SEVKIYAT_DURUM_ADI, geriCagirma, satis, sevkiyat } from "@/lib/api";
import { seriAdresi } from "@/lib/adres";
import { tarih, tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

interface Satir { satisId: number; seriNo: string; urunAd: string; zaman: string; iade: boolean }

export default function AlinanUrunler({ musteriId }: { musteriId: number }) {
  const { veri: satislar, hata } = useVeri(() => satis.gecmis(undefined, musteriId), [musteriId]);
  const { veri: acikGeri } = useVeri(() => geriCagirma.liste(true), []);
  const { veri: sevkiyatlar } = useVeri(() => sevkiyat.liste(undefined, undefined, musteriId), [musteriId]);
  const sevkiyatDurumu = (satisId: number) => {
    const d = sevkiyatlar?.find((v) => v.id === satisId)?.durum;
    return d ? SEVKIYAT_DURUM_ADI[d] : "Teslim Edildi";
  };
  const satirlar: Satir[] | null = satislar && satislar.flatMap((s) => {
    const zaman = s.zaman ? tarihSaat(s.zaman) : tarih(s.tarih);
    return [
      ...s.seriNolar.map((seriNo) => ({ satisId: s.satisId, seriNo, urunAd: s.urunAd, zaman, iade: false })),
      ...s.iadeEdilenler.map((seriNo) => ({ satisId: s.satisId, seriNo, urunAd: s.urunAd, zaman, iade: true })),
    ];
  });
  const { gorunen, cubuk } = useSayfalama(satirlar, String(musteriId));

  return (
    <div className="form-dikey">
      <h3>
        Aldığı Ürünler {satirlar && <span className="rozet rozet--kucuk rozet--mavi">{satirlar.filter((s) => !s.iade).length}</span>}
      </h3>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Seri No</th>
              <th>Ürün</th>
              <th>Sipariş No</th>
              <th>Gönderim Tarihi</th>
              <th>Durum</th>
            </tr>
          </thead>
          <tbody>
            {satirlar === null && !hata && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
            {satirlar?.length === 0 && <tr className="bos"><td colSpan={5}>Henüz ürün almadı.</td></tr>}
            {gorunen?.map((s) => {
              const g = s.iade ? undefined : acikGeri?.find((x) => x.seriNo === s.seriNo);
              return (
                <tr key={`${s.satisId}-${s.seriNo}-${s.iade}`} className={`satir-tikla${g ? " uyari-satir" : ""}`}>
                  <td className="mono"><Link href={seriAdresi(s.seriNo)} className="satir-bag">{s.seriNo}</Link></td>
                  <td>{s.urunAd}</td>
                  <td className="mono">{sevkiyatlar?.find((v) => v.id === s.satisId)?.siparisNo ?? <span className="soluk">—</span>}</td>
                  <td>{s.zaman}</td>
                  <td className={g ? "uyari-metin" : s.iade ? "soluk" : undefined}>
                    {g ? `Geri çağrıldı · ${DURUM_ADI[g.durum]}` : s.iade ? "İade alındı" : sevkiyatDurumu(s.satisId)}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
      {cubuk}
    </div>
  );
}
