"use client";

import { useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import { ApiHatasi, oturum } from "@/lib/api";

/** Yalnız uygulama içi yollara dönülür (dış adrese yönlendirme olmasın). */
function guvenliAdres(sonraki: string | null) {
  return sonraki && sonraki.startsWith("/") && !sonraki.startsWith("//") ? sonraki : "/";
}

export default function GirisFormu() {
  const sonraki = guvenliAdres(useSearchParams().get("sonraki"));
  const [kullaniciAdi, setKullaniciAdi] = useState("");
  const [sifre, setSifre] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);

  async function gonder(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      await oturum.giris(kullaniciAdi.trim(), sifre);
      // Tam sayfa geçiş: proxy yeni çerezi görsün
      window.location.href = sonraki;
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Giriş yapılamadı.");
      setBekliyor(false);
    }
  }

  return (
    <form className="kart kart--giris" onSubmit={gonder}>
      <span className="giris-logo">MKC BİLİŞİM</span>

      <div className="alan">
        <label htmlFor="kullaniciAdi">Kullanıcı adı</label>
        <input
          id="kullaniciAdi"
          className="girdi"
          autoComplete="username"
          autoFocus
          required
          value={kullaniciAdi}
          onChange={(e) => setKullaniciAdi(e.target.value)}
        />
      </div>
      <div className="alan">
        <label htmlFor="sifre">Şifre</label>
        <input
          id="sifre"
          type="password"
          className="girdi"
          autoComplete="current-password"
          required
          value={sifre}
          onChange={(e) => setSifre(e.target.value)}
        />
      </div>

      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}

      <button className="btn btn--tam-alt" type="submit" disabled={bekliyor}>
        {bekliyor ? "Giriş yapılıyor…" : "Giriş yap"}
      </button>
    </form>
  );
}
