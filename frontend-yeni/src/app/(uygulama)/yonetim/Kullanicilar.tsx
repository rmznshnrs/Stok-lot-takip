"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useKullanici } from "@/components/Oturum";
import { ApiHatasi, kullanicilar, type Kullanici, type Rol } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";
import SifreKutusu from "@/components/SifreKutusu";

const BOS = { ad: "", soyad: "", kullaniciAdi: "", sifre: "", rol: "Kullanici" as Rol };

export default function Kullanicilar() {
  const ben = useKullanici();
  const { veri: liste, hata, yenile } = useVeri(() => kullanicilar.listele(), []);
  const [form, setForm] = useState(BOS);
  const [duzenlenen, setDuzenlenen] = useState<Kullanici | null>(null);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);
  const [formAcik, setFormAcik] = useState(false);

  function duzenle(k: Kullanici) {
    setDuzenlenen(k);
    setForm({ ad: k.ad, soyad: k.soyad, kullaniciAdi: k.kullaniciAdi, sifre: "", rol: k.rol });
    setMesaj(null);
    setFormAcik(true);
  }

  function vazgec() {
    setDuzenlenen(null);
    setForm(BOS);
    setMesaj(null);
    setFormAcik(false);
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
      setFormAcik(false);
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
    <>
      <div className="sayfa-bas">
        <span />
        <button
          type="button"
          className="btn"
          aria-expanded={formAcik && !duzenlenen}
          onClick={() => {
            if (formAcik && !duzenlenen) vazgec();
            else {
              setDuzenlenen(null);
              setForm(BOS);
              setMesaj(null);
              setFormAcik(true);
            }
          }}
        >
          {formAcik && !duzenlenen ? "Formu kapat" : "+ Yeni kullanıcı"}
        </button>
      </div>

      {!formAcik && <Mesaj mesaj={mesaj} />}

      {formAcik && (
        <section className="acilir-form">
          <h3>{duzenlenen ? `${duzenlenen.kullaniciAdi} düzenleniyor` : "Yeni kullanıcı"}</h3>
          <form className="form-dikey" onSubmit={kaydet}>
            <Mesaj mesaj={mesaj} />
            <div className="izgara-form">
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
                <label htmlFor="k-sifre">{duzenlenen ? "Yeni şifre" : "Şifre"}</label>
                <SifreKutusu required={!duzenlenen} minLength={8} autoComplete="new-password" {...alan("sifre")} />
                {duzenlenen && <span className="yardim">Boş bırakılırsa şifre değişmez.</span>}
              </div>
            </div>
            <div className="dugme-grup">
              <button type="submit" className="btn" disabled={bekliyor}>{duzenlenen ? "Kaydet" : "Kullanıcı ekle"}</button>
              <button type="button" className="btn btn--ikincil" onClick={vazgec}>Vazgeç</button>
            </div>
          </form>
        </section>
      )}

      <section className="panel">
        {hata && <div className="panel-ic"><div className="mesaj mesaj--error">{hata}</div></div>}
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
                  <td>{k.ad} {k.soyad}{k.id === ben.id && <span className="soluk"> · siz</span>}</td>
                  <td className="mono">{k.kullaniciAdi}</td>
                  <td>{k.rol === "Admin" ? "Yönetici" : "Kullanıcı"}</td>
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
      </section>
    </>
  );
}
