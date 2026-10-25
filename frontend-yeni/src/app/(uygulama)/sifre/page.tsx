import { redirect } from "next/navigation";

/** Eski adres: şifre değiştirme artık Profil sayfasında. */
export default function Sayfa() {
  redirect("/profil");
}
