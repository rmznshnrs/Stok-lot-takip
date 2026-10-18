/** Firma adı: büyük "MKC", yanında küçük ve aralıklı "BİLİŞİM". */
export default function Logo({ buyuk = false }: { buyuk?: boolean }) {
  return (
    <span className={`marka${buyuk ? " marka--buyuk" : ""}`}>
      <span className="marka-mkc">MKC</span>
      <span className="marka-alt">BİLİŞİM</span>
    </span>
  );
}
