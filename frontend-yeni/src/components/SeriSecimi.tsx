"use client";

/** Seçim adayı. musteride verilirse liste müşteridekiler / depodakiler diye ikiye ayrılır. */
export interface SeriAdayi { seriNo: string; alt: string; musteride?: boolean }

/** Seçilenlerin özeti: müşteridekiler geri çağrılır, depodakiler satıştan çekilir. */
export function secimOzeti(adaylar: SeriAdayi[] | null, secili: string[]) {
  const musteride = adaylar?.filter((a) => a.musteride && secili.includes(a.seriNo)).length ?? 0;
  const depoda = secili.length - musteride;
  return [
    musteride ? `${musteride} ürün müşteriden geri istenecek` : "",
    depoda ? `${depoda} ürün satıştan çekilecek` : "",
  ].filter(Boolean).join(", ");
}

/** Seri numarası seçme listesi (geri çağırmada müşteridekiler ve depodakiler ayrı); hiçbiri önceden seçili gelmez. */
export default function SeriSecimi({ adaylar, secili, setSecili, bos }: {
  adaylar: SeriAdayi[] | null;
  secili: string[];
  setSecili: (s: string[]) => void;
  bos: string;
}) {
  if (adaylar === null) return <p className="yukleniyor">Yükleniyor…</p>;
  if (!adaylar.length) return <p className="not">{bos}</p>;
  const gruplar = adaylar.some((a) => a.musteride !== undefined)
    ? [
        { ad: "Satılmış · müşteriden geri istenecek", liste: adaylar.filter((a) => a.musteride) },
        { ad: "Depoda · satıştan çekilecek", liste: adaylar.filter((a) => !a.musteride) },
      ].filter((g) => g.liste.length)
    : [{ ad: "", liste: adaylar }];
  const degistir = (seriNo: string) =>
    setSecili(secili.includes(seriNo) ? secili.filter((x) => x !== seriNo) : [...secili, seriNo]);
  return (
    <>
      {gruplar.map((g) => {
        const numaralar = g.liste.map((a) => a.seriNo);
        const hepsi = numaralar.every((n) => secili.includes(n));
        return (
          <div key={g.ad} className="form-dikey">
            {g.ad && <span className="not">{g.ad}</span>}
            <div className="depo-liste">
              {g.liste.length > 1 && (
                <label className="depo-oge depo-oge--hepsi">
                  <input type="checkbox" checked={hepsi}
                    onChange={() => setSecili(hepsi ? secili.filter((x) => !numaralar.includes(x)) : [...new Set([...secili, ...numaralar])])} />
                  <span>Hepsi · {g.liste.length}</span>
                </label>
              )}
              {g.liste.map((a) => (
                <label key={a.seriNo} className={`depo-oge${secili.includes(a.seriNo) ? " depo-oge--secili" : ""}`}>
                  <input type="checkbox" checked={secili.includes(a.seriNo)} onChange={() => degistir(a.seriNo)} />
                  <span className="mono">{a.seriNo}</span>
                  <span className="not">{a.alt}</span>
                </label>
              ))}
            </div>
          </div>
        );
      })}
    </>
  );
}
