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

Map<String, Object> _onizleme(int adet) => {
  'urunId': 13,
  'urunKod': 'IP',
  'urunAd': 'Panel',
  'adet': adet,
  'parcalar': [
    {
      'parca': {'id': 3, 'kod': 'D-1', 'ad': 'Diyot', 'birim': 'adet'},
      'birimAdet': 1,
      'gereken': adet,
      'kullanilabilir': 2,
      'lotlar': [
        {'lotId': 1, 'lotNo': 'L-D-1', 'adet': adet > 2 ? 2 : adet},
      ],
      'atlananLotlar': <String>[],
      'eksik': adet > 2 ? adet - 2 : 0,
    },
  ],
  'seriNolar': [for (var i = 1; i <= adet; i++) 'SN-IP-000$i'],
  'eksikler': <Object>[],
  'yeterli': adet <= 2,
  'atlananLotlar': <String>[],
};

void main() {
  setUpAll(() => initializeDateFormatting('tr_TR'));

  testWidgets('üretim: ürün seçilince parça kontrolü gelir, stok yetmezse kaydet pasif', (tester) async {
    tester.view.physicalSize = const Size(1080, 2700);
    tester.view.devicePixelRatio = 3;
    addTearDown(tester.view.reset);
    SharedPreferences.setMockInitialValues({});
    FlutterSecureStorage.setMockInitialValues({'token': 't'});
    final ayarlar = await Ayarlar.yukle();
    final api = Api(
      ayarlar,
      istemci: MockClient((r) async {
        final govde = switch (r.url.path) {
          '/api/kimlik/ben' => {'id': 1, 'ad': 'Ali', 'soyad': 'Veli', 'kullaniciAdi': 'ali', 'rol': 'Kullanici'},
          '/api/urunler' => [
            {'id': 13, 'kod': 'IP', 'ad': 'Panel', 'seriOneki': 'SN-IP-', 'parcaCesidi': 1, 'uretimSayisi': 0},
          ],
          '/api/uretim/onizleme' => _onizleme(int.parse(r.url.queryParameters['adet']!)),
          _ => <Object>[],
        };
        return http.Response.bytes(utf8.encode(jsonEncode(govde)), 200);
      }),
    );
    await tester.pumpWidget(Uygulama(ayarlar: ayarlar, api: api));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Üretim'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Panel'));
    await tester.pumpAndSettle(const Duration(milliseconds: 300));
    expect(find.text('Yeterli'), findsOneWidget);
    expect(find.text('Diyot'), findsOneWidget);

    await tester.tap(find.byIcon(Icons.add).first);
    await tester.tap(find.byIcon(Icons.add).first);
    await tester.pumpAndSettle(const Duration(milliseconds: 300));
    expect(find.text('3'), findsOneWidget);
    expect(find.text('1 eksik'), findsOneWidget);
    expect(find.text('Eksik parça var'), findsOneWidget);
  });
}
