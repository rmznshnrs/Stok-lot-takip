"use client";

import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, stok, tedarikciler, type ParcaStok } from "@/lib/api";
import { bugun } from "@/lib/bicim";
import { useVeri } from "@/lib/kancalar";

const bosForm = () => ({ parcaId: "", tedarikci: "", lotNo: "", adet: "", siparisTarihi: bugun(), siparisNo: "" });

export default function MalGirisiFormu({ parcalar, kaydedildi }: { parcalar: ParcaStok[]; kaydedildi: () => void }) {
  const { veri: tedarikciListesi, yenile: tedarikcileriYenile } = useVeri(() => tedarikciler(), []);
  const [form, setForm] = useState(bosForm);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  const alan = (ad: keyof ReturnType<typeof bosForm>) => ({
    id: ad,
    value: form[ad],
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [ad]: e.target.value }),
  });

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    setBekliyor(true);
    try {
      const s = await stok.giris({
        parcaId: Number(form.parcaId),
        tedarikciAdi: form.tedarikci.trim(),
        lotNo: form.lotNo.trim(),
        adet: Number(form.adet),
        siparisTarihi: form.siparisTarihi,
        siparisNo: form.siparisNo.trim(),
      });
      const ek = s.yeniTedarikci ? ` ${s.lot.tedarikciAd} yeni tedarikçi olarak eklendi.` : "";
      setMesaj({ tur: "success", metin: `${s.lot.lotNo} · ${s.lot.parcaKod} · ${s.lot.girisAdet} ${parcaBirimi(parcalar, s.lot.parcaId)} stoğa eklendi.${ek}` });
      setForm({ ...bosForm(), parcaId: form.parcaId, tedarikci: form.tedarikci, siparisTarihi: form.siparisTarihi });
      if (s.yeniTedarikci) tedarikcileriYenile();
      kaydedildi();
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <section className="kart kart--form kart--form-genis">
      <h2>Mal girişi</h2>
      <form className="form-dikey" onSubmit={kaydet}>
        <Mesaj mesaj={mesaj} />
        <div className="alan">
          <label htmlFor="parcaId">Parça</label>
          <select className="girdi" required {...alan("parcaId")}>
            <option value="">Parça seçin</option>
            {parcalar.map((p) => (
              <option key={p.id} value={p.id}>{p.kod} – {p.ad}</option>
            ))}
          </select>
        </div>
        <div className="alan">
          <label htmlFor="tedarikci">Tedarikçi (nereden)</label>
          <input
            className="girdi"
            list="tedarikci-listesi"
            placeholder="Tedarikçi seçin veya yazın"
            autoComplete="off"
            required
            maxLength={200}
            {...alan("tedarikci")}
          />
        </div>
        <div className="alan">
          <label htmlFor="lotNo">Lot numarası</label>
          <input className="girdi girdi--mono" placeholder="L-…" required maxLength={100} {...alan("lotNo")} />
        </div>
        <div className="izgara-2">
          <div className="alan">
            <label htmlFor="adet">Adet</label>
            <input type="number" min={1} className="girdi" required {...alan("adet")} />
          </div>
          <div className="alan">
            <label htmlFor="siparisTarihi">Sipariş tarihi</label>
            <input type="date" className="girdi" required {...alan("siparisTarihi")} />
          </div>
        </div>
        <div className="alan">
          <label htmlFor="siparisNo">Sipariş / irsaliye no</label>
          <input className="girdi" placeholder="örn. SP-1050" maxLength={100} {...alan("siparisNo")} />
        </div>
        <datalist id="tedarikci-listesi">
          {tedarikciListesi?.map((t) => <option key={t.id} value={t.ad} />)}
        </datalist>
        <button type="submit" className="btn btn--tam" disabled={bekliyor}>Stoğa ekle</button>
      </form>
    </section>
  );
}

function parcaBirimi(parcalar: ParcaStok[], id: number) {
  return parcalar.find((p) => p.id === id)?.birim ?? "adet";
}
