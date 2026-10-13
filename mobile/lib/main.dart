import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/date_symbol_data_local.dart';

import 'screens/ana_kabuk.dart';
import 'screens/giris_ekrani.dart';
import 'services/api.dart';
import 'services/ayarlar.dart';
import 'tema.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await initializeDateFormatting('tr_TR');
  final ayarlar = await Ayarlar.yukle();
  runApp(Uygulama(ayarlar: ayarlar, api: Api(ayarlar)));
}

/// Ekranların ortak kullandığı ayarlar ve API.
class Baglam extends InheritedWidget {
  const Baglam({super.key, required this.ayarlar, required this.api, required super.child});

  final Ayarlar ayarlar;
  final Api api;

  // Ayarlar ve API hiç değişmez; bağımlılık kaydetmeye gerek yok (initState'te de kullanılabilir)
  static Baglam of(BuildContext context) => context.getInheritedWidgetOfExactType<Baglam>()!;

  @override
  bool updateShouldNotify(Baglam oldWidget) => false;
}

extension BaglamUzantisi on BuildContext {
  Api get api => Baglam.of(this).api;
  Ayarlar get ayarlar => Baglam.of(this).ayarlar;
}

class Uygulama extends StatelessWidget {
  const Uygulama({super.key, required this.ayarlar, required this.api});

  final Ayarlar ayarlar;
  final Api api;

  @override
  Widget build(BuildContext context) => Baglam(
    ayarlar: ayarlar,
    api: api,
    // Token silinince (çıkış, süre dolması) giriş ekranına, tema değişince yeniden çizilir
    child: ListenableBuilder(
      listenable: ayarlar,
      builder: (context, _) => MaterialApp(
        title: 'MKC Bilişim',
        debugShowCheckedModeBanner: false,
        theme: tema(Brightness.light),
        darkTheme: tema(Brightness.dark),
        themeMode: ayarlar.koyuTema ? ThemeMode.dark : ThemeMode.light,
        locale: const Locale('tr', 'TR'),
        supportedLocales: const [Locale('tr', 'TR')],
        localizationsDelegates: GlobalMaterialLocalizations.delegates,
        home: ayarlar.girisYapildi ? const AnaKabuk() : const GirisEkrani(),
      ),
    ),
  );
}
