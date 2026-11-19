import { NextRequest, NextResponse } from "next/server";
import { API_ADRESI, OTURUM_CEREZI } from "@/lib/oturum";
import { oturumYok, sunucuyaUlasilamadi } from "@/lib/vekil";

type Baglam = { params: Promise<{ yol: string[] }> };

const ACIK_YOLLAR = ["hesap/kayit", "hesap/sifre-unuttum", "hesap/sifre-sifirla"];

async function ilet(istek: NextRequest, { params }: Baglam) {
  const { yol } = await params;
  const yolMetni = yol.map(encodeURIComponent).join("/");

  if (yolMetni.toLowerCase() === "kimlik/giris") {
    return NextResponse.json({ title: "Bulunamadı", status: 404 }, { status: 404 });
  }

  const token = istek.cookies.get(OTURUM_CEREZI)?.value;
  const acik = ACIK_YOLLAR.includes(yolMetni.toLowerCase());
  if (!token && !acik) return oturumYok();

  const basliklar = new Headers({ Accept: "application/json" });
  if (token) basliklar.set("Authorization", `Bearer ${token}`);
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
  const ek = yanit.headers.get("Content-Disposition");
  if (ek) cevapBasliklari.set("Content-Disposition", ek);
  const govde = yanit.status === 204 ? null : await yanit.arrayBuffer();
  const cevap = new NextResponse(govde, { status: yanit.status, headers: cevapBasliklari });

  if (yanit.status === 401) cevap.cookies.delete(OTURUM_CEREZI);
  return cevap;
}

export { ilet as GET, ilet as POST, ilet as PUT, ilet as DELETE };
