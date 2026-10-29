"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { ApiHatasi, hesap } from "@/lib/api";

/** Kullanıcı adı ya da e-posta girilir; hesap varsa e-postasına sıfırlama bağlantısı gider. */
export default function SifremiUnuttumSayfasi() {
  const [deger, setDeger] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const [gonderildi, setGonderildi] = useState(false);

  async function gonder(e: FormEvent) {
    e.preventDefault();
    setHata("");
    setBekliyor(true);
    try {
      await hesap.sifreUnuttum(deger.trim());
      setGonderildi(true);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Gönderilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <main className="giris-sayfa">
      {gonderildi ? (
        <div className="kart kart--giris">
          <h2>E-posta gönderildi</h2>
          <p className="not not--buyuk">
            Sıfırlama bağlantısı e-postanıza gönderildi.
          </p>
          <Link href="/giris" className="btn btn--tam-alt">Giriş sayfasına dön</Link>
        </div>
      ) : (
        <form className="kart kart--giris" onSubmit={gonder}>
          <div className="baslik-grup">
            <h2>Şifremi unuttum</h2>
          </div>
          <div className="alan">
            <label htmlFor="unuttum">Kullanıcı adı / e-posta</label>
            <input id="unuttum" className="girdi" required autoFocus autoComplete="username" value={deger} onChange={(e) => setDeger(e.target.value)} />
          </div>
          {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
          <button className="btn btn--tam-alt" type="submit" disabled={bekliyor}>
            {bekliyor ? "Gönderiliyor…" : "Bağlantı gönder"}
          </button>
          <p className="giris-alt"><Link href="/giris">Giriş sayfasına dön</Link></p>
        </form>
      )}
    </main>
  );
}
