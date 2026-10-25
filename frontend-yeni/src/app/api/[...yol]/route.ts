import { NextRequest, NextResponse } from "next/server";
import { API_ADRESI, OTURUM_CEREZI } from "@/lib/oturum";
import { oturumYok, sunucuyaUlasilamadi } from "@/lib/vekil";

// Tarayıcıdan gelen /api/... isteklerini .NET API'ye iletir; token'ı çerezden alıp ekler.
// Böylece token JavaScript'e açılmaz ve CORS gerekmez.

type Baglam = { params: Promise<{ yol: string[] }> };

async function ilet(istek: NextRequest, { params }: Baglam) {
  const { yol } = await params;
  const yolMetni = yol.map(encodeURIComponent).join("/");

  // Giriş yalnız /api/oturum üzerinden: token tarayıcıya dönmesin
  if (yolMetni.toLowerCase() === "kimlik/giris") {
    return NextResponse.json({ title: "Bulunamadı", status: 404 }, { status: 404 });
  }

  const token = istek.cookies.get(OTURUM_CEREZI)?.value;
  if (!token) return oturumYok();

  const basliklar = new Headers({ Authorization: `Bearer ${token}`, Accept: "application/json" });
  const icerikTuru = istek.headers.get("Content-Type");
  if (icerikTuru) basliklar.set("Content-Type", icerikTuru);

  const govdeVar = istek.method !== "GET" && istek.method !== "HEAD";
  let yanit: Response;
  try {
    yanit = await fetch(`${API_ADRESI}/api/${yolMetni}${istek.nextUrl.search}`, {
      method: istek.method,
      headers: basliklar,
      body: govdeVar ? await istek.arrayBuffer() : undefined,
      cache: "no-store",
    });
  } catch {
    return sunucuyaUlasilamadi();
  }

  const cevapBasliklari = new Headers();
  const turu = yanit.headers.get("Content-Type");
  if (turu) cevapBasliklari.set("Content-Type", turu);
  const govde = yanit.status === 204 ? null : await yanit.arrayBuffer();
  const cevap = new NextResponse(govde, { status: yanit.status, headers: cevapBasliklari });

  // Süresi dolmuş ya da geçersiz token: çerezi sil, istemci giriş sayfasına yönlenir
  if (yanit.status === 401) cevap.cookies.delete(OTURUM_CEREZI);
  return cevap;
}

export { ilet as GET, ilet as POST, ilet as PUT, ilet as DELETE };
