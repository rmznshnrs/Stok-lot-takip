"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useKullanici } from "@/components/Oturum";
import { ApiHatasi, kullanicilar, type Kullanici, type Rol } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";

const BOS = { ad: "", soyad: "", kullaniciAdi: "", sifre: "", rol: "Kullanici" as Rol };

export default function Kullanicilar() {
  const ben = useKullanici();
  const { veri: liste, hata, yenile } = useVeri(() => kullanicilar.listele(), []);
  const [form, setForm] = useState(BOS);
  const [duzenlenen, setDuzenlenen] = useState<Kullanici | null>(null);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  function duzenle(k: Kullanici) {
    setDuzenlenen(k);
    setForm({ ad: k.ad, soyad: k.soyad, kullaniciAdi: k.kullaniciAdi, sifre: "", rol: k.rol });
    setMesaj(null);
  }

  function vazgec() {
    setDuzenlenen(null);
    setForm(BOS);
    setMesaj(null);
  }

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setBekliyor(true);
    setMesaj(null);
    try {
      const k = duzenlenen
        ? await kullanicilar.guncelle(duzenlenen.id, {
            ad: form.ad.trim(), soyad: form.soyad.trim(), rol: form.rol, yeniSifre: form.sifre || undefined,
          })
        : await kullanicilar.olustur({ ...form, ad: form.ad.trim(), soyad: form.soyad.trim(), kullaniciAdi: form.kullaniciAdi.trim() });
      setMesaj({ tur: "success", metin: `${k.kullaniciAdi} ${duzenlenen ? "güncellendi" : "oluşturuldu"}.` });
      setDuzenlenen(null);
      setForm(BOS);
      yenile();
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  const alan = (ad: "ad" | "soyad" | "kullaniciAdi" | "sifre") => ({
    id: `k-${ad}`,
    value: form[ad],
    onChange: (e: React.ChangeEvent<HTMLInputElement>) => setForm({ ...form, [ad]: e.target.value }),
  });

  return (
    <section className="kart kart--genis kart--bosluklu">
      <h2>Kullanıcılar</h2>
      {hata && <div className="mesaj mesaj--error">{hata}</div>}
      <div className="tablo-kap">
        <table className="tablo">
          <thead>
            <tr>
              <th>Ad soyad</th>
              <th>Kullanıcı adı</th>
              <th>Rol</th>
              <th className="sutun-ikon"><span className="gorunmez">İşlem</span></th>
            </tr>
          </thead>
          <tbody>
            {liste === null && !hata && <tr className="bos"><td colSpan={4}>Yükleniyor…</td></tr>}
            {liste?.map((k) => (
              <tr key={k.id}>
                <td>{k.ad} {k.soyad}{k.id === ben.id && <span className="soluk"> (siz)</span>}</td>
                <td className="mono">{k.kullaniciAdi}</td>
                <td>{k.rol === "Admin" ? <span className="rozet rozet--mavi">Yönetici</span> : "Kullanıcı"}</td>
                <td>
                  <button type="button" className="btn-ikon" title="Düzenle" aria-label={`${k.kullaniciAdi} düzenle`} onClick={() => duzenle(k)}>
                    ✎
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <form className="form-dikey kart-alt kart-alt--form" onSubmit={kaydet}>
        <h3>{duzenlenen ? `${duzenlenen.kullaniciAdi} düzenleniyor` : "Yeni kullanıcı"}</h3>
        <Mesaj mesaj={mesaj} />
        <div className="izgara-2">
          <div className="alan">
            <label htmlFor="k-ad">Ad</label>
            <input className="girdi" required maxLength={100} {...alan("ad")} />
          </div>
          <div className="alan">
            <label htmlFor="k-soyad">Soyad</label>
            <input className="girdi" required maxLength={100} {...alan("soyad")} />
          </div>
          <div className="alan">
            <label htmlFor="k-kullaniciAdi">Kullanıcı adı</label>
            <input className="girdi girdi--mono" required={!duzenlenen} disabled={!!duzenlenen} maxLength={50} autoComplete="off" {...alan("kullaniciAdi")} />
          </div>
          <div className="alan">
            <label htmlFor="k-rol">Rol</label>
            <select id="k-rol" className="girdi" value={form.rol} onChange={(e) => setForm({ ...form, rol: e.target.value as Rol })}>
              <option value="Kullanici">Kullanıcı</option>
              <option value="Admin">Yönetici</option>
            </select>
          </div>
          <div className="alan">
            <label htmlFor="k-sifre">{duzenlenen ? "Yeni şifre (boşsa değişmez)" : "Şifre"}</label>
            <input type="password" className="girdi" required={!duzenlenen} minLength={8} autoComplete="new-password" {...alan("sifre")} />
          </div>
        </div>
        <span className="yardim">Şifre en az 8 karakter. Yönetici kullanıcıları yönetir, lot geri çağırır ve stok düzeltir.</span>
        <div className="kaydet-grup">
          {duzenlenen && <button type="button" className="btn btn--ikincil" onClick={vazgec}>Vazgeç</button>}
          <button type="submit" className="btn" disabled={bekliyor}>{duzenlenen ? "Kaydet" : "Kullanıcı ekle"}</button>
        </div>
      </form>
    </section>
  );
}
