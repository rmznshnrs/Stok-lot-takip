"use client";

/** Geri çağrılacak seri numaralarını seçme listesi; hiçbiri önceden seçili gelmez. */
export default function SeriSecimi({ adaylar, secili, setSecili, bos }: {
  adaylar: { seriNo: string; alt: string }[] | null;
  secili: string[];
  setSecili: (s: string[]) => void;
  bos: string;
}) {
  if (adaylar === null) return <p className="yukleniyor">Yükleniyor…</p>;
  if (!adaylar.length) return <p className="not">{bos}</p>;
  const hepsi = adaylar.every((a) => secili.includes(a.seriNo));
  const degistir = (seriNo: string) =>
    setSecili(secili.includes(seriNo) ? secili.filter((x) => x !== seriNo) : [...secili, seriNo]);
  return (
    <div className="depo-liste">
      {adaylar.length > 1 && (
        <label className="depo-oge depo-oge--hepsi">
          <input type="checkbox" checked={hepsi} onChange={() => setSecili(hepsi ? [] : adaylar.map((a) => a.seriNo))} />
          <span>Hepsi · {adaylar.length}</span>
        </label>
      )}
      {adaylar.map((a) => (
        <label key={a.seriNo} className={`depo-oge${secili.includes(a.seriNo) ? " depo-oge--secili" : ""}`}>
          <input type="checkbox" checked={secili.includes(a.seriNo)} onChange={() => degistir(a.seriNo)} />
          <span className="mono">{a.seriNo}</span>
          <span className="not">{a.alt}</span>
        </label>
      ))}
    </div>
  );
}
