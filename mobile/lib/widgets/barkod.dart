import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../tema.dart';
import 'ortak.dart';

/// Kamerayla barkod / karekod okur; okunan metni döndürür. Kamera yoksa veya izin
/// verilmezse null döner (çağıran elle girişe devam eder).
Future<String?> barkodOku(BuildContext context, {String baslik = 'Barkod okut'}) =>
    Navigator.of(context).push<String>(MaterialPageRoute(builder: (_) => _BarkodEkrani(baslik: baslik)));

class _BarkodEkrani extends StatefulWidget {
  const _BarkodEkrani({required this.baslik});

  final String baslik;

  @override
  State<_BarkodEkrani> createState() => _BarkodEkraniState();
}

class _BarkodEkraniState extends State<_BarkodEkrani> {
  final _kamera = MobileScannerController(detectionSpeed: DetectionSpeed.noDuplicates);
  bool _bitti = false;

  @override
  void dispose() {
    _kamera.dispose();
    super.dispose();
  }

  void _okundu(BarcodeCapture yakalanan) {
    if (_bitti) return;
    final deger = yakalanan.barcodes.map((b) => b.rawValue?.trim()).firstWhere((v) => v != null && v.isNotEmpty, orElse: () => null);
    if (deger == null) return;
    _bitti = true;
    Navigator.of(context).pop(deger);
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        backgroundColor: Colors.black,
        appBar: GecisliBar(baslik: widget.baslik, eylemler: [
          IconButton(tooltip: 'Fener', onPressed: () => _kamera.toggleTorch(), icon: const Icon(Icons.flashlight_on)),
        ]),
        body: Stack(children: [
          MobileScanner(
            controller: _kamera,
            onDetect: _okundu,
            errorBuilder: (context, hata) => Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text(
                  hata.errorCode == MobileScannerErrorCode.permissionDenied
                      ? 'Kamera izni verilmedi. Numarayı elle girebilirsiniz.'
                      : 'Kamera açılamadı. Numarayı elle girebilirsiniz.',
                  style: const TextStyle(color: Colors.white),
                  textAlign: TextAlign.center,
                ),
              ),
            ),
          ),
          Center(
            child: Container(
              width: 280,
              height: 180,
              decoration: BoxDecoration(border: Border.all(color: Renk.turkuaz, width: 3), borderRadius: BorderRadius.circular(16)),
            ),
          ),
          const Positioned(
            left: 0,
            right: 0,
            bottom: 40,
            child: Text('Barkodu çerçevenin içine getirin', textAlign: TextAlign.center, style: TextStyle(color: Colors.white, fontSize: 15)),
          ),
        ]),
      );
}

/// Metin kutusu + kamera düğmesi: elle yazılabilir ya da okutulabilir.
class OkutulabilirAlan extends StatelessWidget {
  const OkutulabilirAlan({
    super.key,
    required this.denetleyici,
    required this.etiket,
    this.ipucu,
    this.gonder,
    this.otomatikOdak = false,
  });

  final TextEditingController denetleyici;
  final String etiket;
  final String? ipucu;
  final ValueChanged<String>? gonder;
  final bool otomatikOdak;

  @override
  Widget build(BuildContext context) => TextField(
        controller: denetleyici,
        autofocus: otomatikOdak,
        textInputAction: TextInputAction.done,
        style: const TextStyle(fontFamily: monoAile, fontSize: 16),
        decoration: InputDecoration(
          labelText: etiket,
          hintText: ipucu,
          suffixIcon: IconButton(
            tooltip: 'Kamerayla okut',
            icon: const Icon(Icons.qr_code_scanner),
            onPressed: () async {
              final okunan = await barkodOku(context, baslik: etiket);
              if (okunan == null) return;
              denetleyici.text = okunan;
              gonder?.call(okunan);
            },
          ),
        ),
        onSubmitted: gonder,
      );
}
