import { NextRequest, NextResponse } from "next/server";
import { API_ADRESI, OTURUM_CEREZI } from "@/lib/oturum";
import { sunucuyaUlasilamadi } from "@/lib/vekil";

// Giriş: API'den token alınır, httpOnly çereze yazılır; tarayıcıya yalnız kullanıcı bilgisi döner.
export async function POST(istek: NextRequest) {
  let yanit: Response;
  try {
    yanit = await fetch(`${API_ADRESI}/api/kimlik/giris`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: await istek.text(),
      cache: "no-store",
    });
  } catch {
    return sunucuyaUlasilamadi();
  }

  if (!yanit.ok) {
    return new NextResponse(await yanit.text(), {
      status: yanit.status,
      headers: { "Content-Type": yanit.headers.get("Content-Type") ?? "application/problem+json" },
    });
  }

  const sonuc: { token: string; gecerlilikSonu: string; kullanici: unknown } = await yanit.json();
  const sure = Math.floor((Date.parse(sonuc.gecerlilikSonu) - Date.now()) / 1000);
  const cevap = NextResponse.json({ kullanici: sonuc.kullanici });
  cevap.cookies.set(OTURUM_CEREZI, sonuc.token, {
    httpOnly: true,
    sameSite: "lax",
    secure: istek.nextUrl.protocol === "https:",
    path: "/",
    maxAge: Math.max(sure, 0),
  });
  return cevap;
}

// Çıkış: çerez silinir.
export async function DELETE() {
  const cevap = new NextResponse(null, { status: 204 });
  cevap.cookies.delete(OTURUM_CEREZI);
  return cevap;
}
