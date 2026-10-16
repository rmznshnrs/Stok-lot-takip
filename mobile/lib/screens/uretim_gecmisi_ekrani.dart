import 'dart:async';

import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/ortak.dart';
import 'izlenebilirlik_ekrani.dart';

/// Üretilen seri numaraları (yeniden eskiye): ürün, tarih, depoda / satıldı / hurda, geri çağrılan lot.
class UretimGecmisiSayfasi extends StatefulWidget {
  const UretimGecmisiSayfasi({super.key});

  @override
  State<UretimGecmisiSayfasi> createState() => _UretimGecmisiSayfasiState();
}

class _UretimGecmisiSayfasiState extends State<UretimGecmisiSayfasi> {
  late Future<List<UretimGecmisi>> _gecmis = context.api.uretimGecmisi();
  String _arama = '';
  Timer? _gecikme;

  @override
  void dispose() {
    _gecikme?.cancel();
    super.dispose();
  }

  void _yenile() => setState(() {
    _gecmis = context.api.uretimGecmisi(arama: _arama);
  });

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: const GecisliBar(baslik: 'Üretim geçmişi'),
    body: ListView(
      padding: const EdgeInsets.all(16),
      children: [
        TextField(
          decoration: const InputDecoration(prefixIcon: Icon(Icons.search)),
          onChanged: (m) {
            _gecikme?.cancel();
            _gecikme = Timer(const Duration(milliseconds: 300), () {
              _arama = m.trim();
              _yenile();
            });
          },
        ),
        const SizedBox(height: 12),
        Yukleyici<List<UretimGecmisi>>(
          future: _gecmis,
          yenile: _yenile,
          builder: (context, liste) {
            if (liste.isEmpty) return BosDurum(_arama.isEmpty ? 'Henüz üretim yok.' : 'Aramaya uyan üretim bulunamadı.');
            final depoda = liste.where((u) => u.depoda).length;
            final ayrilan = liste.where((u) => u.hurda != null).length;
            final satilan = liste.length - depoda - ayrilan;
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Padding(
                  padding: const EdgeInsets.only(left: 4, bottom: 8),
                  child: Soluk(
                    '${liste.length} ürün · $depoda depoda · $satilan satıldı${ayrilan > 0 ? ' · $ayrilan hurda / kayıp' : ''}',
                  ),
                ),
                for (final u in liste)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Kart(
                      uyari: u.geriCagrilanLotlar.isNotEmpty,
                      onTap: () =>
                          Navigator.of(context).push(MaterialPageRoute(builder: (_) => SeriIzSayfasi(seriNo: u.seriNo))),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(child: Mono(u.seriNo, kalin: true)),
                              _Durum(u: u),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Soluk('${u.urunAd} · ${tarihSaat(u.tarih)}'),
                          if (u.musteriAd != null)
                            Soluk('${u.musteriAd}${u.satisTarihi != null ? ' · ${tarih(u.satisTarihi!)}' : ''}'),
                          if (u.hurda != null) Soluk(u.hurda!.aciklama),
                          if (u.geriCagrilanLotlar.isNotEmpty) ...[
                            const SizedBox(height: 4),
                            Text(
                              'Geri çağrılan lot: ${u.geriCagrilanLotlar.join(', ')}',
                              style: TextStyle(color: Theme.of(context).colorScheme.error, fontSize: 13),
                            ),
                          ],
                        ],
                      ),
                    ),
                  ),
              ],
            );
          },
        ),
      ],
    ),
  );
}

class _Durum extends StatelessWidget {
  const _Durum({required this.u});

  final UretimGecmisi u;

  @override
  Widget build(BuildContext context) {
    if (u.hurda != null) return Rozet(u.hurda!.ad, tur: RozetTuru.uyari);
    if (u.musteriAd != null) return const Rozet('Satıldı');
    return const Rozet('Depoda', tur: RozetTuru.basari);
  }
}
