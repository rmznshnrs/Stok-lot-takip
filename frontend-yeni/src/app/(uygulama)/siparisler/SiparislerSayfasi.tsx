"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Fragment, useState } from "react";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import { useSayfalama } from "@/components/Sayfalama";
import { firmalar, siparisler, type Siparis } from "@/lib/api";
import { bugun, tarih } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import { KalanGun } from "../AnaSayfaPanolari";
import { durumRozeti, siparisAdresi } from "../musteriler/MusteriSiparisleri";
import SiparisDetay from "../musteriler/SiparisDetay";
import SiparisFormu from "../musteriler/SiparisFormu";
import SiparisUrunleri from "../musteriler/SiparisUrunleri";

type Suzgec = "acik" | "geciken" | "tamamlandi" | "iptal" | "tumu";

const acikMi = (s: Siparis) => s.durum !== "tamamlandi" && s.durum !== "iptal";
const gecikti = (s: Siparis) => acikMi(s) && !!s.istenenTarih && s.istenenTarih < bugun();

const SUZGECLER: { deger: Suzgec; ad: string; uyar: (s: Siparis) => boolean }[] = [
  { deger: "acik", ad: "Açık", uyar: acikMi },
  { deger: "geciken", ad: "Geciken", uyar: gecikti },
  { deger: "tamamlandi", ad: "Tamamlanan", uyar: (s) => s.durum === "tamamlandi" },
  { deger: "iptal", ad: "İptal", uyar: (s) => s.durum === "iptal" },
  { deger: "tumu", ad: "Tümü", uyar: () => true },
];

export default function SiparislerSayfasi() {
  const params = useSearchParams();
  const id = Number(params.get("id")) || null;
  const yeni = params.get("yeni") === "1";
  const musteri = Number(params.get("musteri")) || 0;
  return (
    <>
      <SayfaSekmeleri etiket="Talepler" sekmeler={[]} />
      {id ? <SiparisDetay key={id} id={id} />
        : yeni && musteri ? <SiparisFormu musteriId={musteri} />
        : yeni ? <MusteriSecimi />
        : <SiparisListesi />}
    </>
  );
}

function MusteriSecimi() {
  const router = useRouter();
  const { veri: liste } = useVeri(() => firmalar.musteriler(), []);
  const [secili, setSecili] = useState("");
  return (
    <main className="icerik icerik--dikey">
      <section className="kart kart--bosluklu form-dikey">
        <h2>Yeni Talep</h2>
        <div className="alan">
          <label htmlFor="siparis-musteri">Müşteri</label>
          <select id="siparis-musteri" className="girdi" value={secili} onChange={(e) => setSecili(e.target.value)}>
            <option value="">Seçin</option>
            {liste?.map((m) => <option key={m.id} value={m.id}>{m.ad}</option>)}
          </select>
        </div>
        <div className="dugme-grup">
          <button type="button" className="btn" disabled={!secili}
            onClick={() => router.push(`/siparisler?yeni=1&musteri=${secili}`)}>Devam</button>
          <Link href="/siparisler" className="btn btn--ikincil">Vazgeç</Link>
        </div>
      </section>
    </main>
  );
}

function SiparisListesi() {
  const { veri: tumu, hata } = useVeri(() => siparisler.liste(), []);
  const [suzgec, setSuzgec] = useState<Suzgec>("acik");
  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim().toLocaleLowerCase("tr"));
  const [acik, setAcik] = useState<number | null>(null);
  const kosul = SUZGECLER.find((s) => s.deger === suzgec)!.uyar;
  const liste = tumu && tumu.filter((s) => kosul(s) && (!aranan
    || `${s.siparisNo ?? ""} ${s.musteriAd} ${s.aciklama} ${s.kalemler.map((k) => `${k.urunAd} ${k.urunKod} ${k.ayrilanlar.join(" ")}`).join(" ")}`
      .toLocaleLowerCase("tr").includes(aranan)));
  const { gorunen, cubuk } = useSayfalama(liste, `${suzgec}|${aranan}`);

  return (
    <main className="icerik icerik--dikey">
      <section className="panel">
        <div className="panel-bas">
          <div className="sekmeler" role="radiogroup" aria-label="Talep durumu">
            {SUZGECLER.map((s) => (
              <button key={s.deger} type="button" role="radio" aria-checked={suzgec === s.deger}
                className={`sekme${suzgec === s.deger ? " sekme--aktif" : ""}`} onClick={() => setSuzgec(s.deger)}>
                {s.ad}{tumu && ` ${tumu.filter(s.uyar).length}`}
              </button>
            ))}
          </div>
          <div className="arama-satiri">
            <input type="search" className="girdi girdi--ara" aria-label="Taleplerde ara" placeholder="Müşteri, ürün, sipariş no ya da seri no"
              value={arama} onChange={(e) => setArama(e.target.value)} />
            <Link href="/siparisler?yeni=1" className="btn btn--kucuk">+ Yeni Talep</Link>
          </div>
        </div>
        {hata && <div className="mesaj mesaj--error">{hata}</div>}
        <div className="tablo-kap">
          <table className="tablo">
            <thead>
              <tr>
                <th>Müşteri</th>
                <th>Talep Tarihi</th>
                <th>Ürünler</th>
                <th>İstenen Teslim</th>
                <th>Gönderilen</th>
                <th>Sipariş No</th>
                <th>Durum</th>
              </tr>
            </thead>
            <tbody>
              {liste === null && !hata && <tr className="bos"><td colSpan={7}>Yükleniyor…</td></tr>}
              {liste?.length === 0 && <tr className="bos"><td colSpan={7}>{aranan ? "Sonuç yok." : "Bu bölümde talep yok."}</td></tr>}
              {gorunen?.map((s) => {
                const ac = acik === s.id;
                const toplam = s.kalemler.reduce((t, k) => t + k.adet, 0);
                const giden = s.kalemler.reduce((t, k) => t + k.gonderilenler.length, 0);
                return (
                  <Fragment key={s.id}>
                    <tr className={`satir-tikla${gecikti(s) ? " uyari-satir" : ""}`}>
                      <td>{s.musteriAd}</td>
                      <td><Link href={siparisAdresi(s.id)}>{tarih(s.tarih)}</Link></td>
                      <td>
                        <button type="button" className="btn-baglanti parca-ac satir-bag" aria-expanded={ac}
                          onClick={() => setAcik(ac ? null : s.id)}>
                          <span className="parca-ok" aria-hidden="true">{ac ? "▾" : "▸"}</span>
                          {s.kalemler.map((k) => `${k.urunAd} × ${k.adet}`).join(", ")}
                        </button>
                      </td>
                      <td>
                        {s.istenenTarih ? <>{tarih(s.istenenTarih)} {acikMi(s) && <KalanGun tarihMetni={s.istenenTarih} />}</>
                          : <span className="soluk">—</span>}
                      </td>
                      <td>
                        <span className="ilerleme" aria-hidden="true">
                          <span style={{ width: `${toplam ? (giden / toplam) * 100 : 0}%` }} />
                        </span>
                        {giden}/{toplam}
                      </td>
                      <td className="mono">{s.siparisNo ?? <span className="soluk">—</span>}</td>
                      <td>{durumRozeti(s)}</td>
                    </tr>
                    {ac && (
                      <tr className="acilan-satir">
                        <td colSpan={7}><SiparisUrunleri siparisId={s.id} /></td>
                      </tr>
                    )}
                  </Fragment>
                );
              })}
            </tbody>
          </table>
        </div>
        {cubuk}
      </section>
    </main>
  );
}
