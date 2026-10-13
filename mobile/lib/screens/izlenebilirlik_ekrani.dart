import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/barkod.dart';
import '../widgets/ortak.dart';
import 'ana_kabuk.dart';

/// Lot no → ürünler → müşteriler, seri no → parçalar. Numara yazılır ya da okutulur.
class IzlenebilirlikEkrani extends StatefulWidget {
  const IzlenebilirlikEkrani({super.key});

  @override
  State<IzlenebilirlikEkrani> createState() => _IzlenebilirlikEkraniState();
}

class _IzlenebilirlikEkraniState extends State<IzlenebilirlikEkrani> {
  final _lot = TextEditingController();
  final _seri = TextEditingController();

  @override
  void dispose() {
    _lot.dispose();
    _seri.dispose();
    super.dispose();
  }

  void _ac(Widget sayfa) => Navigator.of(context).push(MaterialPageRoute(builder: (_) => sayfa));

  @override
  Widget build(BuildContext context) => DefaultTabController(
        length: 2,
        child: Scaffold(
          appBar: const GecisliBar(
            baslik: 'İzlenebilirlik',
            alt: TabBar(
              labelColor: Colors.white,
              unselectedLabelColor: Colors.white70,
              indicatorColor: Colors.white,
              tabs: [Tab(text: 'Lot → Müşteri'), Tab(text: 'Seri no → Parçalar')],
            ),
          ),
          body: TabBarView(children: [
            ListView(padding: const EdgeInsets.all(16), children: [
              OkutulabilirAlan(
                denetleyici: _lot,
                etiket: 'Lot numarası',
                ipucu: 'L-…',
                gonder: (v) => v.trim().isEmpty ? null : _ac(LotIzSayfasi(lotNo: v.trim())),
              ),
              const SizedBox(height: 12),
              AnaDugme(
                metin: 'Lotu izle',
                ikon: Icons.search,
                onPressed: () => _lot.text.trim().isEmpty ? null : _ac(LotIzSayfasi(lotNo: _lot.text.trim())),
              ),
            ]),
            ListView(padding: const EdgeInsets.all(16), children: [
              OkutulabilirAlan(
                denetleyici: _seri,
                etiket: 'Seri numarası',
                ipucu: 'SN-…',
                gonder: (v) => v.trim().isEmpty ? null : _ac(SeriIzSayfasi(seriNo: v.trim())),
              ),
              const SizedBox(height: 12),
              AnaDugme(
                metin: 'Ürünü izle',
                ikon: Icons.search,
                onPressed: () => _seri.text.trim().isEmpty ? null : _ac(SeriIzSayfasi(seriNo: _seri.text.trim())),
              ),
            ]),
          ]),
        ),
      );
}

class _Bilgi extends StatelessWidget {
  const _Bilgi(this.ad, this.deger);

  final String ad;
  final String deger;

  @override
  Widget build(BuildContext context) => Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
          SizedBox(width: 120, child: Soluk(ad)),
          Expanded(child: Text(deger)),
        ]),
      );
}

class LotIzSayfasi extends StatefulWidget {
  const LotIzSayfasi({super.key, required this.lotNo});

  final String lotNo;

  @override
  State<LotIzSayfasi> createState() => _LotIzSayfasiState();
}

class _LotIzSayfasiState extends State<LotIzSayfasi> {
  late Future<LotIz?> _iz = context.api.lotIz(widget.lotNo);
  bool _bekliyor = false;

  void _yenile() => setState(() => _iz = context.api.lotIz(widget.lotNo));

  Future<void> _geriCagir(LotIz iz) async {
    final lot = iz.lot;
    final soru = lot.geriCagrildi
        ? '${lot.lotNo} için geri çağırma kaldırılsın mı? Lot yeniden üretimde kullanılabilir olacak.'
        : '${lot.lotNo} geri çağrılsın mı? Lot üretimde kullanılmayacak; ${iz.uretimler.length} ürün ve ${iz.musteriler.length} müşteri etkileniyor.';
    final onay = await showDialog<bool>(
      context: context,
      builder: (c) => AlertDialog(
        content: Text(soru),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Vazgeç')),
          FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Evet')),
        ],
      ),
    );
    if (onay != true || !mounted) return;
    setState(() => _bekliyor = true);
    try {
      await context.api.geriCagir(lot.lotNo, !lot.geriCagrildi);
      _yenile();
    } catch (e) {
      if (mounted) bildir(context, hataMetni(e), hata: true);
    } finally {
      if (mounted) setState(() => _bekliyor = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final admin = KullaniciBaglami.of(context).kullanici.admin;
    return Scaffold(
      appBar: GecisliBar(baslik: widget.lotNo),
      body: Yukleyici<LotIz?>(
        future: _iz,
        yenile: _yenile,
        builder: (context, iz) {
          if (iz == null) return BosDurum('${widget.lotNo} lot numarası bulunamadı.');
          final lot = iz.lot;
          return ListView(padding: const EdgeInsets.all(16), children: [
            Kart(
              uyari: lot.geriCagrildi,
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Mono(lot.lotNo, boyut: 22, kalin: true),
                const SizedBox(height: 4),
                Text('${lot.parcaAd} · ${lot.parcaKod}', style: const TextStyle(fontWeight: FontWeight.w600)),
                if (lot.geriCagrildi) const Padding(padding: EdgeInsets.only(top: 6), child: Rozet('Geri çağrıldı', tur: RozetTuru.uyari)),
                const SizedBox(height: 10),
                _Bilgi('Tedarikçi', lot.tedarikciAd),
                _Bilgi('Sipariş tarihi', tarih(lot.siparisTarihi)),
                if (lot.siparisNo.isNotEmpty) _Bilgi('Sipariş no', lot.siparisNo),
                _Bilgi('Giriş zamanı', tarihSaat(lot.girisZamani)),
                _Bilgi('Gelen / kalan', '${lot.girisAdet} / ${lot.kalanAdet}'),
                _Bilgi('Kullanılan', '${iz.kullanilanAdet}'),
              ]),
            ),
            const SizedBox(height: 10),
            Row(children: [
              Expanded(child: _Sayac(sayi: iz.uretimler.length, ad: 'üründe kullanıldı')),
              const SizedBox(width: 8),
              Expanded(child: _Sayac(sayi: iz.musteriler.length, ad: 'müşteriye ulaştı', uyari: iz.musteriler.isNotEmpty)),
              const SizedBox(width: 8),
              Expanded(child: _Sayac(sayi: iz.depodakiler.length, ad: 'ürün depoda')),
            ]),
            const BolumBasligi('Kullanıldığı ürünler'),
            if (iz.uretimler.isEmpty) const BosDurum('Bu lot henüz hiçbir üretimde kullanılmadı.'),
            for (final u in iz.uretimler)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Kart(
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => SeriIzSayfasi(seriNo: u.seriNo))),
                  child: Row(children: [
                    Expanded(
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Mono(u.seriNo, kalin: true),
                        Soluk('${u.urunAd} · ${u.adet} adet kullanıldı'),
                      ]),
                    ),
                    u.satis == null ? const Rozet('Depoda', tur: RozetTuru.basari) : const Rozet('Satıldı', tur: RozetTuru.uyari),
                  ]),
                ),
              ),
            const BolumBasligi('Etkilenen müşteriler'),
            if (iz.musteriler.isEmpty) const BosDurum('Bu lottan yapılan ürünlerden henüz satılan yok.'),
            for (final m in iz.musteriler)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Kart(
                  child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text(m.ad, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16)),
                    for (final s in m.satislar) Row(children: [Mono(s.seriNo, boyut: 13), Soluk(' · ${tarih(s.tarih)}')]),
                    if (m.iletisim.isNotEmpty) Padding(padding: const EdgeInsets.only(top: 4), child: Soluk(m.iletisim)),
                  ]),
                ),
              ),
            if (admin) ...[
              const SizedBox(height: 16),
              lot.geriCagrildi
                  ? OutlinedButton.icon(
                      onPressed: _bekliyor ? null : () => _geriCagir(iz),
                      icon: const Icon(Icons.undo),
                      label: const Text('Geri çağırmayı kaldır'),
                    )
                  : FilledButton.icon(
                      style: FilledButton.styleFrom(backgroundColor: const Color(0xFFC2410C)),
                      onPressed: _bekliyor ? null : () => _geriCagir(iz),
                      icon: const Icon(Icons.report),
                      label: const Text('Bu lotu geri çağır'),
                    ),
            ],
          ]);
        },
      ),
    );
  }
}

class _Sayac extends StatelessWidget {
  const _Sayac({required this.sayi, required this.ad, this.uyari = false});

  final int sayi;
  final String ad;
  final bool uyari;

  @override
  Widget build(BuildContext context) => Kart(
        padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 8),
        child: Column(children: [
          Text('$sayi',
              style: TextStyle(fontSize: 22, fontWeight: FontWeight.w700, color: uyari ? Theme.of(context).colorScheme.error : null)),
          Soluk(ad, boyut: 11),
        ]),
      );
}

class SeriIzSayfasi extends StatefulWidget {
  const SeriIzSayfasi({super.key, required this.seriNo});

  final String seriNo;

  @override
  State<SeriIzSayfasi> createState() => _SeriIzSayfasiState();
}

class _SeriIzSayfasiState extends State<SeriIzSayfasi> {
  late Future<SeriIz?> _iz = context.api.seriIz(widget.seriNo);

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: GecisliBar(baslik: widget.seriNo),
        body: Yukleyici<SeriIz?>(
          future: _iz,
          yenile: () => setState(() => _iz = context.api.seriIz(widget.seriNo)),
          builder: (context, iz) {
            if (iz == null) return BosDurum('${widget.seriNo} seri numarası bulunamadı.');
            return ListView(padding: const EdgeInsets.all(16), children: [
              Kart(
                uyari: iz.geriCagrilanLotlar.isNotEmpty,
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Mono(iz.seriNo, boyut: 22, kalin: true),
                  const SizedBox(height: 4),
                  Text(iz.urunAd, style: const TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 10),
                  _Bilgi('Üretim tarihi', tarihSaat(iz.tarih)),
                  _Bilgi('Durum', iz.satis == null ? 'Depoda' : 'Satıldı'),
                  if (iz.satis != null) _Bilgi('Satış', '${iz.satis!.musteriAd} · ${tarih(iz.satis!.tarih)}'),
                  if (iz.satis?.musteriIletisim.isNotEmpty ?? false) _Bilgi('Müşteri iletişim', iz.satis!.musteriIletisim),
                  if (iz.geriCagrilanLotlar.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 8),
                      child: Rozet('Geri çağrılan ${iz.geriCagrilanLotlar.length} lot var', tur: RozetTuru.uyari),
                    ),
                ]),
              ),
              const BolumBasligi('İçindeki parçalar ve lotları'),
              for (final p in iz.parcalar)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Kart(
                    uyari: p.lot.geriCagrildi,
                    onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => LotIzSayfasi(lotNo: p.lot.lotNo))),
                    child: Row(children: [
                      Expanded(
                        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                          Text(p.parca.ad, style: const TextStyle(fontWeight: FontWeight.w600)),
                          Row(children: [Mono(p.lot.lotNo, boyut: 13), if (p.lot.geriCagrildi) const Soluk(' · geri çağrıldı')]),
                          Soluk('${p.lot.tedarikciAd} · ${tarih(p.lot.siparisTarihi)}'),
                        ]),
                      ),
                      Text('× ${p.adet}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
                    ]),
                  ),
                ),
            ]);
          },
        ),
      );
}
