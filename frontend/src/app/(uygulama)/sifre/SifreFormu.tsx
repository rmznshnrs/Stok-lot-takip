"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, oturum } from "@/lib/api";

export default function SifreFormu() {
  const [eski, setEski] = useState("");
  const [yeni, setYeni] = useState("");
  const [tekrar, setTekrar] = useState("");
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    if (yeni !== tekrar) return setMesaj({ tur: "error", metin: "Yeni şifreler aynı değil." });
    setBekliyor(true);
    try {
      await oturum.sifreDegistir(eski, yeni);
      setMesaj({ tur: "success", metin: "Şifreniz değiştirildi." });
      setEski("");
      setYeni("");
      setTekrar("");
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Şifre değiştirilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <section className="kart kart--form">
      <h2>Şifre değiştir</h2>
      <form className="form-dikey" onSubmit={kaydet}>
        <Mesaj mesaj={mesaj} />
        <div className="alan">
          <label htmlFor="eski">Mevcut şifre</label>
          <input id="eski" type="password" className="girdi" required autoComplete="current-password" value={eski} onChange={(e) => setEski(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor="yeni">Yeni şifre</label>
          <input id="yeni" type="password" className="girdi" required minLength={8} autoComplete="new-password" value={yeni} onChange={(e) => setYeni(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor="tekrar">Yeni şifre (tekrar)</label>
          <input id="tekrar" type="password" className="girdi" required minLength={8} autoComplete="new-password" value={tekrar} onChange={(e) => setTekrar(e.target.value)} />
        </div>
        <button type="submit" className="btn btn--tam" disabled={bekliyor}>Şifreyi değiştir</button>
      </form>
    </section>
  );
}
