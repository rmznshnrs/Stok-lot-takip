// Sunucu tarafı (route handler ve proxy) ortak ayarlar. Token tarayıcıda JavaScript'e
// hiç açılmaz: httpOnly çerezde durur, API'ye giden isteklere sunucu ekler.

export const OTURUM_CEREZI = "lt_oturum";

/** .NET API'nin adresi (sonunda / olmadan). */
export const API_ADRESI = (process.env.API_ADRESI ?? "http://localhost:5291").replace(/\/+$/, "");
