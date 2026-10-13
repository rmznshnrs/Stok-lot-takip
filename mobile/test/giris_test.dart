import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:lot_takip/main.dart';
import 'package:lot_takip/services/api.dart';
import 'package:lot_takip/services/ayarlar.dart';
import 'package:shared_preferences/shared_preferences.dart';

void main() {
  setUpAll(() => initializeDateFormatting('tr_TR'));

  Future<Ayarlar> ayarlarHazirla() async {
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({});
    return Ayarlar.yukle();
  }

  testWidgets('boş girişte uyarı; yanlış şifrede API mesajı gösterilir', (tester) async {
    final ayarlar = await ayarlarHazirla();
    final api = Api(
      ayarlar,
      istemci: MockClient(
        (_) async => http.Response.bytes(
          utf8.encode(jsonEncode({'title': 'Giriş başarısız', 'detail': 'Kullanıcı adı veya şifre hatalı.'})),
          401,
        ),
      ),
    );
    await tester.pumpWidget(Uygulama(ayarlar: ayarlar, api: api));

    expect(find.text('MKC BİLİŞİM'), findsOneWidget);
    await tester.tap(find.text('Giriş yap'));
    await tester.pump();
    expect(find.text('Kullanıcı adı ve şifre girin.'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextField, 'Kullanıcı adı'), 'admin');
    await tester.enterText(find.widgetWithText(TextField, 'Şifre'), 'yanlis');
    await tester.tap(find.text('Giriş yap'));
    await tester.pumpAndSettle();
    expect(find.text('Kullanıcı adı veya şifre hatalı.'), findsOneWidget);
    expect(ayarlar.girisYapildi, isFalse);
  });

  testWidgets('başarılı girişte alt menülü ana ekran açılır', (tester) async {
    // Telefon boyutu (360x900)
    tester.view.physicalSize = const Size(1080, 2700);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    final ayarlar = await ayarlarHazirla();
    const kullanici = {'id': 1, 'ad': 'Ali', 'soyad': 'Veli', 'kullaniciAdi': 'ali', 'rol': 'Kullanici'};
    final api = Api(
      ayarlar,
      istemci: MockClient((r) async {
        final govde = switch (r.url.path) {
          '/api/kimlik/giris' => {'token': 't', 'gecerlilikSonu': '2026-10-13T20:00:00Z', 'kullanici': kullanici},
          '/api/kimlik/ben' => kullanici,
          _ => <Object>[],
        };
        return http.Response.bytes(utf8.encode(jsonEncode(govde)), 200);
      }),
    );
    await tester.pumpWidget(Uygulama(ayarlar: ayarlar, api: api));

    await tester.enterText(find.widgetWithText(TextField, 'Kullanıcı adı'), 'ali');
    await tester.enterText(find.widgetWithText(TextField, 'Şifre'), 'sifre123');
    await tester.tap(find.text('Giriş yap'));
    await tester.pumpAndSettle();

    for (final sekme in ['Ana', 'Stok', 'Üretim', 'Satış', 'Daha fazla']) {
      expect(find.text(sekme), findsWidgets);
    }
    expect(find.text('Henüz hareket yok.'), findsOneWidget);

    // Çıkış: Daha fazla → Çıkış yap → giriş ekranı
    await tester.tap(find.text('Daha fazla'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Çıkış yap'));
    await tester.pumpAndSettle();
    expect(find.text('MKC BİLİŞİM'), findsOneWidget);
    expect(ayarlar.girisYapildi, isFalse);
  });
}
