"use client";

import Link from "next/link";
import { izleme, stok, type Lot } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

const lotAdresi = (n: string) => `/izlenebilirlik?lot=${encodeURIComponent(n)}`;
const seriAdresi = (n: string) => `/izlenebilirlik?sekme=seri&seri=${encodeURIComponent(n)}`;

/** Numara aranmadan önce: lot sekmesinde geri çağrılan ve son gelen lotlar, seri sekmesinde son üretimler. */
export default function IzOnerileri({ sekme }: { sekme: "lot" | "seri" }) {
  return sekme === "lot" ? <LotOnerileri /> : <UretimOnerileri />;
}

function LotOnerileri() {
  const { veri, hata } = useVeri(async () => {
    const lotlar = (await stok.durum({ bitenler: true })).flatMap((g) => g.lotlar);
    const sonGelen = [...lotlar].sort((a, b) => b.girisZamani.localeCompare(a.girisZamani)).slice(0, 8);
    return { geri: lotlar.filter((l) => l.geriCagrildi), sonGelen };
  }, []);

  if (hata) return <div className="mesaj mesaj--error">{hata}</div>;
  if (!veri) return null;
  return (
    <div className="iz-oneriler">
      {veri.geri.length > 0 && <LotListesi baslik="Geri çağrılan lotlar" lotlar={veri.geri} uyari />}
      <LotListesi baslik="Son gelen lotlar" lotlar={veri.sonGelen} />
    </div>
  );
}

function LotListesi({ baslik, lotlar, uyari = false }: { baslik: string; lotlar: Lot[]; uyari?: boolean }) {
  return (
    <section className={`kart${uyari ? " kart--geri" : ""}`}>
      <div className="adim-baslik">{baslik}</div>
      {lotlar.length === 0 && <p className="not">Henüz lot yok.</p>}
      {lotlar.map((l) => (
        <Link key={l.id} href={lotAdresi(l.lotNo)} className="oneri-oge">
          <span>
            <span className={`mono${l.geriCagrildi ? " uyari-metin" : ""}`}>{l.lotNo}</span>
            <span className="not"> · {l.parcaAd}</span>
          </span>
          <span className="not">{tarih(l.siparisTarihi)}</span>
        </Link>
      ))}
    </section>
  );
}

function UretimOnerileri() {
  const { veri, hata } = useVeri(async () => (await izleme.hareketler(30)).filter((h) => h.tur === "uretim").slice(0, 8), []);

  if (hata) return <div className="mesaj mesaj--error">{hata}</div>;
  if (!veri) return null;
  return (
    <div className="iz-oneriler">
      <section className="kart">
        <div className="adim-baslik">Son üretimler</div>
        {veri.length === 0 && <p className="not">Henüz üretim yok.</p>}
        {veri.map((h, i) => (
          <Link key={i} href={seriAdresi(h.numaraIlk)} className="oneri-oge">
            <span>
              <span className="mono">{h.numara}</span>
              <span className="not"> · {h.aciklama}</span>
            </span>
            <span className="not">{tarih(h.tarih)}</span>
          </Link>
        ))}
      </section>
    </div>
  );
}
