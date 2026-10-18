"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useAdminMi } from "@/components/Oturum";
import { ApiHatasi, uretim, type HurdaTuru } from "@/lib/api";
import { icerir, tarihSaat } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import { seriAdresi } from "@/lib/adres";

/** Ürünün depodaki seri numaraları; yönetici seçtiklerini hurdaya / kayba ayırabilir (açıklama zorunlu). */
export default function UrunDepo({ urunId, degisti }: { urunId: number; degisti?: () => void }) {
  const admin = useAdminMi();
  const { veri: liste, hata, yenile } = useVeri(() => uretim.gecmis(undefined, urunId), [urunId]);
  const depoda = liste?.filter((u) => !u.musteriAd && !u.hurda) ?? [];
  const ayrilan = liste?.filter((u) => u.hurda).length ?? 0;

  const [ara, setAra] = useState("");
  const gorunen = depoda.filter((u) => icerir(u.seriNo, ara.trim()));
  const [secili, setSecili] = useState<string[]>([]);
  const hepsiSecili = gorunen.length > 0 && gorunen.every((u) => secili.includes(u.seriNo));
  const [tur, setTur] = useState<HurdaTuru>("Hurda");
  const [aciklama, setAciklama] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  function degistir(seriNo: string) {
    setSecili((s) => (s.includes(seriNo) ? s.filter((x) => x !== seriNo) : [...s, seriNo]));
  }

  function hepsiniDegistir() {
    const numaralar = gorunen.map((u) => u.seriNo);
    setSecili((s) => (hepsiSecili ? s.filter((x) => !numaralar.includes(x)) : [...new Set([...s, ...numaralar])]));
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
        <h3>Depodaki ürünler <span className="rozet rozet--kucuk rozet--mavi">{depoda.length}</span></h3>
        {ayrilan > 0 && (
          <Link href="/uretim?sekme=gecmis" className="not">{ayrilan} hurda / kayıp</Link>
        )}
      </div>
      <Mesaj mesaj={mesaj} />
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      {liste && depoda.length === 0 && <p className="not">Depoda bu üründen seri no yok.</p>}
      {depoda.length > 0 && (
        <>
          <input
            type="search"
            className="girdi girdi--mono"
            aria-label="Depoda seri no ara"
            value={ara}
            onChange={(e) => setAra(e.target.value)}
          />
          <div className="depo-liste">
            {admin && gorunen.length > 1 && (
              <label className="depo-oge depo-oge--hepsi">
                <input type="checkbox" checked={hepsiSecili} onChange={hepsiniDegistir} />
                <span>Görünenlerin hepsi · {gorunen.length}</span>
              </label>
            )}
            {gorunen.map((u) =>
              admin ? (
                <label key={u.uretimId} className={`depo-oge${secili.includes(u.seriNo) ? " depo-oge--secili" : ""}`}>
                  <input type="checkbox" checked={secili.includes(u.seriNo)} onChange={() => degistir(u.seriNo)} />
                  <span className="mono">{u.seriNo}</span>
                  <span className="not">{tarihSaat(u.tarih)}</span>
                </label>
              ) : (
                <Link key={u.uretimId} href={seriAdresi(u.seriNo)} className="depo-oge">
                  <span className="mono">{u.seriNo}</span>
                  <span className="not">{tarihSaat(u.tarih)}</span>
                </Link>
              ),
            )}
            {gorunen.length === 0 && <p className="not">Aramaya uyan seri no yok.</p>}
          </div>
        </>
      )}
      {admin && depoda.length > 0 && (
        <form className="hurda-form" onSubmit={ayir}>
          <div className="alan hurda-tur">
            <label htmlFor="hurda-tur">Tür</label>
            <select id="hurda-tur" className="girdi" value={tur} onChange={(e) => setTur(e.target.value as HurdaTuru)}>
              <option value="Hurda">Hurda</option>
              <option value="Kayip">Kayıp</option>
            </select>
          </div>
          <div className="alan girdi--esnek">
            <label htmlFor="hurda-aciklama">Açıklama</label>
            <input
              id="hurda-aciklama"
              className="girdi"
              required
              maxLength={500}
              value={aciklama}
              onChange={(e) => setAciklama(e.target.value)}
            />
          </div>
          <button type="submit" className="btn btn--ikincil" disabled={!secili.length || bekliyor}>
            {secili.length ? `${secili.length} ürünü ayır` : "Seri no seçin"}
          </button>
        </form>
      )}
    </div>
  );
}
