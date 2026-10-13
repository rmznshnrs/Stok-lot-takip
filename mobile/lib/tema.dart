import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// Web arayüzüyle aynı renkler (mor-turkuaz).
class Renk {
  static const mor = Color(0xFF6D28D9);
  static const morKoyu = Color(0xFF5B21B6);
  static const morAcik = Color(0xFFEEE7FD);
  static const turkuaz = Color(0xFF0EA5A4);
  static const pembe = Color(0xFFEC4899);
  static const zemin = Color(0xFFF4F2FB);
  static const kenar = Color(0xFFE4DFF3);
  static const metin = Color(0xFF1C1F24);
  static const soluk = Color(0xFF5B616B);
  static const uyari = Color(0xFF8A3B00);
  static const uyariZemin = Color(0xFFFDEBD9);
  static const basari = Color(0xFF047857);
  static const basariZemin = Color(0xFFD1FAE5);

  static const koyuZemin = Color(0xFF16131F);
  static const koyuKart = Color(0xFF201C2B);
  static const koyuKenar = Color(0xFF352F45);

  static const gecis = LinearGradient(colors: [Color(0xFF3B1E8F), mor, turkuaz], stops: [0, 0.55, 1]);
  static const dugmeGecis = LinearGradient(colors: [mor, turkuaz]);
}

const monoAile = 'IBMPlexMono';

ThemeData tema(Brightness parlaklik) {
  final koyu = parlaklik == Brightness.dark;
  final renkler = ColorScheme.fromSeed(
    seedColor: Renk.mor,
    brightness: parlaklik,
    primary: koyu ? const Color(0xFFA78BFA) : Renk.mor,
    secondary: Renk.turkuaz,
    surface: koyu ? Renk.koyuKart : Colors.white,
    error: koyu ? const Color(0xFFF0A868) : Renk.uyari,
  );
  final kenar = koyu ? Renk.koyuKenar : Renk.kenar;
  return ThemeData(
    useMaterial3: true,
    colorScheme: renkler,
    fontFamily: 'IBMPlexSans',
    scaffoldBackgroundColor: koyu ? Renk.koyuZemin : Renk.zemin,
    appBarTheme: const AppBarTheme(
      systemOverlayStyle: SystemUiOverlayStyle.light, // durum çubuğu simgeleri beyaz
      foregroundColor: Colors.white,
      backgroundColor: Colors.transparent,
      elevation: 0,
      titleTextStyle: TextStyle(fontFamily: 'IBMPlexSans', fontSize: 18, fontWeight: FontWeight.w600, color: Colors.white),
    ),
    cardTheme: CardThemeData(
      color: renkler.surface,
      elevation: 0,
      margin: EdgeInsets.zero,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: kenar),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: renkler.surface,
      contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: BorderSide(color: kenar),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: BorderSide(color: kenar),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(10),
        borderSide: BorderSide(color: renkler.primary, width: 2),
      ),
    ),
    filledButtonTheme: FilledButtonThemeData(
      style: FilledButton.styleFrom(
        minimumSize: const Size.fromHeight(54),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        textStyle: const TextStyle(fontFamily: 'IBMPlexSans', fontSize: 16, fontWeight: FontWeight.w600),
      ),
    ),
    outlinedButtonTheme: OutlinedButtonThemeData(
      style: OutlinedButton.styleFrom(
        minimumSize: const Size(48, 48),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      ),
    ),
    navigationBarTheme: NavigationBarThemeData(
      backgroundColor: renkler.surface,
      indicatorColor: koyu ? const Color(0xFF2E2250) : Renk.morAcik,
      height: 68,
    ),
    dividerTheme: DividerThemeData(color: kenar, space: 1),
  );
}
