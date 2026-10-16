import 'dart:async';

import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../tema.dart';
import '../widgets/ortak.dart';
import 'izlenebilirlik_ekrani.dart';
import 'uretim_gecmisi_ekrani.dart';

/// Ürün seç → adet (büyük −/+) → parça kontrolü → kaydet → verilen seri numaraları.
class UretimEkrani extends StatefulWidget {
  const UretimEkrani({super.key});

  @override
  State<UretimEkrani> createState() => _UretimEkraniState();
}

class _UretimEkraniState extends State<UretimEkrani> {
  late Future<List<UrunListe>> _urunler = context.api.urunler();
  UrunListe? _urun;
  int _adet = 1;
  Future<UretimOnizleme>? _onizleme;
  Timer? _gecikme;
  List<String>? _verilenler;
  bool _bekliyor = false;

  @override
  void dispose() {
    _gecikme?.cancel();
    super.dispose();
  }

  void _onizle() {
    final u = _urun;
    if (u == null) return;
    _gecikme?.cancel();
    // +/- art arda basılınca her basışta değil, durunca kontrol et
    _gecikme = Timer(const Duration(milliseconds: 250), () {
      if (mounted) {
        setState(() {
          _onizleme = context.api.uretimOnizle(u.id, _adet);
        });
      }
    });
  }

  void _adetDegistir(int yeni) {
    setState(() {
      _adet = yeni.clamp(1, 999);
      _verilenler = null;
    });
    _onizle();
  }

  Future<void> _kaydet(UretimOnizleme o) async {
    final onay = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Üretim kaydedilsin mi?'),
        content: Text(
          '${o.adet} adet ${o.urunAd}\n${o.seriNolar.first}${o.seriNolar.length > 1 ? ' … ${o.seriNolar.last}' : ''}',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Vazgeç')),
          FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Kaydet')),
        ],
      ),
    );
    if (onay != true || !mounted) return;
    setState(() {
      _bekliyor = true;
    });
    try {
      final seriler = await context.api.uret(o.urunId, o.adet);
      if (!mounted) return;
      setState(() {
        _verilenler = seriler;
        _adet = 1;
      });
      bildir(context, '${seriler.length} adet ${o.urunAd} üretildi.');
    } catch (e) {
      if (mounted) bildir(context, hataMetni(e), hata: true);
    } finally {
      if (mounted) {
        setState(() {
          _bekliyor = false;
        });
        _onizle();
      }
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: GecisliBar(
      baslik: 'Üretim',
      eylemler: [
        IconButton(
          tooltip: 'Üretim geçmişi',
          icon: const Icon(Icons.history),
          onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => const UretimGecmisiSayfasi())),
        ),
      ],
    ),
    body: Yukleyici<List<UrunListe>>(
      future: _urunler,
      yenile: () => setState(() {
        _urunler = context.api.urunler();
      }),
      builder: (context, urunler) {
        if (urunler.isEmpty) return const BosDurum('Önce Ürünler ekranından bir ürün tanımlayın.');
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (_urun == null) ...[
              const BolumBasligi('Ürün seçin'),
              for (final u in urunler)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Kart(
                    onTap: () {
                      setState(() {
                        _urun = u;
                        _adet = 1;
                        _verilenler = null;
                      });
                      _onizle();
                    },
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(u.ad, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
                              Soluk('${u.kod} · ${u.parcaCesidi} parça çeşidi'),
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_right),
                      ],
                    ),
                  ),
                ),
            ] else
              Kart(
                padding: const EdgeInsets.fromLTRB(16, 8, 8, 8),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(_urun!.ad, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
                    ),
                    TextButton(
                      onPressed: () => setState(() {
                        _urun = null;
                        _onizleme = null;
                        _verilenler = null;
                      }),
                      child: const Text('Değiştir'),
                    ),
                  ],
                ),
              ),
            if (_urun != null) ...[
              const SizedBox(height: 16),
              Row(
                children: [
                  _AdetDugmesi(ikon: Icons.remove, onPressed: _adet > 1 ? () => _adetDegistir(_adet - 1) : null),
                  Expanded(
                    child: Column(
                      children: [
                        Text('$_adet', style: const TextStyle(fontSize: 36, fontWeight: FontWeight.w700)),
                        const Soluk('adet'),
                      ],
                    ),
                  ),
                  _AdetDugmesi(ikon: Icons.add, onPressed: _adet < 999 ? () => _adetDegistir(_adet + 1) : null),
                ],
              ),
              if (_verilenler != null) ...[const SizedBox(height: 16), _VerilenSeriler(seriler: _verilenler!)],
              const BolumBasligi('Parça kontrolü'),
              if (_onizleme == null)
                const SizedBox.shrink()
              else
                Yukleyici<UretimOnizleme>(
                  future: _onizleme,
                  yenile: _onizle,
                  builder: (context, o) => Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      for (final p in o.parcalar)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: _ParcaKontrol(p: p),
                        ),
                      if (o.atlananLotlar.isNotEmpty)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: Soluk('Geri çağrılan lotlar atlandı: ${o.atlananLotlar.join(', ')}'),
                        ),
                      const SizedBox(height: 8),
                      Kart(
                        child: Row(
                          children: [
                            const Icon(Icons.qr_code_2),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Mono(
                                o.seriNolar.length > 1 ? '${o.seriNolar.first} … ${o.seriNolar.last}' : o.seriNolar.first,
                                kalin: true,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SizedBox(height: 16),
                      if (!o.yeterli)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 8),
                          child: Text(
                            'Eksik parça var',
                            textAlign: TextAlign.center,
                            style: TextStyle(color: Theme.of(context).colorScheme.error),
                          ),
                        ),
                      AnaDugme(
                        metin: 'Üretimi kaydet',
                        ikon: Icons.save,
                        bekliyor: _bekliyor,
                        onPressed: o.yeterli ? () => _kaydet(o) : null,
                      ),
                    ],
                  ),
                ),
            ],
          ],
        );
      },
    ),
  );
}

class _AdetDugmesi extends StatelessWidget {
  const _AdetDugmesi({required this.ikon, required this.onPressed});

  final IconData ikon;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) => SizedBox.square(
    dimension: 64,
    child: OutlinedButton(
      onPressed: onPressed,
      style: OutlinedButton.styleFrom(padding: EdgeInsets.zero),
      child: Icon(ikon, size: 30),
    ),
  );
}

class _ParcaKontrol extends StatelessWidget {
  const _ParcaKontrol({required this.p});

  final ParcaIhtiyaci p;

  @override
  Widget build(BuildContext context) => Kart(
    uyari: p.eksik > 0,
    padding: const EdgeInsets.all(14),
    child: Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(p.parca.ad, style: const TextStyle(fontWeight: FontWeight.w600)),
            ),
            p.eksik > 0 ? Rozet('${p.eksik} eksik', tur: RozetTuru.uyari) : const Rozet('Yeterli', tur: RozetTuru.basari),
          ],
        ),
        const SizedBox(height: 4),
        Soluk('Gereken ${p.gereken} · kullanılabilir ${p.kullanilabilir}'),
        if (p.lotlar.isNotEmpty) ...[
          const SizedBox(height: 4),
          Mono(p.lotlar.map((l) => '${l.lotNo} × ${l.adet}').join(' · '), boyut: 12),
        ],
      ],
    ),
  );
}

class _VerilenSeriler extends StatelessWidget {
  const _VerilenSeriler({required this.seriler});

  final List<String> seriler;

  @override
  Widget build(BuildContext context) => Card(
    color: Renk.basariZemin,
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            'Üretim kaydedildi: ${seriler.length} adet',
            style: const TextStyle(fontWeight: FontWeight.w600, color: Renk.basari),
          ),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final s in seriler)
                ActionChip(
                  label: Mono(s, boyut: 13),
                  onPressed: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => SeriIzSayfasi(seriNo: s))),
                ),
            ],
          ),
        ],
      ),
    ),
  );
}
