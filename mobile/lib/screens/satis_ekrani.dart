import 'dart:async';

import 'package:flutter/material.dart';

import '../main.dart';
import '../services/api.dart';
import '../services/modeller.dart';
import '../widgets/barkod.dart';
import '../widgets/ortak.dart';
import 'izlenebilirlik_ekrani.dart';

/// Müşteri, tarih, seri no okutarak ekleme; geri çağrılan lot içeren ürün onaysız satılmaz.
class SatisEkrani extends StatefulWidget {
  const SatisEkrani({super.key});

  @override
  State<SatisEkrani> createState() => _SatisEkraniState();
}

class _SatisEkraniState extends State<SatisEkrani> {
  final _musteri = TextEditingController();
  final _seri = TextEditingController();
  DateTime _tarih = DateTime.now();
  final List<Satilabilir> _sepet = [];
  List<String> _musteriler = [];
  bool _onay = false;
  bool _ekleniyor = false;
  bool _bekliyor = false;

  bool get _uyariVar => _sepet.any((k) => k.geriCagrilanLotlar.isNotEmpty);

  @override
  void initState() {
    super.initState();
    _musterileriYukle();
  }

  void _musterileriYukle() =>
      context.api.musteriler().then((m) => mounted ? setState(() => _musteriler = m) : null).catchError((_) => null);

  @override
  void dispose() {
    _musteri.dispose();
    _seri.dispose();
    super.dispose();
  }

  Future<void> _ekle([String? metin]) async {
    final seriler = (metin ?? _seri.text).split(RegExp(r'[\s,;]+')).where((s) => s.isNotEmpty).toList();
    if (seriler.isEmpty) return;
    setState(() => _ekleniyor = true);
    final hatalar = <String>[];
    for (final s in seriler) {
      if (_sepet.any((k) => k.seriNo.toUpperCase() == s.toUpperCase())) {
        hatalar.add('$s zaten listede.');
        continue;
      }
      try {
        final k = await context.api.satisKontrol(s);
        setState(() => _sepet.add(k));
      } catch (e) {
        hatalar.add(hataMetni(e));
      }
    }
    if (!mounted) return;
    setState(() => _ekleniyor = false);
    _seri.clear();
    if (hatalar.isNotEmpty) bildir(context, hatalar.join('\n'), hata: true);
  }

  Future<void> _okutVeEkle() async {
    final okunan = await barkodOku(context, baslik: 'Seri no okut');
    if (okunan != null) await _ekle(okunan);
  }

  Future<void> _tarihSec() async {
    final t = await showDatePicker(context: context, initialDate: _tarih, firstDate: DateTime(2000), lastDate: DateTime.now());
    if (t != null) setState(() => _tarih = t);
  }

  Future<void> _kaydet() async {
    if (_musteri.text.trim().isEmpty) return bildir(context, 'Müşteri adını yazın.', hata: true);
    if (_sepet.isEmpty) return bildir(context, 'En az bir seri numarası ekleyin.', hata: true);
    if (_uyariVar && !_onay) return bildir(context, 'Geri çağrılan lot içeren ürünler var; satmak için onay kutusunu işaretleyin.', hata: true);
    setState(() => _bekliyor = true);
    try {
      final s = await context.api.sat(
        musteri: _musteri.text.trim(),
        seriNolar: [for (final k in _sepet) k.seriNo],
        tarih: _tarih,
        geriCagrilanOnayi: _onay,
      );
      if (!mounted) return;
      bildir(context, '${s.adet} ürün ${s.musteri} müşterisine satıldı.${s.yeniMusteri ? ' Yeni müşteri eklendi.' : ''}');
      setState(() {
        _sepet.clear();
        _onay = false;
        _musteri.clear();
      });
      if (s.yeniMusteri) _musterileriYukle();
    } on ApiHatasi catch (h) {
      // Bu arada bir lot geri çağrıldıysa uyarılar güncellenir, yeniden onay istenir
      if (h.uyarilar.isNotEmpty) {
        setState(() {
          for (final u in h.uyarilar) {
            for (final k in _sepet.where((k) => k.seriNo == u.seriNo)) {
              k.geriCagrilanLotlar = u.lotlar;
            }
          }
          _onay = false;
        });
      }
      if (mounted) bildir(context, h.mesaj, hata: true);
    } finally {
      if (mounted) setState(() => _bekliyor = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: GecisliBar(baslik: 'Satış', eylemler: [
          IconButton(
            tooltip: 'Satış geçmişi',
            icon: const Icon(Icons.history),
            onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const SatisGecmisiSayfasi())),
          ),
        ]),
        body: ListView(padding: const EdgeInsets.all(16), children: [
          ListedenSecilebilirAlan(denetleyici: _musteri, etiket: 'Müşteri', secenekler: _musteriler),
          const SizedBox(height: 14),
          InkWell(
            onTap: _tarihSec,
            child: InputDecorator(
              decoration: const InputDecoration(labelText: 'Satış tarihi', suffixIcon: Icon(Icons.calendar_today, size: 18)),
              child: Text(tarih(_tarih)),
            ),
          ),
          const SizedBox(height: 14),
          SizedBox(
            height: 56,
            child: OutlinedButton.icon(
              onPressed: _ekleniyor ? null : _okutVeEkle,
              icon: const Icon(Icons.qr_code_scanner),
              label: const Text('Seri no okut', style: TextStyle(fontSize: 16)),
            ),
          ),
          const SizedBox(height: 10),
          TextField(
            controller: _seri,
            style: const TextStyle(fontFamily: 'IBMPlexMono'),
            decoration: InputDecoration(
              labelText: 'veya elle yazın',
              hintText: 'SN-…',
              suffixIcon: _ekleniyor
                  ? const Padding(padding: EdgeInsets.all(14), child: SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2)))
                  : IconButton(icon: const Icon(Icons.add), onPressed: _ekle),
            ),
            onSubmitted: _ekle,
          ),
          BolumBasligi('Sepet · ${_sepet.length} ürün'),
          if (_sepet.isEmpty) const BosDurum('Henüz seri numarası eklenmedi.'),
          for (final k in _sepet)
            Padding(
              padding: const EdgeInsets.only(bottom: 8),
              child: Kart(
                uyari: k.geriCagrilanLotlar.isNotEmpty,
                padding: const EdgeInsets.fromLTRB(16, 8, 4, 8),
                child: Row(children: [
                  Expanded(
                    child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Mono(k.seriNo, kalin: true),
                      if (k.geriCagrilanLotlar.isEmpty)
                        Soluk(k.urunAd)
                      else
                        Text('Geri çağrılan lot: ${k.geriCagrilanLotlar.join(', ')}',
                            style: TextStyle(color: Theme.of(context).colorScheme.error, fontSize: 13)),
                    ]),
                  ),
                  IconButton(
                    tooltip: 'Kaldır',
                    icon: const Icon(Icons.close),
                    onPressed: () => setState(() {
                      _sepet.remove(k);
                      if (!_uyariVar) _onay = false;
                    }),
                  ),
                ]),
              ),
            ),
          if (_uyariVar)
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              value: _onay,
              onChanged: (v) => setState(() => _onay = v ?? false),
              title: Text('Geri çağrılan lot içeren ürünleri yine de sat', style: TextStyle(color: Theme.of(context).colorScheme.error)),
            ),
          const SizedBox(height: 16),
          AnaDugme(metin: 'Satışı kaydet', ikon: Icons.check, bekliyor: _bekliyor, onPressed: _sepet.isEmpty ? null : _kaydet),
        ]),
      );
}

class SatisGecmisiSayfasi extends StatefulWidget {
  const SatisGecmisiSayfasi({super.key});

  @override
  State<SatisGecmisiSayfasi> createState() => _SatisGecmisiSayfasiState();
}

class _SatisGecmisiSayfasiState extends State<SatisGecmisiSayfasi> {
  late Future<List<SatisGecmisi>> _gecmis = context.api.satisGecmisi();
  String _arama = '';
  Timer? _gecikme;

  @override
  void dispose() {
    _gecikme?.cancel();
    super.dispose();
  }

  void _yenile() => setState(() => _gecmis = context.api.satisGecmisi(_arama));

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: const GecisliBar(baslik: 'Satış geçmişi'),
        body: ListView(padding: const EdgeInsets.all(16), children: [
          TextField(
            decoration: const InputDecoration(prefixIcon: Icon(Icons.search), hintText: 'Müşteri veya seri no ara'),
            onChanged: (m) {
              _gecikme?.cancel();
              _gecikme = Timer(const Duration(milliseconds: 300), () {
                _arama = m.trim();
                _yenile();
              });
            },
          ),
          const SizedBox(height: 12),
          Yukleyici<List<SatisGecmisi>>(
            future: _gecmis,
            yenile: _yenile,
            builder: (context, liste) => liste.isEmpty
                ? const BosDurum('Satış bulunamadı.')
                : Column(children: [
                    for (final s in liste)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: Kart(
                          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                            Row(children: [
                              Expanded(child: Text(s.musteriAd, style: const TextStyle(fontWeight: FontWeight.w600))),
                              Soluk(tarih(s.tarih)),
                            ]),
                            Soluk('${s.urunAd} · ${s.seriNolar.length} adet'),
                            const SizedBox(height: 6),
                            Wrap(spacing: 6, runSpacing: 6, children: [
                              for (final seri in s.seriNolar)
                                ActionChip(
                                  label: Mono(seri, boyut: 12),
                                  onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => SeriIzSayfasi(seriNo: seri))),
                                ),
                            ]),
                          ]),
                        ),
                      ),
                  ]),
          ),
        ]),
      );
}
