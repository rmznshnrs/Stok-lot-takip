# Lot Takip

Elektronik ürün üreten bir firma için stok, üretim ve satış takibi; lot ve seri numarası
bazında izlenebilirlik.

- **Parçadan bütüne:** bir lottaki parça ne zaman, nereden alındı; hangi ürünlerde (seri no)
  kullanıldı; o ürünler kime satıldı?
- **Bütünden parçaya:** bir seri numaralı ürünün içinde hangi parçalar, hangi lotlardan, kaçar adet var?

## Yapı

| Klasör | İçerik |
|---|---|
| `backend/` | .NET 10 Web API (katmanlar: Entity, DataAccess, Business, Shared, Api, Tests) |
| `frontend/` | Next.js web arayüzü |
| `mobile/` | Flutter mobil uygulama |

Veritabanı SQL Server; şema EF Core migration'larıyla kurulur.

## Gerekenler

- .NET SDK 10 (sürüm `global.json`'da)
- SQL Server (Express yeterli)
- Node.js LTS

## Kurulum

1. Ayar dosyasını oluşturun:

   ```
   copy backend\LotTakip.Api\appsettings.Development.example.json backend\LotTakip.Api\appsettings.Development.json
   ```

   İçinde bağlantı cümlesini, `OrnekVeri:AdminSifre`'yi ve `Jwt:Anahtar`'ı (en az 32 karakter,
   rastgele) doldurun. Bu dosya repoya girmez.

2. Veritabanını kurup örnek veriyi yükleyin (migration'lar da uygulanır):

   ```
   dotnet run --project backend/LotTakip.Api -- ornek-veri
   ```

   `--sifirla` eklenirse mevcut veri silinip örnek veri baştan yüklenir.
   Giriş: kullanıcı adı `admin`, şifre ayar dosyasındaki `OrnekVeri:AdminSifre`.

## Çalıştırma

API (http://localhost:5291, geliştirme ortamında Swagger: http://localhost:5291/swagger):

```
dotnet run --project backend/LotTakip.Api
```

Web arayüzü (http://localhost:3000):

```
cd frontend
npm install
npm run dev
```

## Testler

```
dotnet test LotTakip.sln
```

Testler gerçek SQL Server'da ayrı bir veritabanında (`LotTakip_Test`) çalışır; her çalıştırmada
yeniden kurulur. Başka bir ad için `LOTTAKIP_TEST_DB` ortam değişkeni kullanılır.

Web arayüzü için: `npm run lint` ve `npm run build` (`frontend` klasöründe).

## Temel kurallar

- Stok düşümü FIFO: sipariş tarihi en eski lot önce kullanılır; gerekirse birden çok lottan düşülür.
- Geri çağrılan lotlar üretimde kullanılmaz; bu lotları içeren ürünler satışta uyarı verir ve
  ancak onayla satılır.
- Stok yetmezse üretim kaydedilmez, eksik parçalar ve adetleri gösterilir.
- N adet üretimde N ayrı kayıt ve ardışık seri numarası oluşur (`SN-IP-0001` gibi).
- Bir seri numarası yalnız bir kez satılır. Üretim ve satış kayıtları silinmez.
- Kullanıcı yönetimi, lot geri çağırma ve stok düzeltme yalnız yönetici (Admin) rolündedir.
