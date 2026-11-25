"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import Mesaj, { type MesajBilgisi } from "@/components/Mesaj";
import NumaraUyarisi, { useNumaraKontrol } from "@/components/NumaraUyarisi";
import SayiGirdi from "@/components/SayiGirdi";
import SecimVeyaYeni from "@/components/SecimVeyaYeni";
import { ApiHatasi, stok, tedarikciler, type ParcaStok } from "@/lib/api";
import { bugun } from "@/lib/bicim";
import { numaraAyikla } from "@/lib/gs1";
import { useVeri } from "@/lib/kancalar";

const bosForm = (tedarikci = "") => ({ parcaId: "", tedarikci, lotNo: "", adet: "", siparisTarihi: bugun(), siparisNo: "" });

export default function MalGirisiFormu({ parcalar, kaydedildi, tedarikci, baslik = "Mal Girişi" }: {
  parcalar: ParcaStok[];
  kaydedildi: (mesaj: string) => void;
  tedarikci?: string;
  baslik?: string;
}) {
  const { veri: tedarikciListesi, yenile: tedarikcileriYenile } = useVeri(() => tedarikciler(), []);
  const [form, setForm] = useState(() => bosForm(tedarikci));
  const [mesaj, setMesaj] = useState<MesajBilgisi | null>(null);
  const [sonLot, setSonLot] = useState<string | null>(null);
  const [bekliyor, setBekliyor] = useState(false);
  const lotUyarisi = useNumaraKontrol("lot", form.lotNo);

  const alan = (ad: keyof ReturnType<typeof bosForm>) => ({
    id: ad,
    value: form[ad],
    onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) => setForm({ ...form, [ad]: e.target.value }),
  });

  async function kaydet(e: FormEvent) {
    e.preventDefault();
    setMesaj(null);
    setSonLot(null);
    if (lotUyarisi) return setMesaj({ tur: "error", metin: lotUyarisi });
    setBekliyor(true);
    try {
      const s = await stok.giris({
        parcaId: Number(form.parcaId),
        tedarikciAdi: form.tedarikci.trim(),
        lotNo: numaraAyikla(form.lotNo, "lot"),
        adet: Number(form.adet),
        siparisTarihi: form.siparisTarihi,
        siparisNo: form.siparisNo.trim(),
      });
      const ek = s.sirayaGirenler?.length ? ` Parça geldiği için üretim sırasına girenler: ${s.sirayaGirenler.join(", ")}.` : "";
      setMesaj({ tur: "success", metin: `${s.lot.lotNo} kaydedildi.${ek}` });
      setSonLot(s.lot.lotNo);
      setForm({ ...bosForm(form.tedarikci), parcaId: form.parcaId, siparisTarihi: form.siparisTarihi });
      if (s.yeniTedarikci) tedarikcileriYenile();
      kaydedildi(`${s.lot.lotNo} kaydedildi.${ek}`);
    } catch (err) {
      setMesaj({ tur: "error", metin: err instanceof ApiHatasi ? err.message : "Kaydedilemedi." });
    } finally {
      setBekliyor(false);
    }
  }

  return (
    <section className="acilir-form">
      <h3>{baslik}</h3>
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
          {!tedarikci && (
            <div className="alan">
              <label htmlFor="tedarikci">Tedarikçi</label>
              <SecimVeyaYeni id="tedarikci" required secenekler={tedarikciListesi?.map((t) => t.ad) ?? []}
                value={form.tedarikci} onChange={(v) => setForm({ ...form, tedarikci: v })}
                bosEtiketi="Tedarikçi seçin" yeniEtiketi="Yeni tedarikçi…" />
            </div>
          )}
          <div className="alan">
            <label htmlFor="lotNo">Lot No</label>
            <input className="girdi girdi--mono" required maxLength={50} {...alan("lotNo")} />
            <NumaraUyarisi tur="lot" deger={form.lotNo} />
          </div>
          <div className="alan">
            <label htmlFor="adet">Adet</label>
            <SayiGirdi id="adet" min={1} required value={form.adet} onChange={(v) => setForm({ ...form, adet: v })} />
          </div>
          <div className="alan">
            <label htmlFor="siparisTarihi">Sipariş Tarihi</label>
            <input type="date" className="girdi" required {...alan("siparisTarihi")} />
          </div>
          <div className="alan">
            <label htmlFor="siparisNo">Sipariş / İrsaliye No</label>
            <input className="girdi" maxLength={100} {...alan("siparisNo")} />
          </div>
        </div>
        <div>
          <button type="submit" className="btn" disabled={bekliyor}>{bekliyor ? "Kaydediliyor…" : "Stoğa Ekle"}</button>
        </div>
      </form>
    </section>
  );
}
