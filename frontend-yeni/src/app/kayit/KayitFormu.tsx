"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import SifreKutusu from "@/components/SifreKutusu";
import { ApiHatasi, hesap } from "@/lib/api";

const BOS = { ad: "", soyad: "", kullaniciAdi: "", eposta: "", sifre: "", tekrar: "" };

/** Herkese açık kayıt. Hesap Çalışan olarak açılır; yönetici onaylayana kadar giriş yapılamaz. */
export default function KayitFormu() {
  const [form, setForm] = useState(BOS);
  const [hata, setHata] = useState("");
  const [bekliyor, setBekliyor] = useState(false);
  const [tamam, setTamam] = useState(false);

  async function gonder(e: FormEvent) {
    e.preventDefault();
    setHata("");
    if (form.sifre !== form.tekrar) return setHata("Şifreler aynı değil.");
    setBekliyor(true);
    try {
      await hesap.kayit({
        ad: form.ad.trim(), soyad: form.soyad.trim(), kullaniciAdi: form.kullaniciAdi.trim(),
        eposta: form.eposta.trim(), sifre: form.sifre,
      });
      setTamam(true);
    } catch (err) {
      setHata(err instanceof ApiHatasi ? err.message : "Kaydedilemedi.");
    } finally {
      setBekliyor(false);
    }
  }

  const alan = (ad: keyof typeof BOS) => ({
    id: `kayit-${ad}`,
    value: form[ad],
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [ad]: e.target.value }),
  });

  if (tamam) {
    return (
      <div className="kart kart--giris">
        <h2>Kaydınız alındı</h2>
        <p className="not not--buyuk">
          Yönetici onaylayınca giriş yapabilirsiniz.
        </p>
        <Link href="/giris" className="btn btn--tam-alt">Giriş sayfasına dön</Link>
      </div>
    );
  }

  return (
    <form className="kart kart--giris" onSubmit={gonder}>
      <div className="baslik-grup">
        <h2>MKC Bilişim</h2>
        <span className="not">Kayıt ol</span>
      </div>

      <div className="izgara-2">
        <div className="alan">
          <label htmlFor="kayit-ad">Ad</label>
          <input className="girdi" required maxLength={100} autoComplete="given-name" autoFocus {...alan("ad")} />
        </div>
        <div className="alan">
          <label htmlFor="kayit-soyad">Soyad</label>
          <input className="girdi" required maxLength={100} autoComplete="family-name" {...alan("soyad")} />
        </div>
      </div>
      <div className="alan">
        <label htmlFor="kayit-kullaniciAdi">Kullanıcı adı</label>
        <input className="girdi" required maxLength={50} autoComplete="username" {...alan("kullaniciAdi")} />
      </div>
      <div className="alan">
        <label htmlFor="kayit-eposta">E-posta</label>
        <input type="email" className="girdi" required maxLength={200} autoComplete="email" {...alan("eposta")} />
      </div>
      <div className="alan">
        <label htmlFor="kayit-sifre">Şifre</label>
        <SifreKutusu required minLength={8} autoComplete="new-password" placeholder="En az 8 karakter" {...alan("sifre")} />
      </div>
      <div className="alan">
        <label htmlFor="kayit-tekrar">Şifre tekrar</label>
        <SifreKutusu required minLength={8} autoComplete="new-password" {...alan("tekrar")} />
      </div>

      {hata && <div className="mesaj mesaj--error" role="alert">{hata}</div>}

      <button className="btn btn--tam-alt" type="submit" disabled={bekliyor}>
        {bekliyor ? "Kaydediliyor…" : "Kayıt ol"}
      </button>
      <p className="giris-alt">Zaten hesabınız var mı? <Link href="/giris">Giriş yap</Link></p>
    </form>
  );
}
