import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/ortak.dart';
import 'ana_kabuk.dart';

/// Lot no → ürünler → müşteriler, seri no → parçalar. Numara yazılır ya da okutulur.
class _Bilgi extends StatelessWidget {
  const _Bilgi(this.ad, this.deger);

  final String ad;
  final String deger;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.symmetric(vertical: 4),
    child: Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(width: 120, child: Soluk(ad)),
        Expanded(child: Text(deger)),
      ],
    ),
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

  void _yenile() => setState(() {
    _iz = context.api.lotIz(widget.lotNo);
  });

  Future<void> _calistir(Future<void> Function() islem) async {
    setState(() {
      _bekliyor = true;
    });
    try {
      await islem();
      _yenile();
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

  Future<void> _hataliIsaretle(Lot lot) async {
    if (await hataliLotIsaretle(context, lot)) _yenile();
  }

  Future<void> _hatayiKaldir(Lot lot) async {
    final onay = await _sor('${lot.lotNo} artık hatalı sayılmasın mı? Geri çağrılmış ürünler listede kalır.');
    if (onay && mounted) await _calistir(() => context.api.lotSorun(lot.lotNo, sorunlu: false, uretimdeKullan: true));
  }

  Future<bool> _sor(String soru) async =>
      await showDialog<bool>(
        context: context,
        builder: (c) => AlertDialog(
          content: Text(soru),
          actions: [
            TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Vazgeç')),
            FilledButton(onPressed: () => Navigator.pop(c, true), child: const Text('Evet')),
          ],
        ),
      ) ??
      false;

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
          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Kart(
                uyari: lot.sorunlu,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Mono(lot.lotNo, boyut: 22, kalin: true),
                    const SizedBox(height: 4),
                    Text('${lot.parcaAd} · ${lot.parcaKod}', style: const TextStyle(fontWeight: FontWeight.w600)),
                    if (lot.sorunlu) ...[
                      const Padding(
                        padding: EdgeInsets.only(top: 6),
                        child: Rozet('Hatalı lot', tur: RozetTuru.uyari),
                      ),
                      const SizedBox(height: 4),
                      Soluk(lot.sorunAciklama),
                      Soluk(lot.geriCagrildi ? 'Kalan parçalar üretimde kullanılmıyor' : 'Kalan parçalar üretimde kullanılabilir'),
                    ],
                    const SizedBox(height: 10),
                    _Bilgi('Tedarikçi', lot.tedarikciAd),
                    _Bilgi('Sipariş tarihi', tarih(lot.siparisTarihi)),
                    if (lot.siparisNo.isNotEmpty) _Bilgi('Sipariş no', lot.siparisNo),
                    _Bilgi('Giriş zamanı', tarihSaat(lot.girisZamani)),
                    _Bilgi('Gelen / kalan', '${lot.girisAdet} / ${lot.kalanAdet}'),
                    _Bilgi('Kullanılan', '${iz.kullanilanAdet}'),
                  ],
                ),
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: _Sayac(sayi: iz.uretimler.length, ad: 'üründe kullanıldı'),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _Sayac(sayi: iz.musteriler.length, ad: 'müşteriye ulaştı', uyari: iz.musteriler.isNotEmpty),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _Sayac(sayi: iz.depodakiler.length, ad: 'ürün depoda'),
                  ),
                ],
              ),
              const BolumBasligi('Kullanıldığı ürünler'),
              if (iz.uretimler.isEmpty) const BosDurum('Bu lot henüz hiçbir üretimde kullanılmadı.'),
              for (final u in iz.uretimler)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Kart(
                    onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => SeriIzSayfasi(seriNo: u.seriNo))),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [Mono(u.seriNo, kalin: true), Soluk('${u.urunAd} · ${u.adet} adet kullanıldı')],
                          ),
                        ),
                        if (u.geriCagirmaDurumu != null && u.hurdaTuru == null)
                          const Padding(
                            padding: EdgeInsets.only(right: 6),
                            child: Rozet('Geri çağrıldı', tur: RozetTuru.uyari),
                          ),
                        u.hurdaTuru != null
                            ? Rozet(hurdaAdi(u.hurdaTuru!))
                            : u.satis != null
                            ? const Rozet('Müşteride')
                            : const Rozet('Depoda', tur: RozetTuru.basari),
                      ],
                    ),
                  ),
                ),
              const BolumBasligi('Ürünlerin gittiği müşteriler'),
              if (iz.musteriler.isEmpty) const BosDurum('Bu lottan yapılan ürünlerden henüz satılan yok.'),
              for (final m in iz.musteriler)
                Padding(
                  padding: const EdgeInsets.only(bottom: 8),
                  child: Kart(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(m.ad, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 16)),
                        for (final s in m.satislar) Row(children: [Mono(s.seriNo, boyut: 13), Soluk(' · ${tarih(s.tarih)}')]),
                        if (m.iletisim.isNotEmpty) Padding(padding: const EdgeInsets.only(top: 4), child: Soluk(m.iletisim)),
                      ],
                    ),
                  ),
                ),
              if (admin) ...[
                const SizedBox(height: 16),
                if (!lot.sorunlu)
                  FilledButton.icon(
                    style: FilledButton.styleFrom(backgroundColor: const Color(0xFFC2410C)),
                    onPressed: _bekliyor ? null : () => _hataliIsaretle(lot),
                    icon: const Icon(Icons.report),
                    label: const Text('Hatalı lot olarak işaretle'),
                  )
                else ...[
                  OutlinedButton.icon(
                    onPressed: _bekliyor ? null : () => _hataliIsaretle(lot),
                    icon: const Icon(Icons.edit_outlined),
                    label: const Text('Hatayı düzenle'),
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton.icon(
                    onPressed: _bekliyor ? null : () => _hatayiKaldir(lot),
                    icon: const Icon(Icons.undo),
                    label: const Text('Hatayı kaldır'),
                  ),
                ],
              ],
            ],
          );
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
    child: Column(
      children: [
        Text(
          '$sayi',
          style: TextStyle(fontSize: 22, fontWeight: FontWeight.w700, color: uyari ? Theme.of(context).colorScheme.error : null),
        ),
        Soluk(ad, boyut: 11),
      ],
    ),
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
      yenile: () => setState(() {
        _iz = context.api.seriIz(widget.seriNo);
      }),
      builder: (context, iz) {
        if (iz == null) return BosDurum('${widget.seriNo} seri numarası bulunamadı.');
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Kart(
              uyari: iz.geriCagrilanLotlar.isNotEmpty || (iz.geriCagirma?.acik ?? false),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Mono(iz.seriNo, boyut: 22, kalin: true),
                  const SizedBox(height: 4),
                  Text(iz.urunAd, style: const TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 10),
                  _Bilgi('Üretim tarihi', tarihSaat(iz.tarih)),
                  _Bilgi('Durum', iz.satis != null ? 'Satıldı' : iz.hurda?.ad ?? 'Depoda'),
                  if (iz.hurda != null)
                    _Bilgi('${iz.hurda!.ad} nedeni', '${iz.hurda!.aciklama} · ${iz.hurda!.kullaniciAd}, ${tarihSaat(iz.hurda!.zaman)}'),
                  if (iz.satis != null) _Bilgi('Satış', '${iz.satis!.musteriAd} · ${tarih(iz.satis!.tarih)}'),
                  if (iz.satis?.musteriIletisim.isNotEmpty ?? false) _Bilgi('Müşteri iletişim', iz.satis!.musteriIletisim),
                  if (iz.geriCagirma?.acik ?? false)
                    Padding(
                      padding: const EdgeInsets.only(top: 8),
                      child: Rozet('Geri çağrıldı · ${iz.geriCagirma!.durumAdi}', tur: RozetTuru.uyari),
                    ),
                  if (iz.geriCagrilanLotlar.isNotEmpty)
                    Padding(
                      padding: const EdgeInsets.only(top: 8),
                      child: Rozet('Hatalı lot var: ${iz.geriCagrilanLotlar.join(', ')}', tur: RozetTuru.uyari),
                    ),
                ],
              ),
            ),
            const BolumBasligi('İçindeki parçalar ve lotları'),
            for (final p in iz.parcalar)
              Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: Kart(
                  uyari: p.lot.sorunlu,
                  onTap: () => Navigator.of(context).push(MaterialPageRoute(builder: (_) => LotIzSayfasi(lotNo: p.lot.lotNo))),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(p.parca.ad, style: const TextStyle(fontWeight: FontWeight.w600)),
                            Row(
                              children: [Mono(p.lot.lotNo, boyut: 13), if (p.lot.sorunlu) const Soluk(' · hatalı')],
                            ),
                            Soluk('${p.lot.tedarikciAd} · ${tarih(p.lot.siparisTarihi)}'),
                          ],
                        ),
                      ),
                      Text('× ${p.adet}', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600)),
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

/// Yalnız yönetici: lotu hatalı işaretler. Hata açıklaması ve kalan parçaların üretimde kullanılıp kullanılmayacağı
/// (seçim zorunlu) sorulur. İlk işaretlemede bu lottan parça taşıyan ürünler kendiliğinden geri çağrılır.
/// Kaydedildiyse true döner.
Future<bool> hataliLotIsaretle(BuildContext context, Lot lot) async {
  final aciklama = TextEditingController(text: lot.sorunAciklama);
  bool? kullan = lot.sorunlu ? !lot.geriCagrildi : null;
  final tamam = await showDialog<bool>(
    context: context,
    builder: (c) => StatefulBuilder(
      builder: (c, yenile) => AlertDialog(
        title: Text(lot.sorunlu ? 'Hatayı düzenle' : 'Hatalı lot'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Mono(lot.lotNo, kalin: true),
            const SizedBox(height: 10),
            TextField(
              controller: aciklama,
              decoration: const InputDecoration(labelText: 'Hata'),
              onChanged: (_) => yenile(() {}),
            ),
            const SizedBox(height: 14),
            Soluk('Stokta kalan ${lot.kalanAdet} parça üretimde'),
            const SizedBox(height: 6),
            SegmentedButton<bool>(
              emptySelectionAllowed: true,
              segments: const [
                ButtonSegment(value: true, label: Text('kullanılsın')),
                ButtonSegment(value: false, label: Text('kullanılmasın')),
              ],
              selected: {?kullan},
              onSelectionChanged: (s) => yenile(() {
                kullan = s.isEmpty ? kullan : s.first;
              }),
            ),
            if (!lot.sorunlu) ...[
              const SizedBox(height: 10),
              const Soluk('Kaydedince bu lottan parça taşıyan ürünler kendiliğinden geri çağrılır.', boyut: 12),
            ],
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(c, false), child: const Text('Vazgeç')),
          FilledButton(
            onPressed: kullan == null || aciklama.text.trim().isEmpty ? null : () => Navigator.pop(c, true),
            child: const Text('Kaydet'),
          ),
        ],
      ),
    ),
  );
  final metin = aciklama.text.trim();
  aciklama.dispose();
  if (tamam != true || !context.mounted) return false;
  try {
    final geri = await context.api.lotSorun(lot.lotNo, sorunlu: true, uretimdeKullan: kullan!, aciklama: metin);
    if (context.mounted) {
      bildir(
        context,
        lot.sorunlu
            ? '${lot.lotNo} güncellendi.'
            : geri.isEmpty
            ? '${lot.lotNo} hatalı işaretlendi. Bu lottan yapılmış ürün yok.'
            : '${lot.lotNo} hatalı işaretlendi. ${geri.length} ürün geri çağrıldı.',
      );
    }
    return true;
  } catch (e) {
    if (context.mounted) bildir(context, hataMetni(e), hata: true);
    return false;
  }
}
