import { NextRequest, NextResponse } from "next/server";
import { OTURUM_CEREZI } from "@/lib/oturum";

const ACIK_SAYFALAR = ["/giris", "/kayit", "/sifremi-unuttum", "/sifre-sifirla"];

export function proxy(istek: NextRequest) {
  const oturumVar = istek.cookies.has(OTURUM_CEREZI);
  const { pathname, search } = istek.nextUrl;

  if (ACIK_SAYFALAR.includes(pathname)) {
    return oturumVar ? NextResponse.redirect(new URL("/", istek.url)) : NextResponse.next();
  }
  if (!oturumVar) {
    const adres = new URL("/giris", istek.url);
    if (pathname !== "/") adres.searchParams.set("sonraki", pathname + search);
    return NextResponse.redirect(adres);
  }
  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico|.*\\.(?:png|svg|ico|webmanifest)$).*)"],
};
