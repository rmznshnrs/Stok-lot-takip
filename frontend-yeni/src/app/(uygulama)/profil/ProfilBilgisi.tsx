"use client";

import { useKullanici } from "@/components/Oturum";

/** Giriş yapmış kullanıcının bilgileri. Ad, soyad ve rolü yönetici değiştirir. */
export default function ProfilBilgisi() {
  const k = useKullanici();
  const yonetici = k.rol === "Admin";
  const rol = yonetici ? "Yönetici" : "Kullanıcı";
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

      <p className="not">Ad, soyad veya rol değişecekse yöneticinize başvurun.</p>
    </section>
  );
}
