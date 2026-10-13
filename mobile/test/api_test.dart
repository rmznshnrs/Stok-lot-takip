import 'dart:convert';

import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';
import 'package:lot_takip/services/api.dart';
import 'package:lot_takip/services/ayarlar.dart';
import 'package:lot_takip/services/modeller.dart';
import 'package:shared_preferences/shared_preferences.dart';

http.Response _json(Object govde, [int durum = 200]) =>
    http.Response.bytes(utf8.encode(jsonEncode(govde)), durum, headers: {'content-type': 'application/json; charset=utf-8'});

Map<String, dynamic> _lot({String lotNo = 'L-D-2601', bool geri = false}) => {
      'id': 1, 'lotNo': lotNo, 'parcaId': 3, 'parcaKod': 'D-1N4007', 'parcaAd': 'Diyot', 'tedarikciAd': 'Elektro',
      'siparisTarihi': '2026-01-10', 'siparisNo': 'SP-1', 'girisAdet': 30, 'kalanAdet': 18, 'geriCagrildi': geri,
      'girisZamani': '2026-01-17T09:30:00+03:00', 'sirada': true,
    };

void main() {
  late Ayarlar ayarlar;
  final istekler = <http.Request>[];

  Api apiOlustur(Future<http.Response> Function(http.Request) yanit) => Api(ayarlar, istemci: MockClient((r) {
        istekler.add(r);
        return yanit(r);
      }));

  setUp(() async {
    SharedPreferences.setMockInitialValues({'api_adresi': 'http://sunucu:5291'});
    FlutterSecureStorage.setMockInitialValues({});
    ayarlar = await Ayarlar.yukle();
    istekler.clear();
  });

  test('giriş token\'ı saklar, sonraki istekler Bearer ile gider', () async {
    final api = apiOlustur((r) async => r.url.path.endsWith('giris')
        ? _json({
            'token': 'abc',
            'gecerlilikSonu': '2026-10-13T20:00:00Z',
            'kullanici': {'id': 1, 'ad': 'Ali', 'soyad': 'Veli', 'kullaniciAdi': 'ali', 'rol': 'Admin'},
          })
        : _json({'id': 1, 'ad': 'Ali', 'soyad': 'Veli', 'kullaniciAdi': 'ali', 'rol': 'Admin'}));

    final k = await api.giris('ali', 'sifre123');
    expect(k.admin, isTrue);
    expect(ayarlar.token, 'abc');
    expect(istekler.first.url.toString(), 'http://sunucu:5291/api/kimlik/giris');
    expect(jsonDecode(istekler.first.body), {'kullaniciAdi': 'ali', 'sifre': 'sifre123'});
    expect(istekler.first.headers.containsKey('Authorization'), isFalse);

    await api.ben();
    expect(istekler.last.headers['Authorization'], 'Bearer abc');
  });

  test('401 gelince token silinir (giriş ekranına dönülür)', () async {
    await ayarlar.tokenKaydet('eski');
    final api = apiOlustur((_) async => http.Response('', 401));
    await expectLater(api.parcalar(), throwsA(isA<ApiHatasi>().having((h) => h.durum, 'durum', 401)));
    expect(ayarlar.girisYapildi, isFalse);
  });

  test('hata yanıtındaki mesaj, eksikler ve uyarılar okunur', () async {
    await ayarlar.tokenKaydet('t');
    final api = apiOlustur((_) async => _json({
          'title': 'Satış yapılamadı',
          'status': 409,
          'detail': 'Geri çağrılan lot içeren ürünler var.',
          'uyarilar': [
            {'seriNo': 'SN-TK-0001', 'lotlar': ['L-KAP-01']},
          ],
        }, 409));
    try {
      await api.sat(musteri: 'Gama', seriNolar: ['SN-TK-0001'], tarih: DateTime(2026, 10, 13), geriCagrilanOnayi: false);
      fail('hata bekleniyordu');
    } on ApiHatasi catch (h) {
      expect(h.durum, 409);
      expect(h.mesaj, 'Geri çağrılan lot içeren ürünler var.');
      expect(h.uyarilar.single.lotlar, ['L-KAP-01']);
    }
    expect(jsonDecode(istekler.single.body)['tarih'], '2026-10-13');
  });

  test('doğrulama hatasında ilk alan mesajı gösterilir', () async {
    final api = apiOlustur((_) async => _json({
          'title': 'One or more validation errors occurred.',
          'errors': {'Sifre': ['Şifre gerekli.']},
        }, 400));
    await expectLater(api.giris('ali', ''), throwsA(isA<ApiHatasi>().having((h) => h.mesaj, 'mesaj', 'Şifre gerekli.')));
  });

  test('sunucuya ulaşılamazsa anlaşılır mesaj', () async {
    final api = apiOlustur((_) async => throw http.ClientException('bağlantı yok'));
    await expectLater(api.hareketler(), throwsA(isA<ApiHatasi>().having((h) => h.durum, 'durum', 0)));
  });

  test('bulunamayan lot ve seri null döner, numara adreste kodlanır', () async {
    await ayarlar.tokenKaydet('t');
    final api = apiOlustur((_) async => _json({'title': 'Bulunamadı', 'detail': 'Lot bulunamadı.'}, 404));
    expect(await api.lotIz('L/1 A'), isNull);
    expect(istekler.single.url.toString(), 'http://sunucu:5291/api/izleme/lot/L%2F1%20A');
    expect(await api.seriIz('SN-YOK'), isNull);
  });

  test('izleme ve üretim yanıtları modele dönüşür', () async {
    final seri = SeriIz.json({
      'uretimId': 1, 'seriNo': 'SN-IP-0001', 'tarih': '2026-04-02T10:00:00+03:00', 'urunKod': 'IP', 'urunAd': 'Panel',
      'parcalar': [
        {'parca': {'id': 3, 'kod': 'D', 'ad': 'Diyot', 'birim': 'adet'}, 'lot': _lot(geri: true), 'adet': 1},
      ],
      'satis': {'satisId': 1, 'tarih': '2026-05-12', 'musteriId': 1, 'musteriAd': 'Atlas', 'musteriIletisim': ''},
      'geriCagrilanLotlar': ['L-D-2601'],
    });
    expect(seri.parcalar.single.lot.geriCagrildi, isTrue);
    expect(seri.satis!.musteriAd, 'Atlas');

    final o = UretimOnizleme.json({
      'urunId': 13, 'urunKod': 'IP', 'urunAd': 'Panel', 'adet': 2,
      'parcalar': [
        {
          'parca': {'id': 3, 'kod': 'D', 'ad': 'Diyot', 'birim': 'adet'}, 'birimAdet': 1, 'gereken': 2, 'kullanilabilir': 1,
          'lotlar': [{'lotId': 1, 'lotNo': 'L-D-2601', 'adet': 1}], 'atlananLotlar': <String>[], 'eksik': 1,
        },
      ],
      'seriNolar': ['SN-IP-0007', 'SN-IP-0008'], 'eksikler': <Object>[], 'yeterli': false, 'atlananLotlar': <String>[],
    });
    expect(o.yeterli, isFalse);
    expect(o.parcalar.single.lotlar.single.lotNo, 'L-D-2601');
    expect(tarihMetni(DateTime(2026, 3, 1)), '2026-03-01');
  });

  test('API adresi sondaki / olmadan saklanır', () async {
    await ayarlar.apiAdresiKaydet(' http://192.168.1.20:5291/ ');
    expect(ayarlar.apiAdresi, 'http://192.168.1.20:5291');
  });
}
