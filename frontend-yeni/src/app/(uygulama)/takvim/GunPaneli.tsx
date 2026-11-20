"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, ozet, parcalar, sira, tedarikciler, urunler, type TakvimGunu, type UretimEmri } from "@/lib/api";
import { tarih } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";
import { talepAdi } from "../musteriler/MusteriSiparisleri";

export default function GunPaneli({ gun, veri, isler, kapat, degisti }: {
  gun: string;
  veri: TakvimGunu | undefined;
  isler: UretimEmri[];
  kapat: () => void;
  degisti: () => void;
}) {
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);

  async function calistir(is_: () => Promise<unknown>, basari: string) {
    setMesaj(null);
    try {
      await is_();
      setMesaj({ tur: "success", metin: basari });
      degisti();
      return true;
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? eksikMetni(err) : "Kaydedilemedi." });
      return false;
    }
  }

  return (
    <section className="panel gun-paneli">
      <div className="panel-bas">
        <h3>{tarih(gun)}</h3>
        <button type="button" className="btn btn--ikincil btn--kucuk" onClick={kapat}>Kapat</button>
      </div>
      <div className="panel-ic form-dikey">
        <Mesaj mesaj={mesaj} />

        {veri && veri.urunler.length > 0 && (
          <div className="tablo-kap">
            <table className="tablo tablo--sik">
              <thead>
                <tr>
                  <th>Ürün</th>
                  <th className="sag">Planlanan Üretim</th>
                  <th className="sag">Üretilen</th>
                  <th className="sag">Teslimi İstenen</th>
                </tr>
              </thead>
              <tbody>
                {veri.urunler.map((u) => (
                  <tr key={u.urunId} className={u.istenen > u.planlanan + u.uretilen ? "uyari-satir" : undefined}>
                    <td>{u.urunAd}</td>
                    <td className="sag">{u.planlanan}</td>
                    <td className="sag">{u.uretilen}</td>
                    <td className="sag">{u.istenen}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {isler.length > 0 && (
          <div className="form-dikey">
            <strong>Bu Güne Planlı Üretim İşleri</strong>
            {isler.map((e) => (
              <div key={e.id} className="bas-satir kutu kutu--ince">
                <span>
                  {e.urunAd} · {e.adet - e.uretilen} adet
                  {e.siparisId && <span className="yardim"> · {talepAdi(e.musteriAd, e.talepTarihi)}</span>}
                  {e.aciklama && <span className="yardim"> · {e.aciklama}</span>}
                </span>
                <Link href={`/uretim?eklendi=${e.id}`} className="btn-baglanti">Sırada Gör</Link>
              </div>
            ))}
          </div>
        )}

        {veri && veri.planlar.length > 0 && (
          <div className="form-dikey">
            <strong>Parça Alımları</strong>
            {veri.planlar.map((p) => (
              <div key={p.id} className={`bas-satir kutu kutu--ince${p.tamamlandi ? " soluk" : ""}`}>
                <span className={p.tamamlandi ? "ustu-cizili" : undefined}>
                  {p.parca ? <>Parça alımı: <strong>{p.parca.ad}</strong> <span className="mono yardim">{p.parca.kod}</span>{p.adet && ` × ${p.adet}`}</> : "Not"}
                  {p.tedarikciAd && ` · ${p.tedarikciAd}`}
                  {p.aciklama && ` · ${p.aciklama}`}
                </span>
                <span className="dugme-grup">
                  <button type="button" className="btn btn--ikincil btn--kucuk"
                    onClick={() => calistir(() => ozet.planTamam(p.id, !p.tamamlandi), p.tamamlandi ? "Yeniden açıldı." : "Yapıldı olarak işaretlendi.")}>
                    {p.tamamlandi ? "Geri Al" : "Yapıldı"}
                  </button>
                  <button type="button" className="btn btn--ikincil btn--kucuk"
                    onClick={() => confirm("Hatırlatıcı silinsin mi?") && calistir(() => ozet.planSil(p.id), "Hatırlatıcı silindi.")}>
                    Sil
                  </button>
                </span>
              </div>
            ))}
          </div>
        )}

        <div className="izgara-2 gun-formlari">
          <UretimPlani gun={gun} calistir={calistir} />
          <AlimPlani gun={gun} calistir={calistir} />
        </div>
      </div>
    </section>
  );
}

type Calistir = (is_: () => Promise<unknown>, basari: string) => Promise<boolean>;

function UretimPlani({ gun, calistir }: { gun: string; calistir: Calistir }) {
  const { veri: urunListesi } = useVeri(() => urunler.listele(), []);
  const [urunId, setUrunId] = useState("");
  const [adet, setAdet] = useState("1");
  const [aciklama, setAciklama] = useState("");

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    const ad = urunListesi?.find((u) => String(u.id) === urunId)?.ad ?? "";
    const tamam = await calistir(
      () => sira.ekle({ urunId: Number(urunId), adet: Number(adet), planlananTarih: gun, aciklama: aciklama.trim() }),
      `${adet} adet ${ad} bu güne üretim sırasına eklendi.`);
    if (tamam) {
      setAdet("1");
      setAciklama("");
    }
  }

  return (
    <form className="kutu form-dikey" onSubmit={kaydet}>
      <strong className="takvim-cubuk takvim-cubuk--plan">Ürün Üretimi Planla</strong>
      <div className="alan">
        <label htmlFor="plan-urun">Ürün</label>
        <select id="plan-urun" className="girdi" required value={urunId} onChange={(e) => setUrunId(e.target.value)}>
          <option value="">Seçin</option>
          {urunListesi?.map((u) => <option key={u.id} value={u.id}>{u.ad}</option>)}
        </select>
      </div>
      <div className="alan">
        <label htmlFor="plan-adet">Adet</label>
        <input id="plan-adet" type="number" min={1} max={999} required className="girdi" value={adet} onChange={(e) => setAdet(e.target.value)} />
      </div>
      <div className="alan">
        <label htmlFor="plan-aciklama">Açıklama</label>
        <input id="plan-aciklama" className="girdi" maxLength={500} value={aciklama} onChange={(e) => setAciklama(e.target.value)} />
      </div>
      <div><button type="submit" className="btn btn--kucuk">Sıraya Ekle</button></div>
    </form>
  );
}

function AlimPlani({ gun, calistir }: { gun: string; calistir: Calistir }) {
  const { veri: parcaListesi } = useVeri(() => parcalar.listele(), []);
  const { veri: tedarikciListesi } = useVeri(() => tedarikciler(), []);
  const [parcaId, setParcaId] = useState("");
  const [adet, setAdet] = useState("");
  const [tedarikciId, setTedarikciId] = useState("");

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    const tamam = await calistir(() => ozet.planEkle({
      tarih: gun, parcaId: Number(parcaId), adet: adet ? Number(adet) : undefined,
      tedarikciId: tedarikciId ? Number(tedarikciId) : undefined,
    }), "Parça alımı eklendi.");
    if (tamam) {
      setParcaId("");
      setAdet("");
      setTedarikciId("");
    }
  }

  return (
    <form className="kutu form-dikey" onSubmit={kaydet}>
      <strong className="takvim-cubuk takvim-cubuk--alim">Parça Alımı</strong>
      <div className="alan">
        <label htmlFor="alim-parca">Parça</label>
        <select id="alim-parca" className="girdi" required value={parcaId} onChange={(e) => setParcaId(e.target.value)}>
          <option value="">Seçin</option>
          {parcaListesi?.map((p) => <option key={p.id} value={p.id}>{p.kod} · {p.ad} ({p.kullanilabilirStok} adet)</option>)}
        </select>
      </div>
      <div className="izgara-2">
        <div className="alan">
          <label htmlFor="alim-adet">Adet</label>
          <input id="alim-adet" type="number" min={1} className="girdi" value={adet} onChange={(e) => setAdet(e.target.value)} />
        </div>
        <div className="alan">
          <label htmlFor="alim-tedarikci">Tedarikçi</label>
          <select id="alim-tedarikci" className="girdi" value={tedarikciId} onChange={(e) => setTedarikciId(e.target.value)}>
            <option value="">Seçin</option>
            {tedarikciListesi?.map((t) => <option key={t.id} value={t.id}>{t.ad}</option>)}
          </select>
        </div>
      </div>
      <div><button type="submit" className="btn btn--kucuk">Parça Alımı Ekle</button></div>
    </form>
  );
}

function eksikMetni(h: ApiHatasi) {
  if (!h.eksikler.length) return h.message;
  return `${h.message} ${h.eksikler.map((e) => `${e.parca.kod}: ${e.eksik} eksik`).join(", ")}.`;
}
