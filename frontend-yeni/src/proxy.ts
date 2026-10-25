import { NextRequest, NextResponse } from "next/server";
import { OTURUM_CEREZI } from "@/lib/oturum";

// Sayfa yönlendirmesi: oturum çerezi yoksa giriş sayfasına, varsa girişten ana sayfaya.
// Token'ın geçerliliğini API denetler; süresi dolmuşsa ilk istekte 401 alınır ve çerez silinir.
export function proxy(istek: NextRequest) {
  const oturumVar = istek.cookies.has(OTURUM_CEREZI);
  const { pathname, search } = istek.nextUrl;

  if (pathname === "/giris") {
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
