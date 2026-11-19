import { NextResponse } from "next/server";

export function sunucuyaUlasilamadi() {
  return NextResponse.json(
    { title: "Sunucuya ulaşılamadı", status: 502, detail: "API çalışmıyor ya da adresi yanlış." },
    { status: 502, headers: { "Content-Type": "application/problem+json" } },
  );
}

export function oturumYok() {
  return NextResponse.json(
    { title: "Oturum gerekli", status: 401, detail: "Lütfen giriş yapın." },
    { status: 401, headers: { "Content-Type": "application/problem+json" } },
  );
}
