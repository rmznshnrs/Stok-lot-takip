"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import { ApiHatasi, stok, tedarikciler, type ParcaStok } from "@/lib/api";
import { bugun } from "@/lib/bicim";
import { numaraAyikla } from "@/lib/gs1";
import { useVeri } from "@/lib/kancalar";

const bosForm = () => ({ parcaId: "", tedarikci: "", tedarikciLotNo: "", adet: "", siparisTarihi: bugun(), siparisNo: "" });

export default function MalGirisiFormu({ parcalar, kaydedildi }: { parcalar: ParcaStok[]; kaydedildi: () => void }) {
  const { veri: tedarikciListesi, yenile: tedarikcileriYenile } = useVeri(() => tedarikciler(), []);
  const [form, setForm] = useState(bosForm);
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [sonLot, setSonLot] = useState<string | null>(null);
  const [bekliyor, setBekliyor] = useState(false);

  const alan = (ad: keyof ReturnType<typeof bosForm>) => ({
    id: ad,
    value: form[ad],
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [ad]: e.target.value }),
  });

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    setSonLot(null);
    setBekliyor(true);
    try {
      const s = await stok.giris({
        parcaId: Number(form.parcaId),
        tedarikciAdi: form.tedarikci.trim(),
        // Stok giriş no sistemce verilir. Tedarikçinin GS1 karekodu okutulduysa içinden lot numarası alınır
        tedarikciLotNo: numaraAyikla(form.tedarikciLotNo, "lot"),
        adet: Number(form.adet),
        siparisTarihi: form.siparisTarihi,
        siparisNo: form.siparisNo.trim(),
      });
      setMesaj({ tur: "success", metin: `${s.lot.lotNo} kaydedildi.` });
      setSonLot(s.lot.lotNo);
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
    <section className="acilir-form">
      <h3>Mal girişi</h3>
      <form className="form-dikey" onSubmit={kaydet}>
        <Mesaj mesaj={mesaj} />
        {sonLot && (
          <div>
            <Link href={`/etiket?lot=${encodeURIComponent(sonLot)}`} className="btn btn--ikincil">
              {sonLot} için etiket yazdır
            </Link>
          </div>
        )}
        <div className="izgara-form">
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
            <label htmlFor="tedarikci">Tedarikçi</label>
            <input
              className="girdi"
              list="tedarikci-listesi"
              autoComplete="off"
              required
              maxLength={200}
              {...alan("tedarikci")}
            />
          </div>
          <div className="alan">
            <label htmlFor="tedarikciLotNo">Tedarikçi lot no (isteğe bağlı)</label>
            <input className="girdi girdi--mono" maxLength={100} {...alan("tedarikciLotNo")} />
          </div>
          <div className="alan">
            <label htmlFor="adet">Adet</label>
            <input type="number" min={1} className="girdi" required {...alan("adet")} />
          </div>
          <div className="alan">
            <label htmlFor="siparisTarihi">Sipariş tarihi</label>
            <input type="date" className="girdi" required {...alan("siparisTarihi")} />
          </div>
          <div className="alan">
            <label htmlFor="siparisNo">Sipariş / irsaliye no</label>
            <input className="girdi" maxLength={100} {...alan("siparisNo")} />
          </div>
        </div>
        <datalist id="tedarikci-listesi">
          {tedarikciListesi?.map((t) => <option key={t.id} value={t.ad} />)}
        </datalist>
        <div>
          <button type="submit" className="btn" disabled={bekliyor}>Stoğa ekle</button>
        </div>
      </form>
    </section>
  );
}
