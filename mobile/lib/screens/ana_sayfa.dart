import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../tema.dart';
import '../widgets/barkod.dart';
import '../widgets/ortak.dart';
import 'ana_kabuk.dart';
import 'izlenebilirlik_ekrani.dart';
import 'stok_ekrani.dart';

/// Lot, seri no veya parça koduna göre doğru ekranı açar.
void numaraAc(BuildContext context, String tur, String deger) {
  final sayfa = switch (tur) {
    'lot' => LotIzSayfasi(lotNo: deger),
    'seri' => SeriIzSayfasi(seriNo: deger),
    _ => ParcaLotlariSayfasi(parcaKod: deger),
  };
  Navigator.of(context).push(MaterialPageRoute(builder: (_) => sayfa));
}

class AnaSayfa extends StatefulWidget {
  const AnaSayfa({super.key});

  @override
  State<AnaSayfa> createState() => _AnaSayfaState();
}

class _AnaSayfaState extends State<AnaSayfa> {
  final _arama = TextEditingController();
  late Future<List<Hareket>> _hareketler = context.api.hareketler(10);
  NumaraArama? _sonuc;
  String _aranan = '';
  bool _araniyor = false;

  @override
  void dispose() {
    _arama.dispose();
    super.dispose();
  }

  Future<void> _ara(String metin) async {
    final q = metin.trim();
    if (q.isEmpty) return;
    setState(() {
      _araniyor = true;
      _sonuc = null;
    });
    try {
      final s = await context.api.ara(q);
      if (!mounted) return;
      if (s.tur != null && s.deger != null) {
        numaraAc(context, s.tur!, s.deger!);
      } else {
        setState(() {
          _sonuc = s;
          _aranan = q;
        });
      }
    } catch (e) {
      if (mounted) bildir(context, hataMetni(e), hata: true);
    } finally {
      if (mounted) setState(() => _araniyor = false);
    }
  }

  Future<void> _yenile() async {
    final f = context.api.hareketler(10);
    setState(() => _hareketler = f);
    await f.catchError((_) => <Hareket>[]);
  }

  @override
  Widget build(BuildContext context) {
    final git = KullaniciBaglami.of(context).sekmeyeGit;
    return Scaffold(
      appBar: const GecisliBar(baslik: 'MKC Bilişim'),
      body: RefreshIndicator(
        onRefresh: _yenile,
        child: ListView(padding: const EdgeInsets.all(16), children: [
          OkutulabilirAlan(
            denetleyici: _arama,
            etiket: 'Lot, seri no veya parça kodu',
            ipucu: 'L-D-2601, SN-IP-0001…',
            gonder: _ara,
          ),
          const SizedBox(height: 10),
          AnaDugme(metin: 'İzini sür', ikon: Icons.search, bekliyor: _araniyor, onPressed: () => _ara(_arama.text)),
          if (_sonuc != null) ...[
            const SizedBox(height: 12),
            if (_sonuc!.adaylar.isEmpty)
              Kart(uyari: true, child: Text('“$_aranan” ile eşleşen lot, seri no veya parça kodu bulunamadı.'))
            else
              Kart(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text('“$_aranan” birebir eşleşmedi. Şunlardan biri mi?'),
                  const SizedBox(height: 8),
                  Wrap(spacing: 8, runSpacing: 8, children: [
                    for (final a in _sonuc!.adaylar)
                      ActionChip(
                        avatar: Text(a.tur == 'lot' ? 'lot' : a.tur == 'seri' ? 'seri' : 'parça', style: const TextStyle(fontSize: 10)),
                        label: Mono(a.deger, boyut: 13),
                        onPressed: () => numaraAc(context, a.tur, a.deger),
                      ),
                  ]),
                ]),
              ),
          ],
          const SizedBox(height: 20),
          Row(children: [
            _HizliIslem(no: 1, renk: Renk.mor, baslik: 'Mal girişi', ikon: Icons.move_to_inbox, onTap: () => malGirisiAc(context)),
            const SizedBox(width: 10),
            _HizliIslem(no: 2, renk: Renk.turkuaz, baslik: 'Üret', ikon: Icons.precision_manufacturing, onTap: () => git(Sekme.uretim)),
            const SizedBox(width: 10),
            _HizliIslem(no: 3, renk: Renk.pembe, baslik: 'Satış', ikon: Icons.point_of_sale, onTap: () => git(Sekme.satis)),
          ]),
          const BolumBasligi('Son hareketler'),
          Yukleyici<List<Hareket>>(
            future: _hareketler,
            yenile: _yenile,
            builder: (context, liste) => liste.isEmpty
                ? const BosDurum('Henüz hareket yok.')
                : Column(children: [
                    for (final h in liste)
                      Padding(
                        padding: const EdgeInsets.only(bottom: 8),
                        child: Kart(
                          onTap: () => numaraAc(context, h.numaraTuru, h.numaraIlk),
                          child: Row(children: [
                            Rozet(
                              switch (h.tur) { 'giris' => 'Giriş', 'uretim' => 'Üretim', _ => 'Satış' },
                              tur: switch (h.tur) { 'giris' => RozetTuru.mavi, 'uretim' => RozetTuru.basari, _ => RozetTuru.uyari },
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                                Text(h.aciklama),
                                const SizedBox(height: 2),
                                Row(children: [Soluk(tarih(h.tarih)), const SizedBox(width: 8), Flexible(child: Mono(h.numara, boyut: 12))]),
                              ]),
                            ),
                            const Icon(Icons.chevron_right),
                          ]),
                        ),
                      ),
                  ]),
          ),
        ]),
      ),
    );
  }
}

class _HizliIslem extends StatelessWidget {
  const _HizliIslem({required this.no, required this.renk, required this.baslik, required this.ikon, required this.onTap});

  final int no;
  final Color renk;
  final String baslik;
  final IconData ikon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Expanded(
        child: Card(
          clipBehavior: Clip.antiAlias,
          child: InkWell(
            onTap: onTap,
            child: Container(
              decoration: BoxDecoration(border: Border(top: BorderSide(color: renk, width: 4))),
              padding: const EdgeInsets.fromLTRB(12, 14, 12, 14),
              child: Column(children: [
                CircleAvatar(radius: 22, backgroundColor: renk, child: Icon(ikon, color: Colors.white)),
                const SizedBox(height: 8),
                Text(baslik, style: const TextStyle(fontWeight: FontWeight.w600), textAlign: TextAlign.center),
              ]),
            ),
          ),
        ),
      );
}
