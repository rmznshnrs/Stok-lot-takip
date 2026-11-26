"use client";

import Link from "next/link";
import { GecikenIsler, HaftalikUretim, SiparisDurumuGrafigi } from "@/components/Grafikler";
import { OnumuzdekiGunler, SiradakiIsler, UretimUyarilari, YaklasanTeslimler } from "./AnaSayfaPanolari";
import { ozet as ozetApi } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";

export default function AnaSayfaEkrani() {
  const { veri: o, hata: ozetHatasi } = useVeri(() => ozetApi.anaSayfa(), []);
  const { veri: grafik } = useVeri(() => ozetApi.grafikler(), []);
  const ozet = [
    { ad: "Üretim sırasındaki ürün", deger: o?.siradakiAdet, alt: o && `${o.siradakiIs} iş · bugüne kadar planlanan ${o.bugunPlanlanan}`, yol: "/uretim", dikkat: false },
    { ad: "Gönderilmeyi bekleyen sevkiyat", deger: o && o.hazirlananSevkiyat + o.kargodaSevkiyat, alt: o && `${o.hazirlananSevkiyat} kargoya verilecek · ${o.kargodaSevkiyat} kargoda`, yol: "/sevkiyat", dikkat: false },
    { ad: "Depodaki ürün", deger: o?.depodaUrun, yol: "/stok?gorunum=urun", dikkat: false },
    { ad: "Min stokta ya da altında parça", deger: o?.minAltindaParca, yol: "/stok?gorunum=parca", dikkat: true },
  ];

  return (
    <main className="icerik icerik--dikey">
      {ozetHatasi && <div className="mesaj mesaj--error">{ozetHatasi}</div>}
      <section className="ozet-kutular">
        {ozet.map((o) => {
          const sinif = `ozet-kutu${o.dikkat && o.deger ? " ozet-kutu--dikkat" : ""}`;
          const icerik = (
            <>
              <span>{o.ad}</span>
              <b>{o.deger ?? "…"}</b>
              {o.alt && <small className="yardim">{o.alt}</small>}
            </>
          );
          return <Link key={o.ad} href={o.yol} className={sinif}>{icerik}</Link>;
        })}
      </section>

      <GecikenIsler veri={grafik} />
      <div className="iki-sutun iki-sutun--grafik">
        <HaftalikUretim veri={grafik} />
        <SiparisDurumuGrafigi veri={grafik} />
      </div>

      {o && (o.minUyarilari ?? []).length > 0 && <UretimUyarilari ozet={o} />}
      <div className="iki-sutun">
        <SiradakiIsler />
        {o && <YaklasanTeslimler ozet={o} />}
      </div>
      <OnumuzdekiGunler />
    </main>
  );
}
