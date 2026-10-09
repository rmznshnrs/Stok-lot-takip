"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ApiHatasi, uretim, urunler, type UretimSonucu } from "@/lib/api";
import { useGecikmeli, useVeri } from "@/lib/kancalar";

const EN_COK = 999;

function sinirla(n: number) {
  return Math.min(EN_COK, Math.max(1, Math.floor(n) || 1));
}

export default function UretimEkrani() {
  const router = useRouter();
  const params = useSearchParams();
  const urunId = Number(params.get("urun")) || null;

  const [arama, setArama] = useState("");
  const aranan = useGecikmeli(arama.trim());
  const { veri: liste, hata: listeHatasi } = useVeri(() => urunler.listele(aranan), [aranan]);

  const [adetMetni, setAdetMetni] = useState("1");
  const adet = useGecikmeli(sinirla(Number(adetMetni)), 300);
  const { veri: onizleme, hata: onizlemeHatasi, yenile } = useVeri(
    () => (urunId ? uretim.onizle(urunId, adet) : Promise.resolve(null)),
    [urunId, adet],
  );

  const [sonuc, setSonuc] = useState<UretimSonucu | null>(null);
  const [kayitHatasi, setKayitHatasi] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  // Seçili ürün yoksa ilkini seç
  useEffect(() => {
    if (!urunId && !aranan && liste?.length) router.replace(`/uretim?urun=${liste[0].id}`);
  }, [urunId, aranan, liste, router]);

  function adetDegistir(fark: number) {
    setAdetMetni((m) => String(sinirla(Number(m) + fark)));
  }

  async function kaydet() {
    if (!onizleme) return;
    setBekliyor(true);
    setKayitHatasi("");
    try {
      setSonuc(await uretim.uret(onizleme.urunId, onizleme.adet));
      setAdetMetni("1");
    } catch (err) {
      setKayitHatasi(err instanceof ApiHatasi ? eksikMetni(err) : "Üretim kaydedilemedi.");
    } finally {
      setBekliyor(false);
      yenile();
    }
  }

  const secili = liste?.find((u) => u.id === urunId);
  const guncel = onizleme && onizleme.urunId === urunId && onizleme.adet === adet ? onizleme : null;

  return (
    <main className="icerik">
      <section className="kart kart--liste kart--liste-dar">
        <div className="adim-baslik bas-satir--liste">1 · Ürün seç</div>
        <input
          type="search"
          className="girdi girdi--liste-ara"
          aria-label="Ürün ara"
          placeholder="Ürün ara…"
          value={arama}
          onChange={(e) => setArama(e.target.value)}
        />
        {listeHatasi && <div className="mesaj mesaj--error">{listeHatasi}</div>}
        {liste?.map((u) => (
          <Link
            key={u.id}
            href={`/uretim?urun=${u.id}`}
            replace
            className={`liste-oge${u.id === urunId ? " liste-oge--secili" : ""}`}
            aria-current={u.id === urunId ? "page" : undefined}
            onClick={() => setSonuc(null)}
          >
            <span className="liste-oge-ad">{u.ad}</span>
            <span className="liste-oge-alt">{u.parcaCesidi} parça çeşidi</span>
          </Link>
        ))}
        {liste?.length === 0 && (
          <p className="not">{aranan ? `“${aranan}” için ürün bulunamadı.` : "Henüz ürün yok."}</p>
        )}
        <Link href="/urunler?yeni=1" className="liste-ekle">+ Yeni ürün tanımla</Link>
      </section>

      <section className="kart kart--genis kart--bosluklu">
        {sonuc && (
          <div className="sonuc-kutu" role="status">
            <div className="sonuc-baslik">Üretim kaydedildi: {sonuc.seriNolar.length} adet {sonuc.urunAd}</div>
            <div className="seri-listesi">
              {sonuc.seriNolar.map((s) => (
                <Link key={s} href={`/izlenebilirlik?seri=${encodeURIComponent(s)}`} className="seri-cip">{s}</Link>
              ))}
            </div>
          </div>
        )}

        {liste?.length === 0 && !aranan ? (
          <p className="not not--buyuk">
            Üretim için önce bir <Link href="/urunler?yeni=1">ürün tanımlayın</Link>.
          </p>
        ) : urunId ? (
          <>
            <div className="bas-satir bas-satir--alt">
              <div className="adim-grup">
                <div className="adim-baslik">2 · Kaç adet?</div>
                <div className="adet-secici">
                  <button type="button" className="btn-adet" aria-label="Azalt" onClick={() => adetDegistir(-1)}>−</button>
                  <input
                    type="number"
                    min={1}
                    max={EN_COK}
                    className="girdi girdi--adet-buyuk"
                    aria-label="Adet"
                    value={adetMetni}
                    onChange={(e) => setAdetMetni(e.target.value)}
                    onBlur={() => setAdetMetni(String(sinirla(Number(adetMetni))))}
                  />
                  <button type="button" className="btn-adet" aria-label="Arttır" onClick={() => adetDegistir(1)}>+</button>
                  <span className="adet-urun">{secili?.ad ?? guncel?.urunAd}</span>
                </div>
              </div>
              <Link href={`/urunler?urun=${urunId}`} className="not--buyuk">Ürün ağacını düzenle</Link>
            </div>

            <div className="adim-baslik">
              3 · Parça kontrolü <span className="adim-aciklama">(lotlar otomatik seçilir: önce gelen önce kullanılır)</span>
            </div>

            {onizlemeHatasi ? (
              <div className="mesaj mesaj--warning">
                {onizlemeHatasi} <Link href={`/urunler?urun=${urunId}`}>Ürün ağacına parça ekleyin.</Link>
              </div>
            ) : !guncel ? (
              <p className="yukleniyor">Kontrol ediliyor…</p>
            ) : (
              <>
                <div className="tablo-kap">
                  <table className="tablo">
                    <thead>
                      <tr>
                        <th>Parça</th>
                        <th className="sag">Gereken</th>
                        <th className="sag">Kullanılabilir</th>
                        <th>Kullanılacak lot(lar)</th>
                        <th>Durum</th>
                      </tr>
                    </thead>
                    <tbody>
                      {guncel.parcalar.map((p) => (
                        <tr key={p.parca.id} className={p.eksik ? "uyari-satir" : undefined}>
                          <td>{p.parca.ad} <span className="mono yardim">{p.parca.kod}</span></td>
                          <td className="sag">{p.gereken}</td>
                          <td className="sag">{p.kullanilabilir}</td>
                          <td className="mono">
                            {p.lotlar.length ? (
                              p.lotlar.map((d) => `${d.lotNo} × ${d.adet}`).join(" · ")
                            ) : (
                              <span className="soluk">kullanılabilir lot yok</span>
                            )}
                          </td>
                          {p.eksik ? (
                            <td className="uyari-metin">{p.eksik} adet eksik</td>
                          ) : (
                            <td className="durum-yeterli">Yeterli</td>
                          )}
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <div className="not">
                  {guncel.atlananLotlar.length ? (
                    <>
                      Geri çağrılan lotlar (
                      {guncel.atlananLotlar.map((l, i) => (
                        <span key={l}>{i > 0 && ", "}<span className="mono">{l}</span></span>
                      ))}
                      ) otomatik olarak atlanır.
                    </>
                  ) : (
                    "Geri çağrılan lotlar otomatik olarak atlanır."
                  )}
                </div>

                {kayitHatasi && <div className="mesaj mesaj--error" role="alert">{kayitHatasi}</div>}

                <div className="kart-alt">
                  <div className="baslik-grup">
                    <span className="adim-baslik">4 · Verilecek seri numaraları</span>
                    <span className="mono seri-aralik">
                      {guncel.seriNolar[0]}
                      {guncel.seriNolar.length > 1 && ` … ${guncel.seriNolar[guncel.seriNolar.length - 1]}`}
                    </span>
                  </div>
                  <div className="kaydet-grup">
                    {!guncel.yeterli && <span className="uyari-not">Eksik parça var</span>}
                    <button type="button" className="btn" disabled={!guncel.yeterli || bekliyor} onClick={kaydet}>
                      {bekliyor ? "Kaydediliyor…" : "Üretimi kaydet"}
                    </button>
                  </div>
                </div>
              </>
            )}
          </>
        ) : null}
      </section>
    </main>
  );
}

/** Kayıt anında stok yetmediyse (başkası aynı anda kullandıysa) eksikleri de yazar. */
function eksikMetni(h: ApiHatasi) {
  if (!h.eksikler.length) return h.message;
  return `${h.message} ${h.eksikler.map((e) => `${e.parca.kod}: ${e.eksik} eksik`).join(", ")}.`;
}
