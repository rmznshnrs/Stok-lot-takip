import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../tema.dart';
import '../widgets/ortak.dart';
import 'ana_kabuk.dart';
import 'izlenebilirlik_ekrani.dart';

/// Ürünün depodaki seri numaraları. Yönetici seçtiklerini açıklamayla hurdaya / kayba ayırır;
/// ayrılan ürün satılamaz.
class UrunDepoSayfasi extends StatefulWidget {
  const UrunDepoSayfasi({super.key, required this.urunId, required this.urunAd});

  final int urunId;
  final String urunAd;

  @override
  State<UrunDepoSayfasi> createState() => _UrunDepoSayfasiState();
}

class _UrunDepoSayfasiState extends State<UrunDepoSayfasi> {
  late Future<List<UretimGecmisi>> _liste = context.api.uretimGecmisi(urunId: widget.urunId);
  String _arama = '';
  final Set<String> _secili = {};

  void _yenile() => setState(() {
    _liste = context.api.uretimGecmisi(urunId: widget.urunId);
  });

  Future<void> _ayir() async {
    final sonuc = await showModalBottomSheet<({String tur, String aciklama})>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => _AyirmaFormu(adet: _secili.length),
    );
    if (sonuc == null || !mounted) return;
    try {
      final ayrilan = await context.api.hurdayaAyir(_secili.toList()..sort(), sonuc.tur, sonuc.aciklama);
      if (!mounted) return;
      bildir(context, '${ayrilan.length} ürün ${sonuc.tur == 'Kayip' ? 'kayba' : 'hurdaya'} ayrıldı.');
      _secili.clear();
      _yenile();
    } catch (e) {
      if (mounted) bildir(context, hataMetni(e), hata: true);
    }
  }

  @override
  Widget build(BuildContext context) {
    final admin = KullaniciBaglami.of(context).kullanici.admin;
    return Scaffold(
      appBar: GecisliBar(baslik: widget.urunAd),
      bottomNavigationBar: admin && _secili.isNotEmpty
          ? SafeArea(
              child: Padding(
                padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
                child: AnaDugme(metin: '${_secili.length} ürünü ayır', ikon: Icons.delete_sweep_outlined, onPressed: _ayir),
              ),
            )
          : null,
      body: Yukleyici<List<UretimGecmisi>>(
        future: _liste,
        yenile: _yenile,
        builder: (context, liste) {
          final depoda = liste.where((u) => u.depoda).toList();
          final ayrilan = liste.where((u) => u.hurda != null).length;
          final aranan = _arama.trim().toUpperCase();
          final gorunen = depoda.where((u) => u.seriNo.toUpperCase().contains(aranan)).toList();
          final hepsiSecili = gorunen.isNotEmpty && gorunen.every((u) => _secili.contains(u.seriNo));
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Soluk('Depoda ${depoda.length} ürün${ayrilan > 0 ? ' · $ayrilan hurda / kayıp' : ''}'),
              const SizedBox(height: 12),
              TextField(
                style: const TextStyle(fontFamily: monoAile),
                decoration: const InputDecoration(prefixIcon: Icon(Icons.search)),
                onChanged: (m) => setState(() {
                  _arama = m;
                }),
              ),
              const SizedBox(height: 8),
              if (depoda.isEmpty) const BosDurum('Depoda bu üründen seri no yok.'),
              if (depoda.isNotEmpty && gorunen.isEmpty) const BosDurum('Aramaya uyan seri no yok.'),
              if (admin && gorunen.length > 1)
                CheckboxListTile(
                  contentPadding: const EdgeInsets.symmetric(horizontal: 4),
                  controlAffinity: ListTileControlAffinity.leading,
                  title: Soluk('Görünenlerin hepsi · ${gorunen.length}', boyut: 14),
                  value: hepsiSecili,
                  onChanged: (_) => setState(() {
                    final numaralar = gorunen.map((u) => u.seriNo);
                    hepsiSecili ? _secili.removeAll(numaralar) : _secili.addAll(numaralar);
                  }),
                ),
              for (final u in gorunen)
                Padding(
                  padding: const EdgeInsets.only(bottom: 6),
                  child: admin
                      ? Kart(
                          uyari: _secili.contains(u.seriNo),
                          padding: EdgeInsets.zero,
                          child: CheckboxListTile(
                            controlAffinity: ListTileControlAffinity.leading,
                            title: Mono(u.seriNo, kalin: true),
                            subtitle: Soluk(tarihSaat(u.tarih), boyut: 12),
                            value: _secili.contains(u.seriNo),
                            onChanged: (_) => setState(() {
                              _secili.contains(u.seriNo) ? _secili.remove(u.seriNo) : _secili.add(u.seriNo);
                            }),
                          ),
                        )
                      : Kart(
                          onTap: () =>
                              Navigator.of(context).push(MaterialPageRoute(builder: (_) => SeriIzSayfasi(seriNo: u.seriNo))),
                          child: Row(
                            children: [
                              Expanded(child: Mono(u.seriNo, kalin: true)),
                              Soluk(tarihSaat(u.tarih), boyut: 12),
                            ],
                          ),
                        ),
                ),
              const SizedBox(height: 80),
            ],
          );
        },
      ),
    );
  }
}

/// Tür (hurda / kayıp) ve zorunlu açıklama.
class _AyirmaFormu extends StatefulWidget {
  const _AyirmaFormu({required this.adet});

  final int adet;

  @override
  State<_AyirmaFormu> createState() => _AyirmaFormuState();
}

class _AyirmaFormuState extends State<_AyirmaFormu> {
  String _tur = 'Hurda';
  final _aciklama = TextEditingController();

  @override
  void dispose() {
    _aciklama.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Padding(
    padding: EdgeInsets.fromLTRB(16, 0, 16, 16 + MediaQuery.of(context).viewInsets.bottom),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text('${widget.adet} ürünü ayır', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
        const SizedBox(height: 4),
        const Soluk('Ayrılan ürünler artık satılamaz.'),
        const SizedBox(height: 16),
        SegmentedButton<String>(
          segments: const [
            ButtonSegment(value: 'Hurda', label: Text('Hurda')),
            ButtonSegment(value: 'Kayip', label: Text('Kayıp')),
          ],
          selected: {_tur},
          onSelectionChanged: (s) => setState(() {
            _tur = s.first;
          }),
        ),
        const SizedBox(height: 14),
        TextField(
          controller: _aciklama,
          maxLength: 500,
          decoration: const InputDecoration(labelText: 'Açıklama'),
          onChanged: (_) => setState(() {}),
        ),
        const SizedBox(height: 8),
        AnaDugme(
          metin: 'Ayır',
          ikon: Icons.delete_sweep_outlined,
          onPressed: _aciklama.text.trim().isEmpty
              ? null
              : () => Navigator.pop(context, (tur: _tur, aciklama: _aciklama.text.trim())),
        ),
      ],
    ),
  );
}
