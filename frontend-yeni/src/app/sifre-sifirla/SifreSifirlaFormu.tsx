"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState, type FormEvent } from "react";
import SifreKutusu from "@/components/SifreKutusu";
import { ApiHatasi, hesap } from "@/lib/api";

/** E-postadaki bağlantıyla açılır (?kod=…); yeni şifre belirlenir. */
export default function SifreSifirlaFormu() {
  const kod = useSearchParams().get("kod") ?? "";
  const [yeni, setYeni] = useState("");
  const [tekrar, setTekrar] = useState("");
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const [tamam, setTamam] = useState(false);

  async function gonder(e: FormEvent) {
    e.preventDefault();
    setHata("");
    if (yeni !== tekrar) return setHata("Şifreler aynı değil.");
    setBekliyor(true);
    try {
      await hesap.sifreSifirla(kod, yeni);
      setTamam(true);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Şifre değiştirilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  if (!kod) {
    return (
      <div className="kart kart--giris">
        <h2>Bağlantı eksik</h2>
        <p className="not not--buyuk">Bağlantı geçersiz.</p>
        <Link href="/sifremi-unuttum" className="btn btn--tam-alt">Şifremi unuttum</Link>
      </div>
    );
  }

  if (tamam) {
    return (
      <div className="kart kart--giris">
        <h2>Şifreniz değişti</h2>
        <p className="not not--buyuk">Yeni şifrenizle giriş yapın.</p>
        <Link href="/giris" className="btn btn--tam-alt">Giriş yap</Link>
      </div>
    );
  }

  return (
    <form className="kart kart--giris" onSubmit={gonder}>
      <div className="baslik-grup">
        <h2>Yeni şifre belirleyin</h2>
      </div>
      <div className="alan">
        <label htmlFor="yeni-sifre">Yeni şifre</label>
        <SifreKutusu id="yeni-sifre" required minLength={8} autoFocus autoComplete="new-password" placeholder="En az 8 karakter"
          value={yeni} onChange={(e) => setYeni(e.target.value)} />
      </div>
      <div className="alan">
        <label htmlFor="yeni-tekrar">Yeni şifre tekrar</label>
        <SifreKutusu id="yeni-tekrar" required minLength={8} autoComplete="new-password" value={tekrar} onChange={(e) => setTekrar(e.target.value)} />
      </div>
      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}
      <button className="btn btn--tam-alt" type="submit" disabled={bekliyor}>
        {bekliyor ? "Kaydediliyor…" : "Şifreyi değiştir"}
      </button>
      <p className="giris-alt"><Link href="/giris">Giriş sayfasına dön</Link></p>
    </form>
  );
}
