"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { useKullanici } from "@/components/Oturum";
import { ApiHatasi, hesap, kullanicilar, rolAdi, type Kullanici, type Rol } from "@/lib/api";
import { useVeri } from "@/lib/kancalar";
import SifreKutusu from "@/components/SifreKutusu";

const BOS = { ad: "", soyad: "", kullaniciAdi: "", sifre: "", rol: "Kullanici" as Rol };

export default function Kullanicilar() {
  const ben = useKullanici();
  const { veri: tumu, hata, yenile } = useVeri(() => kullanicilar.listele(), []);
  const { veri: bekleyenler, yenile: bekleyenleriYenile } = useVeri(() => hesap.onayBekleyenler(), []);
  // Onay bekleyenler ayrı bölümde; listede yalnız açılmış hesaplar
  const liste = tumu && bekleyenler ? tumu.filter((k) => !bekleyenler.some((b) => b.id === k.id)) : null;
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
      setMesaj({ tur: "success", metin: `${k.kullaniciAdi} kaydedildi.` });
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

  async function sil(k: Kullanici) {
    if (!window.confirm(`${k.kullaniciAdi} silinsin mi?`)) return;
    setMesaj(null);
    try {
      const r = await kullanicilar.sil(k.id);
      setMesaj({
        tur: "success",
        metin: r.silindi ? `${k.kullaniciAdi} silindi.` : `${k.kullaniciAdi} pasif yapıldı (geçmişte işlemi olduğu için kayıtlarda adı kalır).`,
      });
      if (duzenlenen?.id === k.id) vazgec();
      yenile();
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Silinemedi." });
    }
  }

  async function etkinlestir(k: Kullanici) {
    setMesaj(null);
    try {
      await kullanicilar.etkinlestir(k.id);
      setMesaj({ tur: "success", metin: `${k.kullaniciAdi} yeniden etkin.` });
      yenile();
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "İşlem yapılamadı." });
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

      {bekleyenler && bekleyenler.length > 0 && (
        <OnayBekleyenler
          bekleyenler={bekleyenler}
          degisti={(metin) => {
            setMesaj({ tur: "success", metin });
            bekleyenleriYenile();
            yenile();
          }}
          hata={(metin) => setMesaj({ tur: "error", metin })}
        />
      )}

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
                  <option value="Kullanici">Çalışan</option>
                  <option value="Admin">Yönetici</option>
                </select>
              </div>
              <div className="alan">
                <label htmlFor="k-sifre">{duzenlenen ? "Yeni şifre" : "Şifre"}</label>
                <SifreKutusu required={!duzenlenen} minLength={8} autoComplete="new-password" {...alan("sifre")} />
                {duzenlenen && <span className="yardim">Boşsa değişmez</span>}
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
                <tr key={k.id} className={k.aktif === false ? "satir--pasif" : undefined}>
                  <td>
                    {k.ad} {k.soyad}{k.id === ben.id && <span className="soluk"> · siz</span>}
                    {k.aktif === false && <span className="rozet rozet--kucuk rozet--uyari"> Pasif</span>}
                  </td>
                  <td className="mono">{k.kullaniciAdi}</td>
                  <td>{rolAdi(k.rol)}</td>
                  <td className="sag">
                    {k.aktif === false ? (
                      <button type="button" className="btn btn--ikincil btn--kucuk" onClick={() => etkinlestir(k)}>
                        Etkinleştir
                      </button>
                    ) : (
                      <span className="dugme-grup dugme-grup--sag">
                        <button type="button" className="btn-ikon" title="Düzenle" aria-label={`${k.kullaniciAdi} düzenle`} onClick={() => duzenle(k)}>
                          ✎
                        </button>
                        {k.id !== ben.id && (
                          <button type="button" className="btn-ikon" title="Sil" aria-label={`${k.kullaniciAdi} sil`} onClick={() => sil(k)}>
                            ✕
                          </button>
                        )}
                      </span>
                    )}
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

/** Kendi kaydolup yönetici onayı bekleyen hesaplar: onayla (Çalışan olarak açılır) ya da reddet (silinir). */
function OnayBekleyenler({ bekleyenler, degisti, hata }: {
  bekleyenler: { id: number; ad: string; soyad: string; kullaniciAdi: string; eposta: string | null }[];
  degisti: (mesaj: string) => void;
  hata: (mesaj: string) => void;
}) {
  const [bekliyor, setBekliyor] = useState(false);

  async function yap(is: () => Promise<void>, mesaj: string) {
    setBekliyor(true);
    try {
      await is();
      degisti(mesaj);
    } catch (err) {
      hata(err instanceof ApiHatasi ? err.message : "İşlem yapılamadı.");
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <section className="panel">
      <div className="panel-bas">
        <div className="baslik-grup">
          <h2>Onay bekleyen kayıtlar</h2>
        </div>
      </div>
      <div className="tablo-kap">
        <table className="tablo">
          <thead>
            <tr>
              <th>Ad soyad</th>
              <th>Kullanıcı adı</th>
              <th>E-posta</th>
              <th><span className="gorunmez">İşlem</span></th>
            </tr>
          </thead>
          <tbody>
            {bekleyenler.map((b) => (
              <tr key={b.id}>
                <td>{b.ad} {b.soyad}</td>
                <td className="mono">{b.kullaniciAdi}</td>
                <td>{b.eposta}</td>
                <td className="sag">
                  <span className="dugme-grup dugme-grup--sag">
                    <button type="button" className="btn btn--kucuk" disabled={bekliyor}
                      onClick={() => yap(() => hesap.onayla(b.id), `${b.kullaniciAdi} onaylandı.`)}>
                      Onayla
                    </button>
                    <button type="button" className="btn btn--ikincil btn--kucuk" disabled={bekliyor}
                      onClick={() => window.confirm(`${b.kullaniciAdi} reddedilsin mi?`)
                        && yap(() => hesap.reddet(b.id), `${b.kullaniciAdi} reddedildi.`)}>
                      Reddet
                    </button>
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}
