import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;

import '../main.dart';
import '../services/ayarlar.dart';
import '../widgets/ortak.dart';

/// API (sunucu) adresi: emülatörde 10.0.2.2, telefonda bilgisayarın veya sunucunun adresi.
class ApiAdresiEkrani extends StatefulWidget {
  const ApiAdresiEkrani({super.key});

  @override
  State<ApiAdresiEkrani> createState() => _ApiAdresiEkraniState();
}

class _ApiAdresiEkraniState extends State<ApiAdresiEkrani> {
  late final _adres = TextEditingController(text: context.ayarlar.apiAdresi);
  String? _sonuc;
  bool _bekliyor = false;

  @override
  void dispose() {
    _adres.dispose();
    super.dispose();
  }

  String? _gecerli(String a) {
    final u = Uri.tryParse(a);
    if (u == null || !(u.isScheme('http') || u.isScheme('https')) || u.host.isEmpty) {
      return 'Adres http:// veya https:// ile başlamalı (ör. http://192.168.1.20:5291).';
    }
    return null;
  }

  Future<void> _dene() async {
    final adres = _adres.text.trim().replaceAll(RegExp(r'/+$'), '');
    final hata = _gecerli(adres);
    if (hata != null) return setState(() => _sonuc = hata);
    setState(() {
      _bekliyor = true;
      _sonuc = null;
    });
    try {
      // Giriş ucu her zaman açıktır; boş istek 400 döner ama sunucunun ayakta olduğunu gösterir
      final y = await http
          .post(Uri.parse('$adres/api/kimlik/giris'), headers: {'Content-Type': 'application/json'}, body: '{}')
          .timeout(const Duration(seconds: 8));
      setState(() => _sonuc = y.statusCode < 500 ? 'Sunucuya ulaşıldı.' : 'Sunucu hata verdi (${y.statusCode}).');
    } catch (_) {
      setState(() => _sonuc = 'Sunucuya ulaşılamadı.');
    } finally {
      if (mounted) setState(() => _bekliyor = false);
    }
  }

  Future<void> _kaydet() async {
    final adres = _adres.text.trim();
    final hata = _gecerli(adres);
    if (hata != null) return setState(() => _sonuc = hata);
    await context.ayarlar.apiAdresiKaydet(adres);
    if (mounted) Navigator.of(context).pop();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: const GecisliBar(baslik: 'Sunucu adresi'),
        body: ListView(padding: const EdgeInsets.all(16), children: [
          TextField(
            controller: _adres,
            keyboardType: TextInputType.url,
            decoration: const InputDecoration(labelText: 'API adresi', hintText: 'http://192.168.1.20:5291'),
          ),
          const SizedBox(height: 8),
          Wrap(spacing: 8, children: [
            ActionChip(label: const Text('Emülatör'), onPressed: () => _adres.text = Ayarlar.varsayilanAdres),
          ]),
          if (_sonuc != null) Padding(padding: const EdgeInsets.only(top: 12), child: Text(_sonuc!)),
          const SizedBox(height: 20),
          OutlinedButton.icon(
            onPressed: _bekliyor ? null : _dene,
            icon: const Icon(Icons.wifi_tethering),
            label: const Text('Bağlantıyı dene'),
          ),
          const SizedBox(height: 12),
          AnaDugme(metin: 'Kaydet', onPressed: _kaydet),
        ]),
      );
}
