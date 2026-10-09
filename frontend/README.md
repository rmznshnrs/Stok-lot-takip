# Frontend (Next.js)

Next.js (App Router, TypeScript) ile web arayüzü. Gerekli: Node.js LTS.

## Çalıştırma

Önce API açık olmalı (`backend/LotTakip.Api`, http://localhost:5291).

```
npm install
npm run dev
```

Tarayıcıda http://localhost:3000 açılır, giriş sayfasına yönlenir.

API başka adresteyse `.env.example` dosyasını `.env.local` adıyla kopyalayıp `API_ADRESI`'ni değiştirin.

## Yapı

- `src/lib/api.ts`: tüm API çağrıları ve veri tipleri.
- `src/app/api/oturum`: giriş/çıkış. Token httpOnly çerezde saklanır, tarayıcıdaki koda açılmaz.
- `src/app/api/[...yol]`: `/api/...` isteklerini token ekleyerek .NET API'ye iletir.
- `src/proxy.ts`: oturum yoksa sayfaları giriş ekranına yönlendirir.
- `src/app/(uygulama)`: giriş gerektiren ekranlar (üst menülü düzen).
