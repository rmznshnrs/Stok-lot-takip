"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useAdminMi } from "@/components/Oturum";
import { ApiHatasi, uretim, type HurdaTuru } from "@/lib/api";
import { tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

/** Ürünün depodaki seri numaraları; yönetici seçtiklerini hurdaya / kayba ayırabilir (açıklama zorunlu). */
export default function UrunDepo({ urunId, degisti }: { urunId: number; degisti?: () => void }) {
  const admin = useAdminMi();
  const { veri: liste, hata, yenile } = useVeri(() => uretim.gecmis(undefined, urunId), [urunId]);
  const depoda = liste?.filter((u) => !u.musteriAd && !u.hurda) ?? [];
  const ayrilan = liste?.filter((u) => u.hurda).length ?? 0;

  const [secili, setSecili] = useState<string[]>([]);
  const [tur, setTur] = useState<HurdaTuru>("Hurda");
  const [aciklama, setAciklama] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  function degistir(seriNo: string) {
    setSecili((s) => (s.includes(seriNo) ? s.filter((x) => x !== seriNo) : [...s, seriNo]));
  }

  async function ayir(e: FormEvent) {
    e.preventDefault();
    if (!secili.length) return;
    const ad = tur === "Hurda" ? "hurdaya" : "kayba";
    if (!window.confirm(`${secili.join(", ")} ${ad} ayrılacak; bu ürünler artık satılamaz. Onaylıyor musunuz?`)) return;
    setBekliyor(true);
    setMesaj(null);
    try {
      const s = await uretim.hurda({ seriNolar: secili, tur, aciklama: aciklama.trim() });
      setMesaj({ tur: "success", metin: `${s.seriNolar.join(", ")} ${ad} ayrıldı.` });
      setSecili([]);
      setAciklama("");
      yenile();
      degisti?.();
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <div className="form-dikey">
      <div className="bas-satir">
        <h3>Depodaki ürünler ({depoda.length})</h3>
        {ayrilan > 0 && (
          <Link href="/uretim?sekme=gecmis" className="not">{ayrilan} hurda / kayıp</Link>
        )}
      </div>
      <Mesaj mesaj={mesaj} />
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      {liste && depoda.length === 0 && <p className="not">Depoda bu üründen seri no yok.</p>}
      {depoda.length > 0 && (
        <div className="seri-secim">
          {depoda.map((u) =>
            admin ? (
              <label key={u.uretimId} className={`seri-secenek${secili.includes(u.seriNo) ? " seri-secenek--secili" : ""}`}>
                <input type="checkbox" checked={secili.includes(u.seriNo)} onChange={() => degistir(u.seriNo)} />
                <span className="mono">{u.seriNo}</span>
                <span className="not">{tarihSaat(u.tarih)}</span>
              </label>
            ) : (
              <Link key={u.uretimId} href={`/izlenebilirlik?seri=${encodeURIComponent(u.seriNo)}`} className="seri-secenek">
                <span className="mono">{u.seriNo}</span>
                <span className="not">{tarihSaat(u.tarih)}</span>
              </Link>
            ),
          )}
        </div>
      )}
      {admin && depoda.length > 0 && (
        <form className="hurda-form" onSubmit={ayir}>
          <select className="girdi" aria-label="Tür" value={tur} onChange={(e) => setTur(e.target.value as HurdaTuru)}>
            <option value="Hurda">Hurda</option>
            <option value="Kayip">Kayıp</option>
          </select>
          <input
            className="girdi girdi--esnek"
            aria-label="Açıklama"
            placeholder="Açıklama (zorunlu)"
            required
            maxLength={500}
            value={aciklama}
            onChange={(e) => setAciklama(e.target.value)}
          />
          <button type="submit" className="btn btn--ikincil" disabled={!secili.length || bekliyor}>
            {secili.length ? `${secili.length} ürünü ayır` : "Seri no seçin"}
          </button>
        </form>
      )}
    </div>
  );
}
