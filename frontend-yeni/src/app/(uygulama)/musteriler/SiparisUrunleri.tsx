"use client";

import Link from "next/link";
import { siparisler, type SiparisUrunDurumu, type SiparisUrunu } from "@/lib/api";
import { seriAdresi } from "@/lib/adres";
import { tarih, tarihSaat } from "@/lib/bicim";
import { takipAdresi } from "@/lib/kargo";
import { useVeri } from "@/lib/kancalar";
import { sevkiyatAdresi } from "../sevkiyat/ortak";

const DURUM: Record<SiparisUrunDurumu, [string, string]> = {
  sirada: ["Üretim Sırasında", ""],
  uretimde: ["Üretimde", "rozet--uyari"],
  depoda: ["Depoda, Gönderilecek", "rozet--uyari"],
  hurda: ["Hurdaya Ayrıldı", "rozet--hata"],
  Hazirlaniyor: ["Kargoya Verilecek", "rozet--uyari"],
  Kargoda: ["Kargoda", "rozet--mavi"],
  TeslimEdildi: ["Teslim Edildi", "rozet--basari"],
};

function Kargo({ u }: { u: SiparisUrunu }) {
  if (!u.sevkiyatId) return <span className="soluk">—</span>;
  const adres = u.takipNo ? takipAdresi(u.kargoFirmasi, u.takipNo) : null;
  return (
    <>
      <Link href={sevkiyatAdresi(u.sevkiyatId)} className="mono">{u.sevkiyatNo ?? "Sevkiyatı Aç"}</Link>
      {u.kargoFirmasi && <div className="yardim">{u.kargoFirmasi}{u.takipNo && <> · {adres
        ? <a href={adres} target="_blank" rel="noreferrer" className="mono">{u.takipNo}</a>
        : <span className="mono">{u.takipNo}</span>}</>}</div>}
    </>
  );
}

export default function SiparisUrunleri({ siparisId }: { siparisId: number }) {
  const { veri, hata } = useVeri(() => siparisler.urunler(siparisId), [siparisId]);
  if (hata) return <div className="mesaj mesaj--error">{hata}</div>;
  return (
    <div className="tablo-kap tablo-kap--tam">
      <table className="tablo tablo--sik">
        <thead>
          <tr>
            <th>Seri No</th>
            <th>Ürün</th>
            <th>Üretim</th>
            <th>Sevkiyat / Kargo</th>
            <th>Kargoya Verildi</th>
            <th>Teslim</th>
            <th>Durum</th>
          </tr>
        </thead>
        <tbody>
          {veri === null && <tr className="bos"><td colSpan={7}>Yükleniyor…</td></tr>}
          {veri?.length === 0 && <tr className="bos"><td colSpan={7}>Bu talepte henüz ürün yok.</td></tr>}
          {veri?.map((u, i) => {
            const [ad, renk] = DURUM[u.durum];
            return (
              <tr key={u.seriNo ?? `bekleyen-${i}`}>
                <td className="mono">
                  {u.seriNo ? <Link href={seriAdresi(u.seriNo)}>{u.seriNo}</Link> : <span className="soluk">{u.adet} adet</span>}
                </td>
                <td>{u.urunAd}</td>
                <td>
                  {u.uretimZamani ? tarihSaat(u.uretimZamani)
                    : u.planlananTarih ? <span className="soluk">Plan: {tarih(u.planlananTarih)}</span> : "—"}
                </td>
                <td><Kargo u={u} /></td>
                <td>{u.kargoZamani ? tarihSaat(u.kargoZamani) : <span className="soluk">—</span>}</td>
                <td>{u.teslimZamani ? tarihSaat(u.teslimZamani) : <span className="soluk">—</span>}</td>
                <td><span className={`rozet rozet--kucuk ${renk}`}>{ad}</span></td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
