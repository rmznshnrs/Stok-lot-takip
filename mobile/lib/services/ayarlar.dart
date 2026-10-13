import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Kalıcı ayarlar: API adresi ve tema telefonda, token güvenli depoda.
class Ayarlar extends ChangeNotifier {
  Ayarlar._(this._tercihler);

  static const varsayilanAdres = 'http://10.0.2.2:5291'; // emülatörden bilgisayara
  static const _adresAnahtari = 'api_adresi';
  static const _koyuAnahtari = 'koyu_tema';
  static const _tokenAnahtari = 'token';

  final SharedPreferences _tercihler;
  final _guvenli = const FlutterSecureStorage();
  String? _token;

  static Future<Ayarlar> yukle() async {
    final a = Ayarlar._(await SharedPreferences.getInstance());
    try {
      a._token = await a._guvenli.read(key: _tokenAnahtari);
    } catch (_) {
      // Güvenli depo okunamazsa (ör. cihaz yedeğinden geri yükleme) yeniden giriş istenir
      a._token = null;
    }
    return a;
  }

  String get apiAdresi => _tercihler.getString(_adresAnahtari) ?? varsayilanAdres;

  Future<void> apiAdresiKaydet(String adres) async {
    await _tercihler.setString(_adresAnahtari, adres.trim().replaceAll(RegExp(r'/+$'), ''));
    notifyListeners();
  }

  bool get koyuTema => _tercihler.getBool(_koyuAnahtari) ?? false;

  Future<void> koyuTemaKaydet(bool koyu) async {
    await _tercihler.setBool(_koyuAnahtari, koyu);
    notifyListeners();
  }

  String? get token => _token;
  bool get girisYapildi => _token != null;

  Future<void> tokenKaydet(String? token) async {
    _token = token;
    if (token == null) {
      await _guvenli.delete(key: _tokenAnahtari);
    } else {
      await _guvenli.write(key: _tokenAnahtari, value: token);
    }
    notifyListeners();
  }
}
