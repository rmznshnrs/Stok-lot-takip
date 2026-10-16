import 'dart:async';
import 'dart:convert';

import 'package:http/http.dart' as http;

import 'ayarlar.dart';
import 'modeller.dart';

/// API'nin ProblemDetails yanıtı. Mesaj kullanıcıya gösterilecek Türkçe metindir.
class ApiHatasi implements Exception {
  ApiHatasi(this.durum, this.mesaj, {this.eksikler = const [], this.uyarilar = const []});

  final int durum;
  final String mesaj;
  final List<Map<String, dynamic>> eksikler;
  final List<SatisUyarisi> uyarilar;

  @override
  String toString() => mesaj;
}

/// Tüm API çağrıları. Token her isteğe "Authorization: Bearer" olarak eklenir;
/// 401 gelirse token silinir ve uygulama giriş ekranına döner.
class Api {
  Api(this.ayarlar, {http.Client? istemci}) : _istemci = istemci ?? http.Client();

  final Ayarlar ayarlar;
  final http.Client _istemci;
  static const _zamanAsimi = Duration(seconds: 20);

  Uri _adres(String yol, [Map<String, Object?>? sorgu]) {
    final temiz = <String, String>{
      for (final e in (sorgu ?? {}).entries)
        if (e.value != null && e.value.toString().isNotEmpty) e.key: e.value.toString(),
    };
    return Uri.parse('${ayarlar.apiAdresi}/api/$yol').replace(queryParameters: temiz.isEmpty ? null : temiz);
  }

  Future<dynamic> _istek(
    String yontem,
    String yol, {
    Map<String, Object?>? sorgu,
    Object? govde,
    bool oturumGerekli = true,
  }) async {
    final basliklar = <String, String>{'Accept': 'application/json'};
    if (govde != null) basliklar['Content-Type'] = 'application/json';
    if (oturumGerekli && ayarlar.token != null) basliklar['Authorization'] = 'Bearer ${ayarlar.token}';

    final istek = http.Request(yontem, _adres(yol, sorgu))..headers.addAll(basliklar);
    if (govde != null) istek.body = jsonEncode(govde);

    http.Response yanit;
    try {
      yanit = await http.Response.fromStream(await _istemci.send(istek).timeout(_zamanAsimi));
    } on TimeoutException {
      throw ApiHatasi(0, 'Sunucu yanıt vermedi. API adresini ve bağlantıyı kontrol edin.');
    } catch (_) {
      throw ApiHatasi(0, 'Sunucuya ulaşılamadı. API adresini (${ayarlar.apiAdresi}) kontrol edin.');
    }

    if (yanit.statusCode == 401 && oturumGerekli) {
      await ayarlar.tokenKaydet(null);
      throw ApiHatasi(401, 'Oturumunuz sona erdi. Lütfen yeniden giriş yapın.');
    }
    final metin = utf8.decode(yanit.bodyBytes);
    if (yanit.statusCode >= 400) throw _hata(yanit.statusCode, metin);
    return metin.isEmpty ? null : jsonDecode(metin);
  }

  ApiHatasi _hata(int durum, String metin) {
    try {
      final p = jsonDecode(metin) as Map<String, dynamic>;
      final alanlar = (p['errors'] as Map<String, dynamic>?)?.values.expand((v) => v as List).cast<String>();
      final mesaj = p['detail'] as String? ?? alanlar?.firstOrNull ?? p['title'] as String? ?? 'İstek başarısız ($durum).';
      return ApiHatasi(
        durum,
        mesaj,
        eksikler: ((p['eksikler'] as List?) ?? []).cast<Map<String, dynamic>>(),
        uyarilar: ((p['uyarilar'] as List?) ?? []).map((u) => SatisUyarisi.json(u)).toList(),
      );
    } catch (_) {
      return ApiHatasi(durum, 'İstek başarısız ($durum).');
    }
  }

  Future<dynamic> _al(String yol, [Map<String, Object?>? sorgu]) => _istek('GET', yol, sorgu: sorgu);
  Future<dynamic> _gonder(String yol, [Object? govde]) => _istek('POST', yol, govde: govde ?? {});
  Future<dynamic> _guncelle(String yol, Object govde) => _istek('PUT', yol, govde: govde);

  static List<T> _liste<T>(dynamic j, T Function(Map<String, dynamic>) donustur) =>
      (j as List).map((e) => donustur(e as Map<String, dynamic>)).toList();

  // --- Oturum ----------------------------------------------------------------

  Future<Kullanici> giris(String kullaniciAdi, String sifre) async {
    final j = await _istek(
      'POST',
      'kimlik/giris',
      govde: {'kullaniciAdi': kullaniciAdi, 'sifre': sifre},
      oturumGerekli: false,
    ) as Map<String, dynamic>;
    await ayarlar.tokenKaydet(j['token'] as String);
    return Kullanici.json(j['kullanici']);
  }

  Future<Kullanici> ben() async => Kullanici.json(await _al('kimlik/ben'));

  Future<void> sifreDegistir(String eski, String yeni) => _gonder('kimlik/sifre', {'eskiSifre': eski, 'yeniSifre': yeni});

  // --- Tanımlar --------------------------------------------------------------

  Future<List<ParcaStok>> parcalar([String? arama]) async => _liste(await _al('parcalar', {'arama': arama}), ParcaStok.json);

  Future<ParcaOzet> parcaBul(String metin) async => ParcaOzet.json(await _al('parcalar/bul', {'metin': metin}));

  Future<ParcaOzet> parcaOlustur(String kod, String ad, String birim, int minStok) async =>
      ParcaOzet.json(await _gonder('parcalar', {'kod': kod, 'ad': ad, 'birim': birim, 'minStok': minStok}));

  Future<List<UrunListe>> urunler([String? arama]) async => _liste(await _al('urunler', {'arama': arama}), UrunListe.json);

  Future<UrunDetay> urun(int id) async => UrunDetay.json(await _al('urunler/$id'));

  Future<UrunDetay> urunOlustur(String kod, String ad, String seriOneki) async =>
      UrunDetay.json(await _gonder('urunler', {'kod': kod, 'ad': ad, 'seriOneki': seriOneki}));

  Future<UrunDetay> urunGuncelle(int id, String kod, String ad, String seriOneki) async =>
      UrunDetay.json(await _guncelle('urunler/$id', {'kod': kod, 'ad': ad, 'seriOneki': seriOneki}));

  Future<UrunDetay> agacKaydet(int id, List<({int parcaId, int adet})> satirlar) async => UrunDetay.json(
    await _guncelle('urunler/$id/agac', [
      for (final s in satirlar) {'parcaId': s.parcaId, 'adet': s.adet},
    ]),
  );

  Future<List<String>> tedarikciler() async => [for (final t in await _al('tedarikciler') as List) t['ad'] as String];

  Future<List<String>> musteriler() async => [for (final m in await _al('musteriler') as List) m['ad'] as String];

  // --- Stok ------------------------------------------------------------------

  Future<List<StokGrubu>> stok({String? arama, String? parca, bool bitenler = false}) async =>
      _liste(await _al('stok', {'arama': arama, 'parca': parca, 'bitenler': bitenler ? 'true' : null}), StokGrubu.json);

  Future<({Lot lot, bool yeniTedarikci})> malGirisi({
    required int parcaId,
    required String tedarikci,
    required String lotNo,
    required int adet,
    required DateTime siparisTarihi,
    String siparisNo = '',
  }) async {
    final j = await _gonder('stok/giris', {
      'parcaId': parcaId,
      'tedarikciAdi': tedarikci,
      'lotNo': lotNo,
      'adet': adet,
      'siparisTarihi': tarihMetni(siparisTarihi),
      'siparisNo': siparisNo,
    }) as Map<String, dynamic>;
    return (lot: Lot.json(j['lot']), yeniTedarikci: j['yeniTedarikci'] as bool);
  }

  /// Yalnız yönetici: lotun kalan adedini artırır (+) veya azaltır (−). Tur: SayimFarki, Fire, Diger. Yeni kalanı döner.
  Future<int> stokDuzelt({required int lotId, required int miktar, required String tur, required String aciklama}) async {
    final j = await _gonder('stok/duzeltme', {'stokLotuId': lotId, 'miktar': miktar, 'tur': tur, 'aciklama': aciklama})
        as Map<String, dynamic>;
    return j['yeniKalanAdet'] as int;
  }

  // --- Üretim ----------------------------------------------------------------

  Future<UretimOnizleme> uretimOnizle(int urunId, int adet) async =>
      UretimOnizleme.json(await _al('uretim/onizleme', {'urunId': urunId, 'adet': adet}));

  Future<List<String>> uret(int urunId, int adet) async {
    final j = await _gonder('uretim', {'urunId': urunId, 'adet': adet}) as Map<String, dynamic>;
    return (j['seriNolar'] as List).cast<String>();
  }

  /// Seri no başına satır, yeniden eskiye. Arama seri no, ürün veya müşteride.
  Future<List<UretimGecmisi>> uretimGecmisi({String? arama, int? urunId}) async =>
      _liste(await _al('uretim/gecmis', {'arama': arama, 'urunId': urunId}), UretimGecmisi.json);

  /// Yalnız yönetici: depodaki seri numaralarını hurdaya ("Hurda") veya kayba ("Kayip") ayırır.
  Future<List<String>> hurdayaAyir(List<String> seriNolar, String tur, String aciklama) async {
    final j = await _gonder('uretim/hurda', {'seriNolar': seriNolar, 'tur': tur, 'aciklama': aciklama}) as Map<String, dynamic>;
    return (j['seriNolar'] as List).cast<String>();
  }

  // --- Satış -----------------------------------------------------------------

  Future<Satilabilir> satisKontrol(String seriNo) async => Satilabilir.json(await _al('satis/kontrol', {'seriNo': seriNo}));

  Future<({String musteri, bool yeniMusteri, int adet})> sat({
    required String musteri,
    required List<String> seriNolar,
    required DateTime tarih,
    required bool geriCagrilanOnayi,
  }) async {
    final j = await _gonder('satis', {
      'musteriAdi': musteri,
      'seriNolar': seriNolar,
      'tarih': tarihMetni(tarih),
      'geriCagrilanOnayi': geriCagrilanOnayi,
    }) as Map<String, dynamic>;
    return (musteri: j['musteriAd'] as String, yeniMusteri: j['yeniMusteri'] as bool, adet: (j['seriNolar'] as List).length);
  }

  Future<List<SatisGecmisi>> satisGecmisi([String? arama]) async =>
      _liste(await _al('satis/gecmis', {'arama': arama}), SatisGecmisi.json);

  // --- İzlenebilirlik --------------------------------------------------------

  Future<LotIz?> lotIz(String lotNo) async {
    try {
      return LotIz.json(await _al('izleme/lot/${Uri.encodeComponent(lotNo)}'));
    } on ApiHatasi catch (h) {
      if (h.durum == 404) return null;
      rethrow;
    }
  }

  Future<SeriIz?> seriIz(String seriNo) async {
    try {
      return SeriIz.json(await _al('izleme/seri/${Uri.encodeComponent(seriNo)}'));
    } on ApiHatasi catch (h) {
      if (h.durum == 404) return null;
      rethrow;
    }
  }

  Future<NumaraArama> ara(String q) async => NumaraArama.json(await _al('izleme/ara', {'q': q}));

  Future<List<Hareket>> hareketler([int adet = 8]) async => _liste(await _al('izleme/hareketler', {'adet': adet}), Hareket.json);

  Future<Lot> geriCagir(String lotNo, bool geriCagir) async =>
      Lot.json(await _gonder('izleme/lot/${Uri.encodeComponent(lotNo)}/geri-cagir', {'geriCagir': geriCagir}));
}
