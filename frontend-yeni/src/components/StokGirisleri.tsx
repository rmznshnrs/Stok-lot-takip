"use client";

import Link from "next/link";
import { Fragment, useState } from "react";
import HataliLotFormu from "@/components/HataliLotFormu";
import { AdetDuzeltFormu } from "@/components/LotIslemFormlari";
import { useAdminMi } from "@/components/Oturum";
import { lotAdresi } from "@/lib/adres";
import type { Lot } from "@/lib/api";
import { tarih } from "@/lib/bicim";

type LotIslemi = "hata" | "duzelt";

export default function StokGirisleri({ lotlar, islemler = false, degisti }: {
  lotlar: Lot[];
  islemler?: boolean;
  degisti?: (mesaj: string) => void;
}) {
  const admin = useAdminMi() && islemler;
  const [acik, setAcik] = useState<{ lotId: number; islem: LotIslemi } | null>(null);
  const sutun = admin ? 8 : 7;
  const ac = (lotId: number, islem: LotIslemi) =>
    setAcik(acik?.lotId === lotId && acik.islem === islem ? null : { lotId, islem });
  if (!lotlar.length) return <p className="not">Stok yok.</p>;
  return (
    <table className="tablo tablo--sik tablo--girisler">
      <thead>
        <tr>
          <th>Lot No</th>
          <th>Tedarikçi lot no</th>
          <th>Tedarikçi</th>
          <th>Sipariş tarihi</th>
          <th className="sag">Gelen</th>
          <th className="sag">Kalan</th>
          <th>Durum</th>
          {admin && <th><span className="gorunmez">İşlem</span></th>}
        </tr>
      </thead>
      <tbody>
        {lotlar.map((lot) => {
          const durum = lot.sorunlu
            ? `Hatalı${lot.geriCagrildi ? ", üretime kapalı" : ""}`
            : !lot.kalanAdet ? "Bitti" : "";
          return (
            <Fragment key={lot.id}>
              <tr className={`satir-tikla${lot.sorunlu ? " uyari-satir" : ""}`}>
                <td className="mono">
                  <Link href={lotAdresi(lot.lotNo)} className={`giris-no satir-bag${lot.sorunlu ? " uyari-metin" : ""}`}>{lot.lotNo}</Link>
                </td>
                <td className="mono soluk">{lot.tedarikciLotNo || "—"}</td>
                <td>{lot.tedarikciAd}</td>
                <td>{tarih(lot.siparisTarihi)}</td>
                <td className="sag">{lot.girisAdet}</td>
                <td className={`sag${!lot.kalanAdet ? " soluk" : ""}`}>{lot.kalanAdet}</td>
                <td className={lot.sorunlu ? "uyari-metin" : "soluk"}>
                  {durum}
                </td>
                {admin && (
                  <td className="hucre-dar sag">
                    <span className="dugme-grup dugme-grup--sag">
                      {([
                        ["hata", lot.sorunlu ? "Hatayı düzenle" : "Hatalı İşaretle"],
                        ["duzelt", "Stoktan düş"],
                      ] as const).map(([islem, ad]) => (
                        <button
                          key={islem}
                          type="button"
                          className="btn btn--ikincil btn--kucuk"
                          aria-expanded={acik?.lotId === lot.id && acik.islem === islem}
                          onClick={() => ac(lot.id, islem)}
                        >
                          {ad}
                        </button>
                      ))}
                    </span>
                  </td>
                )}
              </tr>
              {acik?.lotId === lot.id && (
                <tr>
                  <td colSpan={sutun}>
                    {(() => {
                      const ozellik = {
                        lot,
                        kapat: () => setAcik(null),
                        kaydedildi: (metin: string) => {
                          setAcik(null);
                          degisti?.(metin);
                        },
                      };
                      return acik.islem === "hata" ? <HataliLotFormu {...ozellik} />
                        : <AdetDuzeltFormu {...ozellik} />;
                    })()}
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
