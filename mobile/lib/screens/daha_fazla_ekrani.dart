import 'package:flutter/material.dart';

import '../main.dart';
import '../widgets/ortak.dart';
import 'ana_kabuk.dart';
import 'api_adresi_ekrani.dart';
import 'izlenebilirlik_ekrani.dart';
import 'parcalar_ekrani.dart';
import 'sifre_ekrani.dart';
import 'urunler_ekrani.dart';

class DahaFazlaEkrani extends StatelessWidget {
  const DahaFazlaEkrani({super.key});

  @override
  Widget build(BuildContext context) {
    final k = KullaniciBaglami.of(context).kullanici;
    final ayarlar = context.ayarlar;
    void ac(Widget sayfa) => Navigator.of(context).push(MaterialPageRoute(builder: (_) => sayfa));

    return Scaffold(
      appBar: const GecisliBar(baslik: 'Daha fazla'),
      body: ListView(padding: const EdgeInsets.all(16), children: [
        Kart(
          child: Row(children: [
            CircleAvatar(child: Text(k.ad.isEmpty ? '?' : k.ad[0])),
            const SizedBox(width: 12),
            Expanded(
              child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text('${k.ad} ${k.soyad}', style: const TextStyle(fontWeight: FontWeight.w600)),
                Soluk('${k.admin ? 'Yönetici' : 'Kullanıcı'} · ${k.kullaniciAdi}'),
              ]),
            ),
          ]),
        ),
        const BolumBasligi('Ekranlar'),
        _Oge(ikon: Icons.account_tree_outlined, baslik: 'İzlenebilirlik', onTap: () => ac(const IzlenebilirlikEkrani())),
        _Oge(ikon: Icons.category_outlined, baslik: 'Ürünler', onTap: () => ac(const UrunlerEkrani())),
        _Oge(ikon: Icons.memory_outlined, baslik: 'Parçalar', onTap: () => ac(const ParcalarEkrani())),
        const BolumBasligi('Ayarlar'),
        Card(
          child: SwitchListTile(
            secondary: const Icon(Icons.dark_mode_outlined),
            title: const Text('Koyu mod'),
            value: ayarlar.koyuTema,
            onChanged: ayarlar.koyuTemaKaydet,
          ),
        ),
        const SizedBox(height: 8),
        _Oge(ikon: Icons.lock_outline, baslik: 'Şifre değiştir', onTap: () => ac(const SifreEkrani())),
        _Oge(ikon: Icons.dns_outlined, baslik: 'Sunucu adresi', alt: ayarlar.apiAdresi, onTap: () => ac(const ApiAdresiEkrani())),
        _Oge(
          ikon: Icons.logout,
          baslik: 'Çıkış yap',
          tehlikeli: true,
          onTap: () => ayarlar.tokenKaydet(null),
        ),
      ]),
    );
  }
}

class _Oge extends StatelessWidget {
  const _Oge({required this.ikon, required this.baslik, required this.onTap, this.alt, this.tehlikeli = false});

  final IconData ikon;
  final String baslik;
  final String? alt;
  final VoidCallback onTap;
  final bool tehlikeli;

  @override
  Widget build(BuildContext context) {
    final renk = tehlikeli ? Theme.of(context).colorScheme.error : null;
    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Card(
        child: ListTile(
          minTileHeight: 56,
          leading: Icon(ikon, color: renk),
          title: Text(baslik, style: TextStyle(color: renk)),
          subtitle: alt == null ? null : Text(alt!),
          trailing: tehlikeli ? null : const Icon(Icons.chevron_right),
          onTap: onTap,
        ),
      ),
    );
  }
}
