"use client";

import Link from "next/link";
import { ozet, sira, type AnaSayfaOzeti } from "@/lib/api";
import { bugun, tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

const GUN_ADLARI = ["Paz", "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt"];

function gunEkle(t: string, n: number) {
  const [y, a, g] = t.split("-").map(Number);
  const d = new Date(y, a - 1, g + n);
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

function gunFarki(t: string, bugunMetni: string) {
  const [y1, a1, g1] = bugunMetni.split("-").map(Number);
  const [y2, a2, g2] = t.split("-").map(Number);
  return Math.round((Date.UTC(y2, a2 - 1, g2) - Date.UTC(y1, a1 - 1, g1)) / 86_400_000);
}

export function KalanGun({ tarihMetni }: { tarihMetni: string }) {
  const fark = gunFarki(tarihMetni, bugun());
  const [yazi, tur] = fark < 0 ? [`${-fark} gün gecikti`, "gecikti"]
    : fark === 0 ? ["Bugün", "bugun"]
    : fark === 1 ? ["Yarın", "yakin"]
    : [`${fark} gün kaldı`, fark <= 3 ? "yakin" : "uzak"];
  return <span className={`kalan-gun kalan-gun--${tur}`}>{yazi}</span>;
}

export function OnumuzdekiGunler() {
  const bas = bugun();
  const bit = gunEkle(bas, 6);
  const { veri } = useVeri(() => ozet.takvim(bas, bit), [bas, bit]);
  const gunler = Array.from({ length: 7 }, (_, i) => gunEkle(bas, i));
  return (
    <section className="panel">
      <div className="panel-bas">
        <h3>Önümüzdeki 7 Gün</h3>
        <Link href="/takvim" className="btn btn--ikincil btn--kucuk">Takvimi Aç</Link>
      </div>
      <div className="panel-ic">
        <div className="yedi-gun">
          {gunler.map((t, i) => {
            const g = veri?.find((x) => x.tarih === t);
            const plan = g?.urunler.reduce((s, u) => s + u.planlanan, 0) ?? 0;
            const teslim = g?.urunler.reduce((s, u) => s + u.istenen, 0) ?? 0;
            const hatirlatici = g?.planlar.filter((p) => !p.tamamlandi).length ?? 0;
            const [y, a, gun] = t.split("-").map(Number);
            return (
              <Link key={t} href={`/takvim?ay=${t.slice(0, 7)}&gun=${t}`} className={`gun-karti${i === 0 ? " gun-karti--bugun" : ""}`}>
                <span className="gun-karti-ad">{i === 0 ? "Bugün" : GUN_ADLARI[new Date(y, a - 1, gun).getDay()]}</span>
                <span className="gun-karti-no">{gun}</span>
                <span className={`gun-karti-satir gun-karti-satir--plan${plan ? "" : " bos"}`}>{plan} üretim</span>
                <span className={`gun-karti-satir gun-karti-satir--ist${teslim ? "" : " bos"}`}>{teslim} teslim</span>
                {hatirlatici > 0 && <span className="gun-karti-satir gun-karti-satir--alim">{hatirlatici} hatırlatıcı</span>}
              </Link>
            );
          })}
        </div>
      </div>
    </section>
  );
}

export function SiradakiIsler() {
  const { veri: liste, hata } = useVeri(() => sira.liste(), []);
  return (
    <section className="panel">
      <div className="panel-bas">
        <h3>Üretim Sırası</h3>
        {liste && <span className="ozet">{liste.length} iş · {liste.reduce((t, e) => t + e.adet - e.uretilen, 0)} adet</span>}
        <Link href="/uretim" className="btn btn--ikincil btn--kucuk">Sırayı Aç</Link>
      </div>
      {hata && <div className="panel-ic"><div className="mesaj mesaj--error">{hata}</div></div>}
      <div className="tablo-kap tablo-kap--bes">
        <table className="tablo">
          <thead>
            <tr>
              <th className="hucre-dar">Sıra</th>
              <th>Planlanan Gün</th>
              <th>Ürün</th>
              <th className="sag">Kalan Adet</th>
              <th>Sipariş</th>
            </tr>
          </thead>
          <tbody>
            {liste === null && !hata && <tr className="bos"><td colSpan={5}>Yükleniyor…</td></tr>}
            {liste?.length === 0 && <tr className="bos"><td colSpan={5}>Sırada iş yok.</td></tr>}
            {liste?.map((e, i) => (
              <tr key={e.id} className="satir-tikla">
                <td className="hucre-dar"><span className="sira-no">{i + 1}</span></td>
                <td>{tarih(e.planlananTarih)} <KalanGun tarihMetni={e.planlananTarih} /></td>
                <td><Link href={`/uretim?eklendi=${e.id}`} className="satir-bag">{e.urunAd}</Link></td>
                <td className="sag"><b>{e.adet - e.uretilen}</b></td>
                <td>{e.siparisNo ? <>{e.siparisNo} · {e.musteriAd}</> : <span className="soluk">Stok için</span>}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export function YaklasanTeslimler({ ozet: o }: { ozet: AnaSayfaOzeti }) {
  return (
    <section className="panel">
      <div className="panel-bas"><h3>Yaklaşan Teslimler</h3></div>
      <div className="tablo-kap tablo-kap--bes">
        <table className="tablo">
          <thead>
            <tr>
              <th>Teslim</th>
              <th>Sipariş</th>
              <th>Müşteri</th>
              <th className="sag">Gönderilecek</th>
            </tr>
          </thead>
          <tbody>
            {o.yaklasanTeslimler.length === 0 && <tr className="bos"><td colSpan={4}>7 gün içinde teslim yok.</td></tr>}
            {o.yaklasanTeslimler.map((t) => (
              <tr key={t.siparisId} className="satir-tikla">
                <td>{tarih(t.istenenTarih)} <KalanGun tarihMetni={t.istenenTarih} /></td>
                <td className="mono"><Link href={`/musteriler?siparis=${t.siparisId}`} className="satir-bag">{t.no}</Link></td>
                <td>{t.musteriAd}</td>
                <td className="sag"><b>{t.kalan}</b></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export function UretimUyarilari({ ozet: o }: { ozet: AnaSayfaOzeti }) {
  const kapasiteler = (o.kapasiteler ?? []).filter((k) => k.uretilebilir <= 5);
  const enCok = 6;
  return (
    <section className={`panel${o.uyarilar.length ? " panel--dikkat" : ""}`}>
      <div className="panel-bas">
        <h3>Üretim Uyarıları</h3>
        {o.uyarilar.length > 0
          ? <span className="rozet rozet--uyari">{o.uyarilar.length} ürün için parça azaldı</span>
          : <span className="rozet rozet--basari">Bütün ürünler için yeterli parça var</span>}
      </div>
      <div className="panel-ic uyari-duzeni">
        {o.uyarilar.length > 0 && (
          <div className="uyari-kartlari">
            {o.uyarilar.map((u) => (
              <div key={u.urunId} className={`uyari-karti${u.uretilebilir === 0 ? " uyari-karti--kritik" : ""}`}>
                <div className="uyari-karti-bas">
                  <span className="uyari-karti-sayi">{u.uretilebilir}</span>
                  <span>
                    <strong>{u.urunAd}</strong>
                    <span className="yardim">{u.uretilebilir === 0 ? "Hiç üretilemiyor" : "adet daha üretilebilir"}</span>
                  </span>
                </div>
                {u.kisitlayanlar.map((k) => {
                  const kac = Math.floor(k.serbest / k.birimAdet);
                  return (
                    <Link key={k.parca.id} href={`/parcalar?parca=${encodeURIComponent(k.parca.kod)}`} className="parca-cubugu">
                      <span className="parca-cubugu-ad">{k.parca.ad} <span className="mono">{k.parca.kod}</span></span>
                      <span className="parca-cubugu-yol">
                        <span className="parca-cubugu-dolu" style={{ width: `${Math.min(100, (kac / 6) * 100)}%` }} />
                      </span>
                      <span className="parca-cubugu-sayi">{k.serbest} adet · 1 üründe {k.birimAdet}</span>
                    </Link>
                  );
                })}
              </div>
            ))}
          </div>
        )}
        {kapasiteler.length > 0 && (
          <div className="kapasite-grafigi">
            <strong className="not">Parça stoğuyla üretilebilir adet</strong>
            {kapasiteler.map((k) => (
              <div key={k.urunId} className="kapasite-satiri">
                <span className="kapasite-ad">{k.urunAd}</span>
                <span className="kapasite-yol">
                  <span className={`kapasite-dolu${k.uretilebilir <= 1 ? " kapasite-dolu--az" : k.uretilebilir <= 3 ? " kapasite-dolu--orta" : ""}`}
                    style={{ width: `${Math.max(3, Math.min(100, (k.uretilebilir / enCok) * 100))}%` }} />
                </span>
                <b className="kapasite-sayi">{k.uretilebilir}</b>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
