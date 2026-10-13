import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/barkod.dart';
import '../widgets/ortak.dart';

/// Mal girişi: parça (kod veya adla), tedarikçi, lot no (kamerayla okutulabilir), adet, tarih.
class MalGirisiEkrani extends StatefulWidget {
  const MalGirisiEkrani({super.key});

  @override
  State<MalGirisiEkrani> createState() => _MalGirisiEkraniState();
}

class _MalGirisiEkraniState extends State<MalGirisiEkrani> {
  final _parcaMetni = TextEditingController();
  final _tedarikci = TextEditingController();
  final _lotNo = TextEditingController();
  final _adet = TextEditingController();
  final _siparisNo = TextEditingController();
  DateTime _siparisTarihi = DateTime.now();
  ParcaOzet? _parca;
  String? _parcaHatasi;
  List<String> _tedarikciler = [];
  bool _bekliyor = false;
  bool _eklendi = false;

  @override
  void initState() {
    super.initState();
    context.api
        .tedarikciler()
        .then(
          (t) => mounted
              ? setState(() {
                  _tedarikciler = t;
                })
              : null,
        )
        .catchError((_) => null);
  }

  @override
  void dispose() {
    for (final d in [_parcaMetni, _tedarikci, _lotNo, _adet, _siparisNo]) {
      d.dispose();
    }
    super.dispose();
  }

  Future<void> _parcaBul([String? metin]) async {
    final m = (metin ?? _parcaMetni.text).trim();
    if (m.isEmpty) return;
    try {
      final p = await context.api.parcaBul(m);
      setState(() {
        _parca = p;
        _parcaHatasi = null;
      });
    } catch (e) {
      setState(() {
        _parca = null;
        _parcaHatasi = hataMetni(e);
      });
    }
  }

  Future<void> _tarihSec() async {
    final t = await showDatePicker(
      context: context,
      initialDate: _siparisTarihi,
      firstDate: DateTime(2000),
      lastDate: DateTime.now().add(const Duration(days: 365)),
    );
    if (t != null) {
      setState(() {
        _siparisTarihi = t;
      });
    }
  }

  Future<void> _kaydet() async {
    final adet = int.tryParse(_adet.text.trim());
    final eksik = [
      if (_parca == null) 'parça',
      if (_tedarikci.text.trim().isEmpty) 'tedarikçi',
      if (_lotNo.text.trim().isEmpty) 'lot numarası',
      if (adet == null || adet < 1) 'adet',
    ];
    if (eksik.isNotEmpty) return bildir(context, 'Eksik: ${eksik.join(', ')}', hata: true);

    setState(() {
      _bekliyor = true;
    });
    try {
      final s = await context.api.malGirisi(
        parcaId: _parca!.id,
        tedarikci: _tedarikci.text.trim(),
        lotNo: _lotNo.text.trim(),
        adet: adet!,
        siparisTarihi: _siparisTarihi,
        siparisNo: _siparisNo.text.trim(),
      );
      if (!mounted) return;
      _eklendi = true;
      bildir(
        context,
        '${s.lot.lotNo} · ${s.lot.parcaKod} · ${s.lot.girisAdet} ${_parca!.birim} stoğa eklendi.'
        '${s.yeniTedarikci ? ' ${s.lot.tedarikciAd} yeni tedarikçi.' : ''}',
      );
      // Aynı parça ve tedarikçiden bir sonraki lot için form hazır kalır
      _lotNo.clear();
      _adet.clear();
      _siparisNo.clear();
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
    onPopInvokedWithResult: (bitti, _) {
      if (!bitti) Navigator.of(context).pop(_eklendi);
    },
    child: Scaffold(
      appBar: const GecisliBar(baslik: 'Mal girişi'),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          TextField(
            controller: _parcaMetni,
            decoration: InputDecoration(
              labelText: 'Parça (kod veya ad)',
              errorText: _parcaHatasi,
              suffixIcon: IconButton(icon: const Icon(Icons.search), onPressed: _parcaBul),
            ),
            textInputAction: TextInputAction.search,
            onSubmitted: _parcaBul,
            onChanged: (_) => setState(() {
              _parca = null;
            }),
          ),
          if (_parca != null)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: Kart(
                child: Row(
                  children: [
                    const Icon(Icons.check_circle, color: Colors.green),
                    const SizedBox(width: 10),
                    Expanded(child: Text(_parca!.ad)),
                    Mono(_parca!.kod, boyut: 12),
                  ],
                ),
              ),
            ),
          const SizedBox(height: 14),
          ListedenSecilebilirAlan(denetleyici: _tedarikci, etiket: 'Tedarikçi', secenekler: _tedarikciler),
          const SizedBox(height: 14),
          OkutulabilirAlan(denetleyici: _lotNo, etiket: 'Lot numarası', ipucu: 'L-…'),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: TextField(
                  controller: _adet,
                  keyboardType: TextInputType.number,
                  decoration: const InputDecoration(labelText: 'Adet'),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: InkWell(
                  onTap: _tarihSec,
                  child: InputDecorator(
                    decoration: const InputDecoration(
                      labelText: 'Sipariş tarihi',
                      suffixIcon: Icon(Icons.calendar_today, size: 18),
                    ),
                    child: Text(tarih(_siparisTarihi)),
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 14),
          TextField(
            controller: _siparisNo,
            decoration: const InputDecoration(labelText: 'Sipariş / irsaliye no'),
          ),
          const SizedBox(height: 24),
          AnaDugme(metin: 'Stoğa ekle', ikon: Icons.add, bekliyor: _bekliyor, onPressed: _kaydet),
        ],
      ),
    ),
  );
}
