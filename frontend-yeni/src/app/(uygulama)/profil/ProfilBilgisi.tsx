"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useKullanici } from "@/components/Oturum";
import { ApiHatasi, hesap, rolAdi } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";

/** Giriş yapmış kullanıcının bilgileri. Ad, soyad ve rolü yönetici değiştirir. */
export default function ProfilBilgisi() {
  const k = useKullanici();
  const rol = rolAdi(k.rol);
  const satirlar = [
    { ad: "Ad", deger: k.ad },
    { ad: "Soyad", deger: k.soyad },
    { ad: "Kullanıcı adı", deger: k.kullaniciAdi, mono: true },
    { ad: "Rol", deger: rol },
  ];
  return (
    <section className="kart kart--bosluklu">
      <div className="profil-bas">
        <span className="profil-harf" aria-hidden="true">{(k.ad[0] ?? "") + (k.soyad[0] ?? "")}</span>
        <div className="baslik-grup">
          <h2>{k.ad} {k.soyad}</h2>
          <span className="not">{rol} · <span className="mono">{k.kullaniciAdi}</span></span>
        </div>
      </div>

      <dl className="profil-liste">
        {satirlar.map((s) => (
          <div key={s.ad}>
            <dt>{s.ad}</dt>
            <dd className={s.mono ? "mono" : undefined}>{s.deger}</dd>
          </div>
        ))}
      </dl>


      <EpostaFormu />
    </section>
  );
}

/** Şifre sıfırlama bağlantısının gideceği e-posta. */
function EpostaFormu() {
  const { veri } = useVeri(() => hesap.eposta(), []);
  return veri ? <EpostaAlani ilk={veri.eposta ?? ""} /> : null;
}

function EpostaAlani({ ilk }: { ilk: string }) {
  const [eposta, setEposta] = useState(ilk);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    setBekliyor(true);
    try {
      const s = await hesap.epostaAyarla(eposta.trim());
      setEposta(s.eposta ?? "");
      setMesaj({ tur: "success", metin: s.eposta ? "E-posta kaydedildi." : "E-posta kaldırıldı." });
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <form className="form-dikey" onSubmit={kaydet}>
      <div className="alan">
        <label htmlFor="profil-eposta">E-posta</label>
        <input id="profil-eposta" type="email" className="girdi" maxLength={200} autoComplete="email"
          value={eposta} onChange={(e) => setEposta(e.target.value)} />
      </div>
      <Mesaj mesaj={mesaj} />
      <div className="dugme-grup">
        <button type="submit" className="btn btn--ikincil" disabled={bekliyor}>E-postayı kaydet</button>
      </div>
    </form>
  );
}
