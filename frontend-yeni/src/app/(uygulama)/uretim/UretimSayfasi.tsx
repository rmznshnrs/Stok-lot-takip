"use client";

import GeriIleri from "@/components/GeriIleri";
import { useSearchParams } from "next/navigation";
import SirayaEkle from "./SirayaEkle";
import UretimGecmisi from "./UretimGecmisi";
import UretimSirasi from "./UretimSirasi";

export default function UretimSayfasi() {
  const params = useSearchParams();
  const sekme = params.get("sekme");
  const gecmis = sekme === "gecmis";
  const uretimde = sekme === "uretimde";
  const yeni = params.get("yeni") === "1";
  const ara = params.get("ara")?.trim() ?? "";
  return (
    <>
      <div className="ust-sekme-satiri">
        <h1>Üretim</h1>
      </div>
      {gecmis ? <UretimGecmisi key={ara} ilkArama={ara} /> : yeni ? <><div className="geri-ileri-satiri"><GeriIleri /></div><SirayaEkle /></> : <UretimSirasi key={String(uretimde)} uretimde={uretimde} />}
    </>
  );
}
