"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { useSayfalama } from "@/components/Sayfalama";
import { sevkiyat, type SevkiyatDurumu } from "@/lib/api";
import { tarih, tarihSaat } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { siparisAdresi } from "../musteriler/MusteriSiparisleri";
import SevkiyatDetay from "./SevkiyatDetay";
import SevkiyatFormu from "./SevkiyatFormu";
import { sevkiyatAdresi, sevkiyatRozeti, urunOzeti } from "./ortak";

export default function SevkiyatSayfasi() {
  const params = useSearchParams();
  const id = Number(params.get("id")) || null;
  const yeni = params.get("yeni") === "1";
  const siparisId = Number(params.get("siparis")) || null;
  return (
    <>
      <SayfaSekmeleri etiket="Sevkiyat" sekmeler={[]} />
      {id ? <SevkiyatDetay key={id} id={id} /> : yeni ? <SevkiyatFormu ilkSiparis={siparisId} /> : <SevkiyatListesi />}
    </>
  );
}

const SUZGECLER: { deger: SevkiyatDurumu | ""; ad: string }[] = [
  { deger: "", ad: "Tümü" },
  { deger: "Hazirlaniyor", ad: "Kargoya Verilecek" },
  { deger: "Kargoda", ad: "Kargoda" },
  { deger: "TeslimEdildi", ad: "Teslim Edildi" },
];

function SevkiyatListesi() {
  const [durum, setDurum] = useState<SevkiyatDurumu | "">("");
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata } = useVeri(() => sevkiyat.liste(aranan, durum || undefined), [aranan, durum]);
  const { gorunen, cubuk } = useSayfalama(liste, `${aranan}|${durum}`);

  return (
    <main className="icerik icerik--dikey">
      <section className="panel">
        <div className="panel-bas">
          <div className="sekmeler" role="radiogroup" aria-label="Durum">
            {SUZGECLER.map((s) => (
              <button key={s.deger} type="button" role="radio" aria-checked={durum === s.deger}
                className={`sekme${durum === s.deger ? " sekme--aktif" : ""}`} onClick={() => setDurum(s.deger)}>
                {s.ad}
              </button>
            ))}
          </div>
          <div className="arama-satiri">
            <input type="search" className="girdi girdi--ara" aria-label="Sevkiyatlarda ara" placeholder="No, müşteri, takip no, seri no"
              value={arama} onChange={(e) => setArama(e.target.value)} />
            <Link href="/sevkiyat?yeni=1" className="btn btn--kucuk">+ Yeni Sevkiyat</Link>
          </div>
        </div>
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Sevkiyat No</th>
                <th>Tarih</th>
                <th>Müşteri</th>
                <th>Sipariş</th>
                <th>Ürünler</th>
                <th>Kargo</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !hata && <tr className="bos"><td colSpan={7}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && <tr className="bos"><td colSpan={7}>{aranan || durum ? "Sonuç yok." : "Henüz sevkiyat yok."}</td></tr>}
              {gorunen?.map((s) => (
                <tr key={s.id} className="satir-tikla">
                  <td className="mono"><Link href={sevkiyatAdresi(s.id)} className="satir-bag">{s.no}</Link></td>
                  <td>{s.zaman ? tarihSaat(s.zaman) : tarih(s.tarih)}</td>
                  <td><Link href={`/musteriler?id=${s.musteriId}`}>{s.musteriAd}</Link></td>
                  <td className="mono">{s.siparisId ? <Link href={siparisAdresi(s.siparisId)}>{s.siparisNo}</Link> : <span className="soluk">—</span>}</td>
                  <td>{urunOzeti(s) || <span className="soluk">{s.iadeler.length} ürün iade alındı</span>}</td>
                  <td>{s.takipNo ? <>{s.kargoFirmasi} · <span className="mono">{s.takipNo}</span></> : <span className="soluk">—</span>}</td>
                  <td>{sevkiyatRozeti(s)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {cubuk}
      </section>
    </main>
  );
}
