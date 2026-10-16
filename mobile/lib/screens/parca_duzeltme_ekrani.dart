import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/ortak.dart';

/// Yalnız yönetici: sayımda farklı çıkan ya da bozulan parçalar için bir lotun kalan adedini düzeltir.
class AdetDuzeltSayfasi extends StatefulWidget {
  const AdetDuzeltSayfasi({super.key, required this.parcaKod});

  final String parcaKod;

  @override
  State<AdetDuzeltSayfasi> createState() => _AdetDuzeltSayfasiState();
}

class _AdetDuzeltSayfasiState extends State<AdetDuzeltSayfasi> {
  late Future<List<StokGrubu>> _gruplar = _yukle();
  final _miktar = TextEditingController();
  final _aciklama = TextEditingController();
  int? _lotId;
  String _tur = 'SayimFarki';
  bool _bekliyor = false;
  bool _degisti = false;

  Future<List<StokGrubu>> _yukle() => context.api.stok(parca: widget.parcaKod, bitenler: true);

  @override
  void dispose() {
    _miktar.dispose();
    _aciklama.dispose();
    super.dispose();
  }

  Future<void> _kaydet(Lot lot) async {
    final miktar = int.tryParse(_miktar.text.trim().replaceAll('−', '-'));
    if (miktar == null || miktar == 0) {
      return bildir(context, 'Miktar sıfırdan farklı bir tam sayı olmalı; azaltmak için eksi yazın.', hata: true);
    }
    if (_aciklama.text.trim().isEmpty) return bildir(context, 'Açıklama yazın.', hata: true);
    final onay = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        title: const Text('Düzeltme kaydedilsin mi?'),
        content: Text('${lot.lotNo}: kalan ${lot.kalanAdet} → ${lot.kalanAdet + miktar}'),
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
      final yeni = await context.api.stokDuzelt(lotId: lot.id, miktar: miktar, tur: _tur, aciklama: _aciklama.text.trim());
      if (!mounted) return;
      bildir(context, '${lot.lotNo} düzeltildi. Yeni kalan: $yeni.');
      _miktar.clear();
      _aciklama.clear();
      _degisti = true;
      setState(() {
        _gruplar = _yukle();
      });
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
  Widget build(BuildContext context) => PopScope(
    canPop: false,
    // Düzeltme yapıldıysa önceki sayfa listeyi yenilesin
    onPopInvokedWithResult: (bitti, _) {
      if (!bitti) Navigator.of(context).pop(_degisti);
    },
    child: Scaffold(
      appBar: const GecisliBar(baslik: 'Adet düzelt'),
      body: Yukleyici<List<StokGrubu>>(
        future: _gruplar,
        yenile: () => setState(() {
          _gruplar = _yukle();
        }),
        builder: (context, gruplar) {
          if (gruplar.isEmpty || gruplar.first.lotlar.isEmpty) {
            return const BosDurum('Bu parçanın hiç lotu yok; önce mal girişi yapın.');
          }
          final g = gruplar.first;
          final lot = g.lotlar.where((l) => l.id == _lotId).firstOrNull;
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Text(g.parca.ad, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
              const SizedBox(height: 4),
              const Soluk('Sayımda farklı çıkan ya da bozulan parçalar için lotu seçin; azaltmak için eksi, artırmak için artı adet girin.'),
              const BolumBasligi('Lot'),
              for (final l in g.lotlar)
                Padding(
                  padding: const EdgeInsets.only(bottom: 6),
                  child: Kart(
                    uyari: l.id == _lotId,
                    padding: const EdgeInsets.all(14),
                    onTap: () => setState(() {
                      _lotId = l.id;
                    }),
                    child: Row(
                      children: [
                        Icon(l.id == _lotId ? Icons.radio_button_checked : Icons.radio_button_unchecked, size: 20),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Mono(l.lotNo, kalin: true),
                              Soluk('${tarih(l.siparisTarihi)}${l.geriCagrildi ? ' · geri çağrıldı' : ''}', boyut: 12),
                            ],
                          ),
                        ),
                        Text('${l.kalanAdet}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                        Soluk(' / ${l.girisAdet}', boyut: 12),
                      ],
                    ),
                  ),
                ),
              const SizedBox(height: 12),
              TextField(
                controller: _miktar,
                enabled: lot != null,
                keyboardType: const TextInputType.numberWithOptions(signed: true),
                decoration: const InputDecoration(labelText: 'Miktar'),
              ),
              const SizedBox(height: 14),
              SegmentedButton<String>(
                segments: const [
                  ButtonSegment(value: 'SayimFarki', label: Text('Sayım farkı')),
                  ButtonSegment(value: 'Fire', label: Text('Fire')),
                  ButtonSegment(value: 'Diger', label: Text('Diğer')),
                ],
                selected: {_tur},
                onSelectionChanged: lot == null
                    ? null
                    : (s) => setState(() {
                        _tur = s.first;
                      }),
              ),
              const SizedBox(height: 14),
              TextField(
                controller: _aciklama,
                enabled: lot != null,
                maxLength: 500,
                decoration: const InputDecoration(labelText: 'Açıklama'),
              ),
              const SizedBox(height: 8),
              AnaDugme(
                metin: 'Düzeltmeyi kaydet',
                bekliyor: _bekliyor,
                onPressed: lot == null ? null : () => _kaydet(lot),
              ),
            ],
          );
        },
      ),
    ),
  );
}
