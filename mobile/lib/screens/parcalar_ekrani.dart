import 'dart:async';

import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/ortak.dart';
import 'stok_ekrani.dart';

class ParcalarEkrani extends StatefulWidget {
  const ParcalarEkrani({super.key});

  @override
  State<ParcalarEkrani> createState() => _ParcalarEkraniState();
}

class _ParcalarEkraniState extends State<ParcalarEkrani> {
  late Future<List<ParcaStok>> _parcalar = context.api.parcalar();
  String _arama = '';
  Timer? _gecikme;

  @override
  void dispose() {
    _gecikme?.cancel();
    super.dispose();
  }

  void _yenile() => setState(() => _parcalar = context.api.parcalar(_arama));

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: const GecisliBar(baslik: 'Parçalar'),
        floatingActionButton: FloatingActionButton.extended(
          icon: const Icon(Icons.add),
          label: const Text('Yeni parça'),
          onPressed: () async {
            final eklendi = await Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => const YeniParcaSayfasi()));
            if (eklendi == true) _yenile();
          },
        ),
        body: ListView(padding: const EdgeInsets.fromLTRB(16, 16, 16, 96), children: [
          TextField(
            decoration: const InputDecoration(prefixIcon: Icon(Icons.search), hintText: 'Kod veya ad ara'),
            onChanged: (m) {
              _gecikme?.cancel();
              _gecikme = Timer(const Duration(milliseconds: 300), () {
                _arama = m.trim();
                _yenile();
              });
            },
          ),
          const SizedBox(height: 12),
          Yukleyici<List<ParcaStok>>(
            future: _parcalar,
            yenile: _yenile,
            builder: (context, liste) => liste.isEmpty
                ? BosDurum(_arama.isEmpty ? 'Henüz parça tanımlanmadı.' : '“$_arama” için parça bulunamadı.')
                : Column(children: [
                    for (final p in liste)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: Kart(
                          uyari: p.minAltinda,
                          onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => ParcaLotlariSayfasi(parcaKod: p.kod))),
                          child: Row(children: [
                            Expanded(
                              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                Text(p.ad, style: const TextStyle(fontWeight: FontWeight.w600)),
                                Mono(p.kod, boyut: 12),
                                const SizedBox(height: 4),
                                Soluk(p.kullanildigiUrunler.isEmpty ? 'Hiçbir ürün ağacında yok' : p.kullanildigiUrunler.join(', ')),
                              ]),
                            ),
                            Column(crossAxisAlignment: CrossAxisAlignment.end, children: [
                              Text('${p.kullanilabilirStok}', style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
                              Soluk('${p.lotSayisi} lot', boyut: 12),
                            ]),
                          ]),
                        ),
                      ),
                  ]),
          ),
        ]),
      );
}

class YeniParcaSayfasi extends StatefulWidget {
  const YeniParcaSayfasi({super.key});

  @override
  State<YeniParcaSayfasi> createState() => _YeniParcaSayfasiState();
}

class _YeniParcaSayfasiState extends State<YeniParcaSayfasi> {
  final _kod = TextEditingController();
  final _ad = TextEditingController();
  final _birim = TextEditingController(text: 'adet');
  final _minStok = TextEditingController(text: '0');
  bool _bekliyor = false;

  @override
  void dispose() {
    for (final d in [_kod, _ad, _birim, _minStok]) {
      d.dispose();
    }
    super.dispose();
  }

  Future<void> _kaydet() async {
    if (_kod.text.trim().isEmpty || _ad.text.trim().isEmpty) return bildir(context, 'Kod ve ad girin.', hata: true);
    setState(() => _bekliyor = true);
    try {
      final p = await context.api.parcaOlustur(
        _kod.text.trim(),
        _ad.text.trim(),
        _birim.text.trim().isEmpty ? 'adet' : _birim.text.trim(),
        int.tryParse(_minStok.text.trim()) ?? 0,
      );
      if (!mounted) return;
      bildir(context, '${p.kod} – ${p.ad} kaydedildi.');
      Navigator.of(context).pop(true);
    } catch (e) {
      if (mounted) bildir(context, hataMetni(e), hata: true);
    } finally {
      if (mounted) setState(() => _bekliyor = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: const GecisliBar(baslik: 'Yeni parça'),
        body: ListView(padding: const EdgeInsets.all(16), children: [
          TextField(controller: _kod, style: const TextStyle(fontFamily: 'IBMPlexMono'), decoration: const InputDecoration(labelText: 'Parça kodu')),
          const SizedBox(height: 14),
          TextField(controller: _ad, decoration: const InputDecoration(labelText: 'Parça adı')),
          const SizedBox(height: 14),
          Row(children: [
            Expanded(child: TextField(controller: _birim, decoration: const InputDecoration(labelText: 'Birim'))),
            const SizedBox(width: 12),
            Expanded(
              child: TextField(controller: _minStok, keyboardType: TextInputType.number, decoration: const InputDecoration(labelText: 'Minimum stok')),
            ),
          ]),
          const SizedBox(height: 24),
          AnaDugme(metin: 'Parçayı kaydet', bekliyor: _bekliyor, onPressed: _kaydet),
        ]),
      );
}
