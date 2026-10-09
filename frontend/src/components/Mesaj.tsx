export type MesajTuru = "success" | "error" | "warning" | "info";
export interface MesajBilgisi { tur: MesajTuru; metin: string }

/** İşlem sonucu bildirimi (başarılı, hata, uyarı, bilgi). */
export default function Mesaj({ mesaj }: { mesaj: MesajBilgisi | null }) {
  if (!mesaj) return null;
  return (
    <div className={`mesaj mesaj--${mesaj.tur}`} role={mesaj.tur === "error" ? "alert" : "status"}>
      {mesaj.metin}
    </div>
  );
}
