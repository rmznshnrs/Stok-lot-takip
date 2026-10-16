import 'package:flutter/material.dart';

import '../main.dart';
import '../widgets/ortak.dart';
import 'ana_kabuk.dart';

/// Giriş yapmış kullanıcının bilgileri ve şifre değiştirme.
class ProfilEkrani extends StatefulWidget {
  const ProfilEkrani({super.key});

  @override
  State<ProfilEkrani> createState() => _ProfilEkraniState();
}

class _ProfilEkraniState extends State<ProfilEkrani> {
  final _eski = TextEditingController();
  final _yeni = TextEditingController();
  final _tekrar = TextEditingController();
  bool _bekliyor = false;

  @override
  void dispose() {
    _eski.dispose();
    _yeni.dispose();
    _tekrar.dispose();
    super.dispose();
  }

  Future<void> _kaydet() async {
    if (_yeni.text != _tekrar.text) return bildir(context, 'Yeni şifreler aynı değil.', hata: true);
    setState(() {
      _bekliyor = true;
    });
    try {
      await context.api.sifreDegistir(_eski.text, _yeni.text);
      if (!mounted) return;
      bildir(context, 'Şifreniz değiştirildi.');
      _eski.clear();
      _yeni.clear();
      _tekrar.clear();
    } catch (e) {
      if (mounted) bildir(context, hataMetni(e), hata: true);
    } finally {
      if (mounted) {
        setState(() {
          _bekliyor = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final k = KullaniciBaglami.of(context).kullanici;
    final rol = k.admin ? 'Yönetici' : 'Kullanıcı';
    final harfler = '${k.ad.isEmpty ? '' : k.ad[0]}${k.soyad.isEmpty ? '' : k.soyad[0]}';
    return Scaffold(
      appBar: const GecisliBar(baslik: 'Profil'),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          Kart(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    CircleAvatar(radius: 24, child: Text(harfler, style: const TextStyle(fontWeight: FontWeight.w600))),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text('${k.ad} ${k.soyad}', style: const TextStyle(fontSize: 17, fontWeight: FontWeight.w600)),
                          Soluk(rol),
                        ],
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 8),
                _Satir(ad: 'Ad', deger: k.ad),
                _Satir(ad: 'Soyad', deger: k.soyad),
                _Satir(ad: 'Kullanıcı adı', deger: k.kullaniciAdi, mono: true),
                _Satir(ad: 'Rol', deger: rol),
                const SizedBox(height: 8),
                const Soluk('Ad, soyad veya rol değişecekse yöneticinize başvurun.', boyut: 12),
              ],
            ),
          ),
          const BolumBasligi('Şifre değiştir'),
          SifreAlani(denetleyici: _eski, etiket: 'Mevcut şifre'),
          const SizedBox(height: 14),
          SifreAlani(denetleyici: _yeni, etiket: 'Yeni şifre'),
          const SizedBox(height: 14),
          SifreAlani(denetleyici: _tekrar, etiket: 'Yeni şifre tekrar'),
          const SizedBox(height: 24),
          AnaDugme(metin: 'Şifreyi değiştir', bekliyor: _bekliyor, onPressed: _kaydet),
        ],
      ),
    );
  }
}

class _Satir extends StatelessWidget {
  const _Satir({required this.ad, required this.deger, this.mono = false});

  final String ad, deger;
  final bool mono;

  @override
  Widget build(BuildContext context) => Container(
    padding: const EdgeInsets.symmetric(vertical: 10),
    decoration: BoxDecoration(border: Border(bottom: BorderSide(color: Theme.of(context).dividerColor))),
    child: Row(
      children: [
        Expanded(child: Soluk(ad)),
        mono ? Mono(deger, kalin: true) : Text(deger, style: const TextStyle(fontWeight: FontWeight.w500)),
      ],
    ),
  );
}
