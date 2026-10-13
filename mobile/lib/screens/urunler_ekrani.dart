import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/ortak.dart';

class UrunlerEkrani extends StatefulWidget {
  const UrunlerEkrani({super.key});

  @override
  State<UrunlerEkrani> createState() => _UrunlerEkraniState();
}

class _UrunlerEkraniState extends State<UrunlerEkrani> {
  late Future<List<UrunListe>> _urunler = context.api.urunler();

  void _yenile() => setState(() {
    _urunler = context.api.urunler();
  });

  Future<void> _ac(int? id) async {
    await Navigator.of(context).push(MaterialPageRoute(builder: (_) => UrunSayfasi(urunId: id)));
    _yenile();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: const GecisliBar(baslik: 'Ürünler'),
    floatingActionButton: FloatingActionButton.extended(
      icon: const Icon(Icons.add),
      label: const Text('Yeni ürün'),
      onPressed: () => _ac(null),
    ),
    body: Yukleyici<List<UrunListe>>(
      future: _urunler,
      yenile: _yenile,
      builder: (context, liste) => liste.isEmpty
          ? const BosDurum('Henüz ürün yok.')
          : ListView(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 96),
              children: [
                for (final u in liste)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Kart(
                      onTap: () => _ac(u.id),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(u.ad, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16)),
                                Row(children: [Mono(u.kod, boyut: 12), Soluk(' · önek '), Mono(u.seriOneki, boyut: 12)]),
                                Soluk('${u.parcaCesidi} parça çeşidi · ${u.uretimSayisi} üretim'),
                              ],
                            ),
                          ),
                          const Icon(Icons.chevron_right),
                        ],
                      ),
                    ),
                  ),
              ],
            ),
    ),
  );
}

/// Ürün bilgileri ve ürün ağacı (1 ürün için hangi parçadan kaç adet). urunId null ise yeni ürün.
class UrunSayfasi extends StatefulWidget {
  const UrunSayfasi({super.key, required this.urunId});

  final int? urunId;

  @override
  State<UrunSayfasi> createState() => _UrunSayfasiState();
}

class _UrunSayfasiState extends State<UrunSayfasi> {
  final _ad = TextEditingController();
  final _kod = TextEditingController();
  final _onek = TextEditingController();
  List<AgacSatiri> _agac = [];
  List<ParcaStok> _parcalar = [];
  UrunDetay? _urun;
  bool _yuklendi = false;
  String? _hata;
  bool _bekliyor = false;

  @override
  void initState() {
    super.initState();
    _yukle();
  }

  @override
  void dispose() {
    _ad.dispose();
    _kod.dispose();
    _onek.dispose();
    super.dispose();
  }

  Future<void> _yukle() async {
    try {
      final api = context.api;
      final parcalar = await api.parcalar();
      final urun = widget.urunId == null ? null : await api.urun(widget.urunId!);
      if (!mounted) return;
      setState(() {
        _parcalar = parcalar;
        _urunuGoster(urun);
        _yuklendi = true;
        _hata = null;
      });
    } catch (e) {
      if (mounted) {
        setState(() {
          _hata = hataMetni(e);
        });
      }
    }
  }

  void _urunuGoster(UrunDetay? u) {
    _urun = u;
    _ad.text = u?.ad ?? '';
    _kod.text = u?.kod ?? '';
    _onek.text = u?.seriOneki ?? '';
    _agac = [...?u?.agac];
  }

  Future<void> _parcaEkle() async {
    final kalan = _parcalar.where((p) => !_agac.any((s) => s.parcaId == p.id)).toList();
    final secilen = await showModalBottomSheet<ParcaStok>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (c) => SizedBox(
        height: MediaQuery.of(c).size.height * 0.7,
        child: kalan.isEmpty
            ? const BosDurum('Eklenecek parça kalmadı.')
            : ListView(
                children: [
                  for (final p in kalan)
                    ListTile(title: Text(p.ad), subtitle: Mono(p.kod, boyut: 12), onTap: () => Navigator.pop(c, p)),
                ],
              ),
      ),
    );
    if (secilen == null) return;
    setState(() {
      _agac.add(
        AgacSatiri(
          parcaId: secilen.id,
          parcaKod: secilen.kod,
          parcaAd: secilen.ad,
          adet: 1,
          kullanilabilirStok: secilen.kullanilabilirStok,
          minAltinda: secilen.minAltinda,
        ),
      );
      _agac.sort((a, b) => a.parcaKod.compareTo(b.parcaKod));
    });
  }

  Future<void> _kaydet() async {
    if (_ad.text.trim().isEmpty || _kod.text.trim().isEmpty || _onek.text.trim().isEmpty) {
      return bildir(context, 'Ürün adı, kodu ve seri no öneki girin.', hata: true);
    }
    setState(() {
      _bekliyor = true;
    });
    try {
      final api = context.api;
      final u = _urun == null
          ? await api.urunOlustur(_kod.text.trim(), _ad.text.trim(), _onek.text.trim())
          : await api.urunGuncelle(_urun!.id, _kod.text.trim(), _ad.text.trim(), _onek.text.trim());
      final kayitli = await api.agacKaydet(u.id, [for (final s in _agac) (parcaId: s.parcaId, adet: s.adet)]);
      if (!mounted) return;
      setState(() {
        _urunuGoster(kayitli);
      });
      bildir(context, '${kayitli.kod} – ${kayitli.ad} kaydedildi.');
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
  Widget build(BuildContext context) {
    final onekKilitli = _urun != null && !_urun!.seriOnekiDegistirilebilir;
    return Scaffold(
      appBar: GecisliBar(baslik: _urun?.ad ?? (widget.urunId == null ? 'Yeni ürün' : 'Ürün')),
      body: _hata != null
          ? HataGorunumu(metin: _hata!, yenile: _yukle)
          : !_yuklendi
          ? const Center(child: CircularProgressIndicator())
          : ListView(
              padding: const EdgeInsets.all(16),
              children: [
                TextField(
                  controller: _ad,
                  decoration: const InputDecoration(labelText: 'Ürün adı'),
                ),
                const SizedBox(height: 14),
                Row(
                  children: [
                    Expanded(
                      child: TextField(
                        controller: _kod,
                        style: const TextStyle(fontFamily: 'IBMPlexMono'),
                        decoration: const InputDecoration(labelText: 'Ürün kodu'),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: TextField(
                        controller: _onek,
                        enabled: !onekKilitli,
                        style: const TextStyle(fontFamily: 'IBMPlexMono'),
                        decoration: InputDecoration(
                          labelText: 'Seri no öneki',
                          hintText: 'SN-IP-',
                          helperText: onekKilitli ? 'Üretim yapıldı, değişmez' : null,
                        ),
                      ),
                    ),
                  ],
                ),
                const BolumBasligi('Ürün ağacı (1 ürün için)'),
                if (_agac.isEmpty) const BosDurum('Ürün ağacı boş.'),
                for (final s in _agac)
                  Padding(
                    padding: const EdgeInsets.only(bottom: 8),
                    child: Kart(
                      padding: const EdgeInsets.fromLTRB(14, 8, 4, 8),
                      child: Row(
                        children: [
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(s.parcaAd),
                                Row(children: [Mono(s.parcaKod, boyut: 12), Soluk(' · stok ${s.kullanilabilirStok}', boyut: 12)]),
                              ],
                            ),
                          ),
                          IconButton(
                            onPressed: s.adet > 1
                                ? () => setState(() {
                                    s.adet--;
                                  })
                                : null,
                            icon: const Icon(Icons.remove_circle_outline),
                          ),
                          Text('${s.adet}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
                          IconButton(
                            onPressed: () => setState(() {
                              s.adet++;
                            }),
                            icon: const Icon(Icons.add_circle_outline),
                          ),
                          IconButton(
                            tooltip: 'Kaldır',
                            onPressed: () => setState(() {
                              _agac.remove(s);
                            }),
                            icon: Icon(Icons.delete_outline, color: Theme.of(context).colorScheme.error),
                          ),
                        ],
                      ),
                    ),
                  ),
                OutlinedButton.icon(onPressed: _parcaEkle, icon: const Icon(Icons.add), label: const Text('Parça ekle')),
                const SizedBox(height: 24),
                AnaDugme(metin: 'Kaydet', bekliyor: _bekliyor, onPressed: _kaydet),
              ],
            ),
    );
  }
}
