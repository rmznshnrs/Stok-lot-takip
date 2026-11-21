"use client";

import Link from "next/link";
import { Fragment, useState } from "react";
import { SIPARIS_DURUM_ADI, siparisler, type Siparis } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import SiparisUrunleri from "./SiparisUrunleri";

export const siparisAdresi = (id: number) => `/siparisler?id=${id}`;

export const talepAdi = (musteriAd: string | null, talepTarihi: string | null) =>
  `${musteriAd ?? ""}${talepTarihi ? ` · ${tarih(talepTarihi)} talebi` : ""}`;

export function durumRozeti(s: Siparis) {
  const renk = s.durum === "tamamlandi" || s.durum === "hazir" ? "rozet--basari" : s.durum === "iptal" ? "" : "rozet--uyari";
  return <span className={`rozet rozet--kucuk ${renk}`}>{SIPARIS_DURUM_ADI[s.durum]}</span>;
}

export default function MusteriSiparisleri({ musteriId }: { musteriId: number }) {
  const { veri: liste, hata } = useVeri(() => siparisler.liste(musteriId), [musteriId]);
  const [acik, setAcik] = useState<number | null>(null);
  return (
    <div className="form-dikey">
      <div className="bas-satir">
        <h3>Talepler {liste && <span className="rozet rozet--kucuk rozet--mavi">{liste.length}</span>}</h3>
        <Link href={`/siparisler?yeni=1&musteri=${musteriId}`} className="btn btn--kucuk">+ Yeni Talep</Link>
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Tarih</th>
              <th>Sipariş No</th>
              <th>Ürünler</th>
              <th>İstenen Teslim</th>
              <th>Durum</th>
            </tr>
          </thead>
          <tbody>
            {liste === null && !hata && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
            {liste?.length === 0 && <tr className="bos"><td colSpan={5}>Talep yok.</td></tr>}
            {liste?.map((s) => {
              const acikMi = acik === s.id;
              return (
                <Fragment key={s.id}>
                  <tr className="satir-tikla">
                    <td><Link href={siparisAdresi(s.id)}>{tarih(s.tarih)}</Link></td>
                    <td className="mono">{s.siparisNo ?? <span className="soluk">—</span>}</td>
                    <td>
                      <button type="button" className="btn-baglanti parca-ac satir-bag" aria-expanded={acikMi}
                        onClick={() => setAcik(acikMi ? null : s.id)}>
                        <span className="parca-ok" aria-hidden="true">{acikMi ? "▾" : "▸"}</span>
                        {s.kalemler.map((k) => `${k.urunAd} × ${k.adet}`).join(", ")}
                      </button>
                    </td>
                    <td>{s.istenenTarih ? tarih(s.istenenTarih) : <span className="soluk">—</span>}</td>
                    <td>{durumRozeti(s)}</td>
                  </tr>
                  {acikMi && (
                    <tr className="acilan-satir">
                      <td colSpan={5}>
                        <SiparisUrunleri siparisId={s.id} />
                      </td>
                    </tr>
                  )}
                </Fragment>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
