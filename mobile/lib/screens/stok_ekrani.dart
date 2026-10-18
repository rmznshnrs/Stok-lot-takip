import 'dart:async';

import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/ortak.dart';
import 'izlenebilirlik_ekrani.dart';
import 'mal_girisi_ekrani.dart';
import 'ana_kabuk.dart';
import 'parca_duzeltme_ekrani.dart';

Future<bool?> malGirisiAc(BuildContext context) =>
    Navigator.of(context).push<bool>(MaterialPageRoute(builder: (_) => const MalGirisiEkrani()));

/// Parça kartları: kullanılabilir stok, sıradaki lot, minimum uyarısı.
class StokEkrani extends StatefulWidget {
  const StokEkrani({super.key});

  @override
  State<StokEkrani> createState() => _StokEkraniState();
}

class _StokEkraniState extends State<StokEkrani> {
  late Future<List<StokGrubu>> _gruplar = context.api.stok();
  String _arama = '';
  Timer? _gecikme;

  @override
  void dispose() {
    _gecikme?.cancel();
    super.dispose();
  }

  Future<void> _yenile() async {
    final f = context.api.stok(arama: _arama);
    setState(() {
      _gruplar = f;
    });
    await f.catchError((_) => <StokGrubu>[]);
  }

  void _aramaDegisti(String metin) {
    _gecikme?.cancel();
    _gecikme = Timer(const Duration(milliseconds: 300), () {
      _arama = metin.trim();
      _yenile();
    });
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: const GecisliBar(baslik: 'Stok'),
    floatingActionButton: FloatingActionButton.extended(
      onPressed: () async {
        if (await malGirisiAc(context) == true) _yenile();
      },
      icon: const Icon(Icons.add),
      label: const Text('Mal girişi'),
    ),
    body: RefreshIndicator(
      onRefresh: _yenile,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
        children: [
          TextField(
            decoration: const InputDecoration(prefixIcon: Icon(Icons.search)),
            onChanged: _aramaDegisti,
          ),
          const SizedBox(height: 12),
          Yukleyici<List<StokGrubu>>(
            future: _gruplar,
            yenile: _yenile,
            builder: (context, gruplar) => gruplar.isEmpty
                ? BosDurum(_arama.isEmpty ? 'Henüz parça tanımlanmadı.' : '“$_arama” için sonuç yok.')
                : Column(
                    children: [
                      for (final g in gruplar)
                        Padding(
                          padding: const EdgeInsets.only(bottom: 10),
                          child: _ParcaKarti(grup: g, yenile: _yenile),
                        ),
                    ],
                  ),
          ),
        ],
      ),
    ),
  );
}

class _ParcaKarti extends StatelessWidget {
  const _ParcaKarti({required this.grup, required this.yenile});

  final StokGrubu grup;
  final VoidCallback yenile;

  @override
  Widget build(BuildContext context) {
    final p = grup.parca;
    final renk = p.minAltinda ? Theme.of(context).colorScheme.error : Theme.of(context).colorScheme.primary;
    return Kart(
      uyari: p.minAltinda,
      onTap: () async {
        await Navigator.of(context).push(MaterialPageRoute(builder: (_) => ParcaLotlariSayfasi(parcaKod: p.kod)));
        yenile();
      },
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(p.ad, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600)),
                Mono(p.kod, boyut: 12),
                const SizedBox(height: 6),
                if (grup.siradaki != null)
                  Row(
                    children: [
                      const Soluk('Sıradaki: '),
                      Flexible(child: Mono(grup.siradaki!.lotNo, boyut: 12)),
                    ],
                  )
                else
                  const Soluk('Kullanılabilir lot yok'),
                if (p.minAltinda)
                  Padding(
                    padding: const EdgeInsets.only(top: 4),
                    child: Rozet('Min ${p.minStok} altında', tur: RozetTuru.uyari),
                  ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                '${p.kullanilabilirStok}',
                style: TextStyle(fontSize: 24, fontWeight: FontWeight.w700, color: renk),
              ),
              Soluk(p.birim, boyut: 12),
              if (p.toplamStok != p.kullanilabilirStok) Soluk('toplam ${p.toplamStok}', boyut: 11),
            ],
          ),
          const Icon(Icons.chevron_right),
        ],
      ),
    );
  }
}

/// Bir parçanın lotları (önce kullanılacak üstte). Lota dokununca izlenebilirlik açılır.
class ParcaLotlariSayfasi extends StatefulWidget {
  const ParcaLotlariSayfasi({super.key, required this.parcaKod});

  final String parcaKod;

  @override
  State<ParcaLotlariSayfasi> createState() => _ParcaLotlariSayfasiState();
}

class _ParcaLotlariSayfasiState extends State<ParcaLotlariSayfasi> {
  bool _bitenler = false;
  late Future<List<StokGrubu>> _gruplar = _yukle();

  Future<List<StokGrubu>> _yukle() => context.api.stok(parca: widget.parcaKod, bitenler: _bitenler);

  void _yenile() => setState(() {
    _gruplar = _yukle();
  });

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: GecisliBar(
      baslik: widget.parcaKod,
      eylemler: [
        if (KullaniciBaglami.of(context).kullanici.admin)
          IconButton(
            tooltip: 'Adet düzelt',
            icon: const Icon(Icons.exposure),
            onPressed: () async {
              final degisti = await Navigator.of(
                context,
              ).push<bool>(MaterialPageRoute(builder: (_) => AdetDuzeltSayfasi(parcaKod: widget.parcaKod)));
              if (degisti == true) _yenile();
            },
          ),
      ],
    ),
    body: Yukleyici<List<StokGrubu>>(
      future: _gruplar,
      yenile: _yenile,
      builder: (context, gruplar) {
        if (gruplar.isEmpty) return const BosDurum('Parça bulunamadı.');
        final g = gruplar.first;
        final admin = KullaniciBaglami.of(context).kullanici.admin;
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(g.parca.ad, style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
            const SizedBox(height: 4),
            Soluk(
              'Kullanılabilir ${g.parca.kullanilabilirStok} · toplam ${g.parca.toplamStok} · min ${g.parca.minStok} ${g.parca.birim}',
            ),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              title: const Text('Tükenen lotları da göster'),
              value: _bitenler,
              onChanged: (v) {
                _bitenler = v;
                _yenile();
              },
            ),
            if (g.lotlar.isEmpty) const BosDurum('Stokta lot yok.'),
            for (final lot in g.lotlar)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Kart(
                  uyari: lot.sorunlu,
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => LotIzSayfasi(lotNo: lot.lotNo))),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Wrap(
                              spacing: 8,
                              crossAxisAlignment: WrapCrossAlignment.center,
                              children: [
                                Mono(lot.lotNo, kalin: true),
                                if (lot.sorunlu) Rozet(lot.geriCagrildi ? 'Hatalı · üretime kapalı' : 'Hatalı', tur: RozetTuru.uyari),
                                if (lot.sirada) const Rozet('Sıradaki'),
                                if (lot.kalanAdet == 0) const Rozet('Bitti', tur: RozetTuru.basari),
                              ],
                            ),
                            const SizedBox(height: 4),
                            Soluk('${lot.tedarikciAd} · ${tarih(lot.siparisTarihi)}'),
                          ],
                        ),
                      ),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text('${lot.kalanAdet}', style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
                          Soluk('/ ${lot.girisAdet}', boyut: 12),
                        ],
                      ),
                      if (admin && !lot.sorunlu)
                        IconButton(
                          tooltip: 'Hatalı işaretle',
                          icon: const Icon(Icons.report_outlined, color: Color(0xFFC2410C)),
                          onPressed: () async {
                            if (await hataliLotIsaretle(context, lot)) _yenile();
                          },
                        ),
                    ],
                  ),
                ),
              ),
          ],
        );
      },
    ),
  );
}
