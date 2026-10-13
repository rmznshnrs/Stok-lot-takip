import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../main.dart';
import '../tema.dart';
import '../widgets/ortak.dart';
import 'api_adresi_ekrani.dart';

class GirisEkrani extends StatefulWidget {
  const GirisEkrani({super.key});

  @override
  State<GirisEkrani> createState() => _GirisEkraniState();
}

class _GirisEkraniState extends State<GirisEkrani> {
  final _kullaniciAdi = TextEditingController();
  final _sifre = TextEditingController();
  String? _hata;
  bool _bekliyor = false;

  @override
  void dispose() {
    _kullaniciAdi.dispose();
    _sifre.dispose();
    super.dispose();
  }

  Future<void> _giris() async {
    if (_kullaniciAdi.text.trim().isEmpty || _sifre.text.isEmpty) {
      setState(() {
        _hata = 'Kullanıcı adı ve şifre girin.';
      });
      return;
    }
    setState(() {
      _hata = null;
      _bekliyor = true;
    });
    try {
      // Başarılı girişte token kaydedilir, uygulama ana ekrana geçer
      await context.api.giris(_kullaniciAdi.text.trim(), _sifre.text);
    } catch (e) {
      if (mounted) {
        setState(() {
          _hata = hataMetni(e);
        });
      }
    } finally {
      if (mounted) {
        setState(() {
          _bekliyor = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) => AnnotatedRegion<SystemUiOverlayStyle>(
    value: SystemUiOverlayStyle.light,
    child: Scaffold(
      body: DecoratedBox(
        decoration: const BoxDecoration(gradient: Renk.gecis),
        child: SafeArea(
          child: Center(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(24),
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(24),
                  child: AutofillGroup(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        const Text(
                          'MKC BİLİŞİM',
                          style: TextStyle(fontFamily: monoAile, fontSize: 18, fontWeight: FontWeight.w600, letterSpacing: 1),
                        ),
                        const SizedBox(height: 24),
                        TextField(
                          controller: _kullaniciAdi,
                          decoration: const InputDecoration(labelText: 'Kullanıcı adı'),
                          autofillHints: const [AutofillHints.username],
                          textInputAction: TextInputAction.next,
                        ),
                        const SizedBox(height: 14),
                        TextField(
                          controller: _sifre,
                          decoration: const InputDecoration(labelText: 'Şifre'),
                          obscureText: true,
                          autofillHints: const [AutofillHints.password],
                          onSubmitted: (_) => _giris(),
                        ),
                        if (_hata != null) ...[
                          const SizedBox(height: 14),
                          Text(_hata!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                        ],
                        const SizedBox(height: 20),
                        AnaDugme(metin: 'Giriş yap', ikon: Icons.login, bekliyor: _bekliyor, onPressed: _giris),
                        const SizedBox(height: 8),
                        TextButton.icon(
                          onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const ApiAdresiEkrani())),
                          icon: const Icon(Icons.dns_outlined, size: 18),
                          label: Text('Sunucu: ${context.ayarlar.apiAdresi}', overflow: TextOverflow.ellipsis),
                        ),
                      ],
                    ),
                  ),
                ),
              ),
            ),
          ),
        ),
      ),
    ),
  );
}
