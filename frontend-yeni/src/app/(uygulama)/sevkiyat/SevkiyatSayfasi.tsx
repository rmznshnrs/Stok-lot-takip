"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { useSayfalama } from "@/components/Sayfalama";
import { sevkiyat, type Sevkiyat, type SevkiyatDurumu } from "@/lib/api";
import { YAKIN_GUN, tarih, tarihSaat, yakinMi } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { useSiralama } from "@/lib/siralama";
import { useSatirSiniri } from "@/lib/satirSiniri";
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

type SevkiyatSuzgeci = SevkiyatDurumu | "acik" | "";
const SUZGECLER: { deger: SevkiyatSuzgeci; ad: string; uyar: (s: Sevkiyat) => boolean }[] = [
  { deger: "acik", ad: "Açık", uyar: (s) => s.durum !== "TeslimEdildi" },
  { deger: "Hazirlaniyor", ad: "Kargoya Verilecek", uyar: (s) => s.durum === "Hazirlaniyor" },
  { deger: "Kargoda", ad: "Kargoda", uyar: (s) => s.durum === "Kargoda" },
  { deger: "TeslimEdildi", ad: "Teslim Edildi", uyar: (s) => s.durum === "TeslimEdildi" && yakinMi(s.teslimZamani ?? s.kargoZamani ?? s.zaman) },
  { deger: "", ad: "Tümü", uyar: () => true },
];
const DURUM_SIRASI: Record<SevkiyatDurumu, number> = { Hazirlaniyor: 0, Kargoda: 1, TeslimEdildi: 2 };
const sonIslem = (s: Sevkiyat) => s.teslimZamani ?? s.kargoZamani ?? s.zaman ?? s.tarih;
const acilisSirasi = (a: Sevkiyat, b: Sevkiyat) =>
  DURUM_SIRASI[a.durum] - DURUM_SIRASI[b.durum]
  || (a.durum === "Hazirlaniyor" ? sonIslem(a).localeCompare(sonIslem(b)) : sonIslem(b).localeCompare(sonIslem(a)));
const eskiTeslim = (s: Sevkiyat) => s.durum === "TeslimEdildi" && !yakinMi(s.teslimZamani ?? s.kargoZamani ?? s.zaman);

function SevkiyatListesi() {
  const [durum, setDurum] = useState<SevkiyatSuzgeci>("acik");
  const [eskiler, setEskiler] = useState(false);
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: tumu, hata } = useVeri(() => sevkiyat.liste(aranan), [aranan]);
  const kosul = SUZGECLER.find((s) => s.deger === durum)!.uyar;
  const eskiGoster = durum === "TeslimEdildi" && (eskiler || !!aranan);
  const liste = tumu && tumu.filter((s) => kosul(s) || (eskiGoster && eskiTeslim(s))).sort(acilisSirasi);
  const eskiSayisi = durum === "TeslimEdildi" && !eskiGoster ? (tumu?.filter(eskiTeslim).length ?? 0) : 0;
  const { sirali, baslik, siralamaAnahtari } = useSiralama(liste, {
    no: (s) => s.no, tarih: (s) => s.teslimZamani ?? s.kargoZamani ?? s.zaman ?? s.tarih, musteri: (s) => s.musteriAd, siparisNo: (s) => s.siparisNo,
    kargo: (s) => s.kargoFirmasi, durum: (s) => s.durum,
  });
  const { gorunen, cubuk } = useSayfalama(sirali, `${aranan}|${durum}|${siralamaAnahtari}`);
  const tabloKap = useSatirSiniri<HTMLDivElement>(10, gorunen);

  return (
    <main className="icerik icerik--dikey">
      <section className="panel">
        <div className="panel-bas">
          <div className="sekmeler" role="radiogroup" aria-label="Durum">
            {SUZGECLER.map((s) => (
              <button key={s.deger} type="button" role="radio" aria-checked={durum === s.deger}
                className={`sekme${durum === s.deger ? " sekme--aktif" : ""}`} onClick={() => setDurum(s.deger)}>
                {s.ad}{tumu && ` ${tumu.filter(s.uyar).length}`}
              </button>
            ))}
          </div>
          <div className="arama-satiri">
            <input type="search" className="girdi girdi--ara" aria-label="Sevkiyatlarda ara" placeholder="Sipariş no, müşteri, takip no, seri no"
              value={arama} onChange={(e) => setArama(e.target.value)} />
            <Link href="/sevkiyat?yeni=1" className="btn btn--kucuk">+ Yeni Sevkiyat</Link>
          </div>
        </div>
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="tablo-kap" ref={tabloKap}>
          <table className="tablo">
            <thead>
              <tr>
                {baslik("no", "Sevkiyat No")}
                {baslik("tarih", "Tarih")}
                {baslik("musteri", "Müşteri")}
                {baslik("siparisNo", "Sipariş No")}
                <th>Ürünler</th>
                {baslik("kargo", "Kargo")}
                {baslik("durum", "Durum")}
              </tr>
            </thead>
            <tbody>
              {liste === null && !hata && <tr className="bos"><td colSpan={7}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && <tr className="bos"><td colSpan={7}>{aranan || durum ? "Sonuç yok." : "Henüz sevkiyat yok."}</td></tr>}
              {gorunen?.map((s) => (
                <tr key={s.id} className="satir-tikla">
                  <td className="mono"><Link href={sevkiyatAdresi(s.id)} className="satir-bag">{s.no ?? <span className="soluk">Girilmedi</span>}</Link></td>
                  <td>{(() => { const z = s.teslimZamani ?? s.kargoZamani ?? s.zaman; return z ? tarihSaat(z) : tarih(s.tarih); })()}</td>
                  <td><Link href={`/musteriler?id=${s.musteriId}`}>{s.musteriAd}</Link></td>
                  <td className="mono">{s.siparisNo ?? <span className="soluk">—</span>}</td>
                  <td>{urunOzeti(s) || <span className="soluk">{s.iadeler.length} ürün iade alındı</span>}</td>
                  <td>{s.takipNo ? <>{s.kargoFirmasi} · <span className="mono">{s.takipNo}</span></> : <span className="soluk">—</span>}</td>
                  <td>{sevkiyatRozeti(s)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        {eskiSayisi > 0 && (
          <div className="eskiler-satiri">
            <button type="button" className="btn btn--ikincil btn--kucuk" onClick={() => setEskiler(true)}>
              {YAKIN_GUN} Günden Eskileri Göster · {eskiSayisi}
            </button>
          </div>
        )}
        {cubuk}
      </section>
    </main>
  );
}
