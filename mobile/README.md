# Mobil (Flutter)

MKC Bilişim mobil uygulaması (Android). Web ile aynı API'yi kullanır; tüm API çağrıları
`lib/services/api.dart` içinde.

Alt menü: Ana (arama, hızlı işlemler, son hareketler), Stok (parçalar, lotlar, mal girişi),
Üretim, Satış (seri no okutma, satış geçmişi), Daha fazla (İzlenebilirlik, Ürünler, Parçalar,
koyu mod, şifre, sunucu adresi, çıkış). Lot ve seri numaraları kamerayla okutulabilir
(`mobile_scanner`); kamera yoksa elle yazılır. Token telefonda şifreli saklanır
(`flutter_secure_storage`).

## Gerekenler

- Flutter SDK (stable) ve Android Studio (Android SDK, NDK, emülatör)
- Çalışan API (`backend/LotTakip.Api`)

## Çalıştırma

Emülatörde (sunucu adresi varsayılan olarak `http://10.0.2.2:5291`, yani bilgisayardaki API):

```
flutter emulators --launch Medium_Phone_API_37.0
flutter run
```

Gerçek telefonda: telefon ile bilgisayar aynı ağda olmalı. API'yi ağa açık başlatın
(`dotnet run --project backend/LotTakip.Api --urls http://0.0.0.0:5291`), uygulamada giriş
ekranındaki "Sunucu" bağlantısından adresi bilgisayarın IP'si ile girin
(ör. `http://192.168.1.20:5291`).

APK: `flutter build apk --release` → `build/app/outputs/flutter-apk/app-release.apk`

## Testler

```
flutter test
dart analyze
```

Not: Proje yolu Türkçe karakter içerdiği için `android/gradle.properties` içinde
`android.overridePathCheck=true` var. `flutter analyze` bu yolda hata verirse `dart analyze` kullanın.
