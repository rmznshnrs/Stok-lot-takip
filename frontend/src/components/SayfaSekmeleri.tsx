import Link from "next/link";

/** Sayfanın üstündeki sekmeler (ör. Parçalar / Tedarikçiler). */
export default function SayfaSekmeleri({ etiket, sekmeler }: {
  etiket: string;
  sekmeler: { adres: string; ad: string; aktif: boolean }[];
}) {
  return (
    <div className="ust-sekme-satiri">
      <nav className="sekmeler sekmeler--buyuk" aria-label={etiket}>
        {sekmeler.map((s) => (
          <Link key={s.adres} href={s.adres} className={`sekme${s.aktif ? " sekme--aktif" : ""}`} aria-current={s.aktif ? "page" : undefined}>
            {s.ad}
          </Link>
        ))}
      </nav>
    </div>
  );
}
