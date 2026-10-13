import 'package:flutter/material.dart';

import '../main.dart';
import '../services/modeller.dart';
import '../widgets/ortak.dart';
import 'ana_sayfa.dart';
import 'daha_fazla_ekrani.dart';
import 'satis_ekrani.dart';
import 'stok_ekrani.dart';
import 'uretim_ekrani.dart';

/// Giriş yapmış kullanıcı (rol: yönetici işlemleri).
class KullaniciBaglami extends InheritedWidget {
  const KullaniciBaglami({super.key, required this.kullanici, required this.sekmeyeGit, required super.child});

  final Kullanici kullanici;
  final void Function(int sekme) sekmeyeGit;

  static KullaniciBaglami of(BuildContext context) => context.dependOnInheritedWidgetOfExactType<KullaniciBaglami>()!;

  @override
  bool updateShouldNotify(KullaniciBaglami oldWidget) => kullanici != oldWidget.kullanici;
}

class Sekme {
  static const ana = 0, stok = 1, uretim = 2, satis = 3, daha = 4;
}

/// Alt menülü 5 sekme: Ana, Stok, Üretim, Satış, Daha fazla.
class AnaKabuk extends StatefulWidget {
  const AnaKabuk({super.key});

  @override
  State<AnaKabuk> createState() => _AnaKabukState();
}

class _AnaKabukState extends State<AnaKabuk> {
  late Future<Kullanici> _kullanici = context.api.ben();
  int _sekme = Sekme.ana;

  // Her sekmenin kendi gezinme yığını; sekme değişince geçmiş korunur
  final _gezginler = List.generate(5, (_) => GlobalKey<NavigatorState>());

  static const _ekranlar = <Widget>[AnaSayfa(), StokEkrani(), UretimEkrani(), SatisEkrani(), DahaFazlaEkrani()];

  void _git(int sekme) {
    if (sekme == _sekme) {
      _gezginler[sekme].currentState?.popUntil((r) => r.isFirst);
    }
    setState(() => _sekme = sekme);
  }

  @override
  Widget build(BuildContext context) => FutureBuilder<Kullanici>(
        future: _kullanici,
        builder: (context, s) {
          if (s.hasError) {
            return Scaffold(
              body: HataGorunumu(
                metin: hataMetni(s.error!),
                yenile: () => setState(() => _kullanici = context.api.ben()),
              ),
            );
          }
          if (!s.hasData) return const Scaffold(body: Center(child: CircularProgressIndicator()));
          return KullaniciBaglami(
            kullanici: s.data!,
            sekmeyeGit: _git,
            child: PopScope(
              // Geri tuşu önce sekmenin içindeki sayfayı kapatır
              canPop: false,
              onPopInvokedWithResult: (bitti, _) async {
                if (bitti) return;
                final gezgin = _gezginler[_sekme].currentState;
                if (gezgin != null && gezgin.canPop()) {
                  gezgin.pop();
                } else if (_sekme != Sekme.ana) {
                  setState(() => _sekme = Sekme.ana);
                } else {
                  Navigator.of(context).maybePop();
                }
              },
              child: Scaffold(
                body: IndexedStack(
                  index: _sekme,
                  children: [
                    for (var i = 0; i < _ekranlar.length; i++)
                      Navigator(
                        key: _gezginler[i],
                        onGenerateRoute: (_) => MaterialPageRoute(builder: (_) => _ekranlar[i]),
                      ),
                  ],
                ),
                bottomNavigationBar: NavigationBar(
                  selectedIndex: _sekme,
                  onDestinationSelected: _git,
                  destinations: const [
                    NavigationDestination(icon: Icon(Icons.home_outlined), selectedIcon: Icon(Icons.home), label: 'Ana'),
                    NavigationDestination(icon: Icon(Icons.inventory_2_outlined), selectedIcon: Icon(Icons.inventory_2), label: 'Stok'),
                    NavigationDestination(icon: Icon(Icons.precision_manufacturing_outlined), selectedIcon: Icon(Icons.precision_manufacturing), label: 'Üretim'),
                    NavigationDestination(icon: Icon(Icons.point_of_sale_outlined), selectedIcon: Icon(Icons.point_of_sale), label: 'Satış'),
                    NavigationDestination(icon: Icon(Icons.menu), label: 'Daha fazla'),
                  ],
                ),
              ),
            ),
          );
        },
      );
}
