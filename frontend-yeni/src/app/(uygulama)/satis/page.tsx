import { redirect } from "next/navigation";

/** Satış, sipariş + sevkiyata dönüştü; eski adres Sevkiyat'a gider. */
export default function Sayfa() {
  redirect("/sevkiyat");
}
