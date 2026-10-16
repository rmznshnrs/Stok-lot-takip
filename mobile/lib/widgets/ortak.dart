import 'package:flutter/material.dart';
import 'package:intl/intl.dart';

import '../services/api.dart';
import '../tema.dart';

final _tarihBicimi = DateFormat('dd.MM.yyyy', 'tr_TR');
final _tarihSaatBicimi = DateFormat('dd.MM.yyyy HH:mm', 'tr_TR');

String tarih(DateTime t) => _tarihBicimi.format(t);
// Türkiye saati (UTC+3, yaz saati yok); telefonun saat dilimi farklı olsa da aynı görünsün
String tarihSaat(DateTime t) => _tarihSaatBicimi.format(t.toUtc().add(const Duration(hours: 3)));

/// Hata metni: API hatasıysa onun mesajı.
String hataMetni(Object e) => e is ApiHatasi ? e.mesaj : 'Beklenmeyen bir hata oluştu.';

void bildir(BuildContext context, String metin, {bool hata = false}) {
  ScaffoldMessenger.of(context)
    ..hideCurrentSnackBar()
    ..showSnackBar(
      SnackBar(content: Text(metin), backgroundColor: hata ? Renk.uyari : null, behavior: SnackBarBehavior.floating),
    );
}

/// Üstte mor-turkuaz geçişli başlık çubuğu.
class GecisliBar extends StatelessWidget implements PreferredSizeWidget {
  const GecisliBar({super.key, required this.baslik, this.eylemler, this.alt});

  final String baslik;
  final List<Widget>? eylemler;
  final PreferredSizeWidget? alt;

  @override
  Size get preferredSize => Size.fromHeight(kToolbarHeight + (alt?.preferredSize.height ?? 0));

  @override
  Widget build(BuildContext context) => AppBar(
    title: Text(baslik),
    actions: eylemler,
    bottom: alt,
    // Container çocuksuz olduğunda alanı doldurur (DecoratedBox sıfır boyutta kalır)
    flexibleSpace: Container(decoration: const BoxDecoration(gradient: Renk.gecis)),
  );
}

/// Liste öğesi kartı.
class Kart extends StatelessWidget {
  const Kart({super.key, required this.child, this.onTap, this.uyari = false, this.padding = const EdgeInsets.all(16)});

  final Widget child;
  final VoidCallback? onTap;
  final bool uyari;
  final EdgeInsets padding;

  @override
  Widget build(BuildContext context) {
    final koyu = Theme.of(context).brightness == Brightness.dark;
    return Card(
      color: uyari ? (koyu ? const Color(0xFF2A1E15) : const Color(0xFFFDF4EC)) : null,
      shape: uyari
          ? RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(14),
              side: const BorderSide(color: Color(0xFFE0A06A)),
            )
          : null,
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(padding: padding, child: child),
      ),
    );
  }
}

class Rozet extends StatelessWidget {
  const Rozet(this.metin, {super.key, this.tur = RozetTuru.mavi});

  final String metin;
  final RozetTuru tur;

  @override
  Widget build(BuildContext context) {
    final (zemin, renk) = switch (tur) {
      RozetTuru.mavi => (Renk.morAcik, Renk.morKoyu),
      RozetTuru.uyari => (Renk.uyariZemin, Renk.uyari),
      RozetTuru.basari => (Renk.basariZemin, Renk.basari),
    };
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
      decoration: BoxDecoration(color: zemin, borderRadius: BorderRadius.circular(99)),
      child: Text(
        metin,
        style: TextStyle(color: renk, fontSize: 12, fontWeight: FontWeight.w500),
      ),
    );
  }
}

enum RozetTuru { mavi, uyari, basari }

/// Kod, lot ve seri numaraları için eşit aralıklı yazı.
class Mono extends StatelessWidget {
  const Mono(this.metin, {super.key, this.boyut = 14, this.kalin = false, this.renk});

  final String metin;
  final double boyut;
  final bool kalin;
  final Color? renk;

  @override
  Widget build(BuildContext context) => Text(
    metin,
    style: TextStyle(fontFamily: monoAile, fontSize: boyut, fontWeight: kalin ? FontWeight.w600 : FontWeight.w400, color: renk),
  );
}

class Soluk extends StatelessWidget {
  const Soluk(this.metin, {super.key, this.boyut = 13});

  final String metin;
  final double boyut;

  @override
  Widget build(BuildContext context) => Text(
    metin,
    style: TextStyle(fontSize: boyut, color: Theme.of(context).colorScheme.onSurfaceVariant),
  );
}

class BolumBasligi extends StatelessWidget {
  const BolumBasligi(this.metin, {super.key});

  final String metin;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.fromLTRB(4, 16, 4, 8),
    child: Text(
      metin.toUpperCase(),
      style: TextStyle(fontSize: 12, letterSpacing: 0.6, color: Theme.of(context).colorScheme.onSurfaceVariant),
    ),
  );
}

/// Mor-turkuaz geçişli ana düğme.
class AnaDugme extends StatelessWidget {
  const AnaDugme({super.key, required this.metin, required this.onPressed, this.bekliyor = false, this.ikon});

  final String metin;
  final VoidCallback? onPressed;
  final bool bekliyor;
  final IconData? ikon;

  @override
  Widget build(BuildContext context) {
    final aktif = onPressed != null && !bekliyor;
    return Opacity(
      opacity: aktif ? 1 : 0.5,
      child: DecoratedBox(
        decoration: BoxDecoration(gradient: Renk.dugmeGecis, borderRadius: BorderRadius.circular(12)),
        child: FilledButton.icon(
          style: FilledButton.styleFrom(
            backgroundColor: Colors.transparent,
            shadowColor: Colors.transparent,
            foregroundColor: Colors.white,
          ),
          onPressed: aktif ? onPressed : null,
          icon: bekliyor
              ? const SizedBox.square(dimension: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
              : Icon(ikon ?? Icons.check),
          label: Text(metin),
        ),
      ),
    );
  }
}

/// Yükleniyor / hata / içerik: Future sonucunu gösterir, hata olursa yeniden deneme sunar.
class Yukleyici<T> extends StatelessWidget {
  const Yukleyici({super.key, required this.future, required this.yenile, required this.builder});

  final Future<T>? future;
  final VoidCallback yenile;
  final Widget Function(BuildContext, T) builder;

  @override
  Widget build(BuildContext context) => FutureBuilder<T>(
    future: future,
    builder: (context, s) {
      if (s.hasError) return HataGorunumu(metin: hataMetni(s.error!), yenile: yenile);
      if (!s.hasData) {
        return const Center(
          child: Padding(padding: EdgeInsets.all(32), child: CircularProgressIndicator()),
        );
      }
      return builder(context, s.data as T);
    },
  );
}

class HataGorunumu extends StatelessWidget {
  const HataGorunumu({super.key, required this.metin, required this.yenile});

  final String metin;
  final VoidCallback yenile;

  @override
  Widget build(BuildContext context) => Center(
    child: Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          const Icon(Icons.cloud_off, size: 40, color: Renk.uyari),
          const SizedBox(height: 12),
          Text(metin, textAlign: TextAlign.center),
          const SizedBox(height: 12),
          OutlinedButton.icon(onPressed: yenile, icon: const Icon(Icons.refresh), label: const Text('Tekrar dene')),
        ],
      ),
    ),
  );
}

class BosDurum extends StatelessWidget {
  const BosDurum(this.metin, {super.key});

  final String metin;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.all(32),
    child: Center(child: Soluk(metin, boyut: 14)),
  );
}

/// Şifre girişi; sağdaki göz düğmesiyle yazılan şifre gösterilir / gizlenir.
class SifreAlani extends StatefulWidget {
  const SifreAlani({super.key, required this.denetleyici, required this.etiket, this.otomatikDoldurma, this.gonder});

  final TextEditingController denetleyici;
  final String etiket;
  final Iterable<String>? otomatikDoldurma;
  final ValueChanged<String>? gonder;

  @override
  State<SifreAlani> createState() => _SifreAlaniState();
}

class _SifreAlaniState extends State<SifreAlani> {
  bool _goster = false;

  @override
  Widget build(BuildContext context) => TextField(
    controller: widget.denetleyici,
    obscureText: !_goster,
    autofillHints: widget.otomatikDoldurma,
    onSubmitted: widget.gonder,
    decoration: InputDecoration(
      labelText: widget.etiket,
      suffixIcon: IconButton(
        tooltip: _goster ? 'Şifreyi gizle' : 'Şifreyi göster',
        icon: Icon(_goster ? Icons.visibility_off_outlined : Icons.visibility_outlined),
        onPressed: () => setState(() {
          _goster = !_goster;
        }),
      ),
    ),
  );
}

/// Metin kutusu + listeden seçme düğmesi (müşteri, tedarikçi). Listede olmayan da yazılabilir.
class ListedenSecilebilirAlan extends StatelessWidget {
  const ListedenSecilebilirAlan({super.key, required this.denetleyici, required this.etiket, required this.secenekler});

  final TextEditingController denetleyici;
  final String etiket;
  final List<String> secenekler;

  Future<void> _sec(BuildContext context) async {
    final secilen = await showModalBottomSheet<String>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (c) => _SecimListesi(baslik: etiket, secenekler: secenekler),
    );
    if (secilen != null) denetleyici.text = secilen;
  }

  @override
  Widget build(BuildContext context) => TextField(
    controller: denetleyici,
    decoration: InputDecoration(
      labelText: etiket,
      suffixIcon: secenekler.isEmpty
          ? null
          : IconButton(tooltip: 'Listeden seç', icon: const Icon(Icons.list), onPressed: () => _sec(context)),
    ),
  );
}

class _SecimListesi extends StatefulWidget {
  const _SecimListesi({required this.baslik, required this.secenekler});

  final String baslik;
  final List<String> secenekler;

  @override
  State<_SecimListesi> createState() => _SecimListesiState();
}

class _SecimListesiState extends State<_SecimListesi> {
  String _arama = '';

  @override
  Widget build(BuildContext context) {
    final liste = widget.secenekler.where((s) => s.toLowerCase().contains(_arama.toLowerCase())).toList();
    return SizedBox(
      height: MediaQuery.of(context).size.height * 0.7,
      child: Column(
        children: [
          Padding(
            padding: EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: TextField(
              autofocus: false,
              decoration: const InputDecoration(prefixIcon: Icon(Icons.search)),
              onChanged: (m) => setState(() {
                _arama = m;
              }),
            ),
          ),
          Expanded(
            child: ListView(
              children: [for (final s in liste) ListTile(title: Text(s), onTap: () => Navigator.pop(context, s))],
            ),
          ),
        ],
      ),
    );
  }
}
