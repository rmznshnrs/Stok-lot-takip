import 'package:flutter/material.dart';

import '../main.dart';
import '../widgets/ortak.dart';

class SifreEkrani extends StatefulWidget {
  const SifreEkrani({super.key});

  @override
  State<SifreEkrani> createState() => _SifreEkraniState();
}

class _SifreEkraniState extends State<SifreEkrani> {
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
      Navigator.of(context).pop();
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
  Widget build(BuildContext context) => Scaffold(
    appBar: const GecisliBar(baslik: 'Şifre değiştir'),
    body: ListView(
      padding: const EdgeInsets.all(16),
      children: [
        TextField(
          controller: _eski,
          obscureText: true,
          decoration: const InputDecoration(labelText: 'Mevcut şifre'),
        ),
        const SizedBox(height: 14),
        TextField(
          controller: _yeni,
          obscureText: true,
          decoration: const InputDecoration(labelText: 'Yeni şifre'),
        ),
        const SizedBox(height: 14),
        TextField(
          controller: _tekrar,
          obscureText: true,
          decoration: const InputDecoration(labelText: 'Yeni şifre tekrar'),
        ),
        const SizedBox(height: 24),
        AnaDugme(metin: 'Şifreyi değiştir', bekliyor: _bekliyor, onPressed: _kaydet),
      ],
    ),
  );
}
