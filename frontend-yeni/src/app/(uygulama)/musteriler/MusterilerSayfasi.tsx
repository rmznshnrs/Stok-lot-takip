"use client";

import { useSearchParams } from "next/navigation";
import FirmaListesi from "@/components/FirmaListesi";
import SayfaSekmeleri from "@/components/SayfaSekmeleri";
import SeriSayfasi from "@/components/SeriSayfasi";
import AlinanUrunler from "./AlinanUrunler";
import GeriCagirmalar from "./GeriCagirmalar";
import MusteriSiparisleri from "./MusteriSiparisleri";
import SiparisDetay from "./SiparisDetay";
import SiparisFormu from "./SiparisFormu";

/**
 * Müşteriler: bilgiler, siparişler ve aldığı ürünler; Geri çağırmalar sekmesi (?sekme=geri).
 * Seri no sayfası (?seri=...), sipariş (?siparis=id) ve yeni sipariş (?siparis=yeni&musteri=id) da burada açılır.
 */
export default function MusterilerSayfasi() {
  const params = useSearchParams();
  const geri = params.get("sekme") === "geri";
  const seri = params.get("seri")?.trim();
  const siparis = params.get("siparis");
  const siparisMusteri = Number(params.get("musteri")) || 0;
  return (
    <>
      <SayfaSekmeleri
        etiket="Müşteriler"
        sekmeler={[
          { adres: "/musteriler", ad: "Müşteriler", aktif: !geri && !seri && !siparis },
          { adres: "/musteriler?sekme=geri", ad: "Geri Çağırmalar", aktif: geri },
        ]}
      />
      {siparis === "yeni" && siparisMusteri ? (
        <SiparisFormu musteriId={siparisMusteri} />
      ) : siparis && Number(siparis) ? (
        <SiparisDetay key={siparis} id={Number(siparis)} />
      ) : seri ? (
        <SeriSayfasi key={seri} seriNo={seri} />
      ) : geri ? (
        <GeriCagirmalar />
      ) : (
        <FirmaListesi tur="musteri" kok="/musteriler" ek={(m) => (
          <>
            <MusteriSiparisleri key={`s-${m.id}`} musteriId={m.id} />
            <AlinanUrunler key={m.id} musteriId={m.id} />
          </>
        )} />
      )}
    </>
  );
}
