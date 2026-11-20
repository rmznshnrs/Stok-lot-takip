"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import { ApiHatasi, sira, uretim, urunler } from "@/lib/api";
import { bugun } from "@/lib/bicim";
import { useGecikmeli, useVeri } from "@/lib/kancalar";
import SayiGirdi from "@/components/SayiGirdi";

const EN_COK = 999;

function sinirla(n: number) {
  return Math.min(EN_COK, Math.max(1, Math.floor(n) || 1));
}

export default function SirayaEkle() {
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

  const [planlanan, setPlanlanan] = useState(bugun);
  const [aciklama, setAciklama] = useState("");
  const [kayitHatasi, setKayitHatasi] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  useEffect(() => {
    if (!urunId && !aranan && liste?.length) router.replace(`/uretim?yeni=1&urun=${liste[0].id}`);
  }, [urunId, aranan, liste, router]);

  async function kaydet() {
    if (!onizleme) return;
    setBekliyor(true);
    setKayitHatasi("");
    try {
      const e = await sira.ekle({ urunId: onizleme.urunId, adet: onizleme.adet, planlananTarih: planlanan, aciklama: aciklama.trim() });
      router.push(`/uretim?eklendi=${e.id}`);
    } catch (err) {
      setKayitHatasi(err instanceof ApiHatasi ? eksikMetni(err) : "Sıraya eklenemedi.");
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
          placeholder="Ürün ara"
          value={arama}
          onChange={(e) => setArama(e.target.value)}
        />
        {listeHatasi && <div className="mesaj mesaj--error">{listeHatasi}</div>}
        {liste?.map((u) => (
          <Link
            key={u.id}
            href={`/uretim?yeni=1&urun=${u.id}`}
            replace
            className={`liste-oge${u.id === urunId ? " liste-oge--secili" : ""}`}
            aria-current={u.id === urunId ? "page" : undefined}
          >
            <span className="liste-oge-ad">{u.ad}</span>
            <span className="liste-oge-alt">{u.parcaCesidi} parça çeşidi</span>
          </Link>
        ))}
        {liste?.length === 0 && (
          <p className="not">{aranan ? "Sonuç yok." : "Ürün yok."}</p>
        )}
        <Link href="/urunler?yeni=1" className="liste-ekle">+ Yeni ürün tanımla</Link>
      </section>

      <section className="kart kart--genis kart--bosluklu">
        {liste?.length === 0 && !aranan ? (
          <p className="not not--buyuk">
            Ürün yok. <Link href="/urunler?yeni=1">Ürün ekle</Link>
          </p>
        ) : urunId ? (
          <>
            <div className="bas-satir bas-satir--alt">
              <div className="adim-grup">
                <div className="adim-baslik">2 · Kaç adet?</div>
                <div className="adet-secici">
                  <SayiGirdi min={1} max={EN_COK} aria-label="Adet" value={adetMetni} onChange={setAdetMetni} />
                  <span className="adet-urun">{secili?.ad ?? guncel?.urunAd}</span>
                </div>
              </div>
            </div>

            <div className="izgara-2">
              <div className="alan">
                <label htmlFor="sira-tarih">Planlanan Gün</label>
                <input id="sira-tarih" type="date" className="girdi" value={planlanan} onChange={(e) => setPlanlanan(e.target.value)} />
              </div>
              <div className="alan">
                <label htmlFor="sira-aciklama">Açıklama</label>
                <input id="sira-aciklama" className="girdi" maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
              </div>
            </div>

            <div className="adim-baslik">
              3 · Parça kontrolü
            </div>

            {onizlemeHatasi ? (
              <div className="mesaj mesaj--warning">
                {onizlemeHatasi} <Link href={`/urunler?urun=${urunId}&duzenle=1`}>Ürün ağacını düzenle</Link>
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
                        <th className="sag">Stokta</th>
                        <th>Kullanılacak Lotlar</th>
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
                              p.lotlar.map((d, i) => (
                                <span key={d.lotId}>
                                  {i > 0 && " · "}
                                  <span className={d.sorunlu ? "uyari-metin" : undefined}>{d.lotNo} · {d.adet} adet</span>
                                  {d.sorunlu && <span className="lot-etiket lot-etiket--uyari"> hatalı</span>}
                                </span>
                              ))
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
                {guncel.atlananLotlar.length > 0 && (
                  <div className="not">
                    Atlanan hatalı lotlar:{" "}
                    {guncel.atlananLotlar.map((l, i) => (
                      <span key={l}>{i > 0 && ", "}<span className="mono">{l}</span></span>
                    ))}
                  </div>
                )}
                {guncel.sorunluKullanilanLotlar.length > 0 && (
                  <div className="mesaj mesaj--warning">
                    Hatalı lot kullanılacak:{" "}
                    <span className="mono">{guncel.sorunluKullanilanLotlar.join(", ")}</span>
                  </div>
                )}

                {kayitHatasi && <div className="mesaj mesaj--error" role="alert">{kayitHatasi}</div>}

                <div className="kart-alt">
                  <span />
                  <div className="kaydet-grup">
                    {!guncel.yeterli && <span className="uyari-not">Eksik parça var</span>}
                    <button type="button" className="btn" disabled={!guncel.yeterli || bekliyor || !planlanan} onClick={kaydet}>
                      {bekliyor ? "Ekleniyor…" : "Sıraya ekle"}
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

function eksikMetni(h: ApiHatasi) {
  if (!h.eksikler.length) return h.message;
  return `${h.message} ${h.eksikler.map((e) => `${e.parca.kod}: ${e.eksik} eksik`).join(", ")}.`;
}
