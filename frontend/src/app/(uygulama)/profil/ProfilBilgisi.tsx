"use client";

import { useKullanici } from "@/components/Oturum";

/** Giriş yapmış kullanıcının bilgileri. Ad, soyad ve rolü yönetici Yönetim'den değiştirir. */
export default function ProfilBilgisi() {
  const k = useKullanici();
  const satirlar = [
    { ad: "Ad", deger: k.ad },
    { ad: "Soyad", deger: k.soyad },
    { ad: "Kullanıcı adı", deger: k.kullaniciAdi, mono: true },
    { ad: "Rol", deger: k.rol === "Admin" ? "Yönetici" : "Kullanıcı" },
  ];
  return (
    <section className="kart kart--form">
      <div className="profil-bas">
        <span className="profil-harf" aria-hidden="true">{(k.ad[0] ?? "") + (k.soyad[0] ?? "")}</span>
        <div className="baslik-grup">
          <h2>{k.ad} {k.soyad}</h2>
          <span className="not">{k.rol === "Admin" ? "Yönetici" : "Kullanıcı"}</span>
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
