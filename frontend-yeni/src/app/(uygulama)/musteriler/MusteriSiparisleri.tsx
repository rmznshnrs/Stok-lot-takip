"use client";

import Link from "next/link";
import { SIPARIS_DURUM_ADI, siparisler, type Siparis } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

export const siparisAdresi = (id: number) => `/musteriler?siparis=${id}`;

/** Durum rozetinin rengi: bitenler yeşil, iptal gri, diğerleri uyarı. */
export function durumRozeti(s: Siparis) {
  const renk = s.durum === "tamamlandi" || s.durum === "hazir" ? "rozet--basari" : s.durum === "iptal" ? "" : "rozet--uyari";
  return <span className={`rozet rozet--kucuk ${renk}`}>{SIPARIS_DURUM_ADI[s.durum]}</span>;
}

/** Müşterinin siparişleri (yeniden eskiye) ve yeni sipariş bağlantısı. */
export default function MusteriSiparisleri({ musteriId }: { musteriId: number }) {
  const { veri: liste, hata } = useVeri(() => siparisler.liste(musteriId), [musteriId]);
  return (
    <div className="form-dikey">
      <div className="bas-satir">
        <h3>Siparişler {liste && <span className="rozet rozet--kucuk rozet--mavi">{liste.length}</span>}</h3>
        <Link href={`/musteriler?siparis=yeni&musteri=${musteriId}`} className="btn btn--kucuk">+ Yeni Sipariş</Link>
      </div>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      <div className="tablo-kap">
        <table className="tablo tablo--sik">
          <thead>
            <tr>
              <th>Sipariş No</th>
              <th>Tarih</th>
              <th>Ürünler</th>
              <th>İstenen Teslim</th>
              <th>Durum</th>
            </tr>
          </thead>
          <tbody>
            {liste === null && !hata && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
            {liste?.length === 0 && <tr className="bos"><td colSpan={5}>Sipariş yok.</td></tr>}
            {liste?.map((s) => (
              <tr key={s.id} className="satir-tikla">
                <td className="mono"><Link href={siparisAdresi(s.id)} className="satir-bag">{s.no}</Link></td>
                <td>{tarih(s.tarih)}</td>
                <td>{s.kalemler.map((k) => `${k.urunAd} × ${k.adet}`).join(", ")}</td>
                <td>{s.istenenTarih ? tarih(s.istenenTarih) : <span className="soluk">—</span>}</td>
                <td>{durumRozeti(s)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
