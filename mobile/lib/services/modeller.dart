// API'nin döndürdüğü veriler (JSON → Dart).

typedef J = Map<String, dynamic>;

DateTime _tarih(dynamic v) => DateTime.parse(v as String);
List<String> _metinler(dynamic v) => (v as List).cast<String>();

/// "2026-03-01" biçimi (API'nin tarih alanları).
String tarihMetni(DateTime t) =>
    '${t.year.toString().padLeft(4, '0')}-${t.month.toString().padLeft(2, '0')}-${t.day.toString().padLeft(2, '0')}';

class Kullanici {
  Kullanici.json(J j) : id = j['id'], ad = j['ad'], soyad = j['soyad'], kullaniciAdi = j['kullaniciAdi'], rol = j['rol'];
  final int id;
  final String ad, soyad, kullaniciAdi, rol;
  bool get admin => rol == 'Admin';
}

class ParcaOzet {
  ParcaOzet.json(J j) : id = j['id'], kod = j['kod'], ad = j['ad'], birim = j['birim'];
  final int id;
  final String kod, ad, birim;
}

class Lot {
  Lot.json(J j)
    : id = j['id'],
      lotNo = j['lotNo'],
      parcaId = j['parcaId'],
      parcaKod = j['parcaKod'],
      parcaAd = j['parcaAd'],
      tedarikciAd = j['tedarikciAd'],
      siparisTarihi = _tarih(j['siparisTarihi']),
      siparisNo = j['siparisNo'] ?? '',
      girisAdet = j['girisAdet'],
      kalanAdet = j['kalanAdet'],
      geriCagrildi = j['geriCagrildi'],
      girisZamani = _tarih(j['girisZamani']),
      sirada = j['sirada'] ?? false;
  final int id, parcaId, girisAdet, kalanAdet;
  final String lotNo, parcaKod, parcaAd, tedarikciAd, siparisNo;
  final DateTime siparisTarihi, girisZamani;
  final bool geriCagrildi, sirada;
}

class SatisBilgisi {
  SatisBilgisi.json(J j) : tarih = _tarih(j['tarih']), musteriAd = j['musteriAd'], musteriIletisim = j['musteriIletisim'] ?? '';
  final DateTime tarih;
  final String musteriAd, musteriIletisim;
}

class ParcaStok {
  ParcaStok.json(J j)
    : id = j['id'],
      kod = j['kod'],
      ad = j['ad'],
      birim = j['birim'],
      minStok = j['minStok'],
      toplamStok = j['toplamStok'],
      kullanilabilirStok = j['kullanilabilirStok'],
      lotSayisi = j['lotSayisi'],
      kullanildigiUrunler = _metinler(j['kullanildigiUrunler']),
      minAltinda = j['minAltinda'];
  final int id, minStok, toplamStok, kullanilabilirStok, lotSayisi;
  final String kod, ad, birim;
  final List<String> kullanildigiUrunler;
  final bool minAltinda;
}

class StokGrubu {
  StokGrubu.json(J j)
    : parca = ParcaStok.json(j['parca']),
      lotlar = [for (final l in j['lotlar'] as List) Lot.json(l)],
      siradaki = j['siradaki'] == null ? null : Lot.json(j['siradaki']);
  final ParcaStok parca;
  final List<Lot> lotlar;
  final Lot? siradaki;
}

class ParcaIhtiyaci {
  ParcaIhtiyaci.json(J j)
    : parca = ParcaOzet.json(j['parca']),
      gereken = j['gereken'],
      kullanilabilir = j['kullanilabilir'],
      eksik = j['eksik'],
      lotlar = [for (final l in j['lotlar'] as List) (lotNo: l['lotNo'] as String, adet: l['adet'] as int)];
  final ParcaOzet parca;
  final int gereken, kullanilabilir, eksik;
  final List<({String lotNo, int adet})> lotlar;
}

class UretimOnizleme {
  UretimOnizleme.json(J j)
    : urunId = j['urunId'],
      urunAd = j['urunAd'],
      adet = j['adet'],
      parcalar = [for (final p in j['parcalar'] as List) ParcaIhtiyaci.json(p)],
      seriNolar = _metinler(j['seriNolar']),
      yeterli = j['yeterli'],
      atlananLotlar = _metinler(j['atlananLotlar']);
  final int urunId, adet;
  final String urunAd;
  final List<ParcaIhtiyaci> parcalar;
  final List<String> seriNolar, atlananLotlar;
  final bool yeterli;
}

/// "Kayip" → "Kayıp"
String hurdaAdi(String tur) => tur == 'Kayip' ? 'Kayıp' : 'Hurda';

/// Depodaki seri no'nun hurdaya / kayba ayrılma bilgisi. Tur: "Hurda" veya "Kayip".
class HurdaBilgisi {
  HurdaBilgisi.json(J j) : tur = j['tur'], aciklama = j['aciklama'], zaman = _tarih(j['zaman']), kullaniciAd = j['kullaniciAd'];
  final String tur, aciklama, kullaniciAd;
  final DateTime zaman;
  String get ad => hurdaAdi(tur);
}

class LotIzUretim {
  LotIzUretim.json(J j)
    : seriNo = j['seriNo'],
      urunAd = j['urunAd'],
      adet = j['adet'],
      satis = j['satis'] == null ? null : SatisBilgisi.json(j['satis']),
      hurdaTuru = j['hurdaTuru'];
  final String seriNo, urunAd;
  final int adet;
  final SatisBilgisi? satis;
  final String? hurdaTuru;
}

class LotIzMusteri {
  LotIzMusteri.json(J j)
    : ad = j['ad'],
      iletisim = j['iletisim'] ?? '',
      satislar = [for (final s in j['satislar'] as List) (seriNo: s['seriNo'] as String, tarih: _tarih(s['tarih']))];
  final String ad, iletisim;
  final List<({String seriNo, DateTime tarih})> satislar;
}

class LotIz {
  LotIz.json(J j)
    : lot = Lot.json(j['lot']),
      kullanilanAdet = j['kullanilanAdet'],
      uretimler = [for (final u in j['uretimler'] as List) LotIzUretim.json(u)],
      musteriler = [for (final m in j['musteriler'] as List) LotIzMusteri.json(m)];
  final Lot lot;
  final int kullanilanAdet;
  final List<LotIzUretim> uretimler;
  final List<LotIzMusteri> musteriler;
  List<LotIzUretim> get depodakiler => [
    for (final u in uretimler)
      if (u.satis == null && u.hurdaTuru == null) u,
  ];
}

class SeriIz {
  SeriIz.json(J j)
    : seriNo = j['seriNo'],
      tarih = _tarih(j['tarih']),
      urunAd = j['urunAd'],
      parcalar = [
        for (final p in j['parcalar'] as List)
          (parca: ParcaOzet.json(p['parca']), lot: Lot.json(p['lot']), adet: p['adet'] as int),
      ],
      satis = j['satis'] == null ? null : SatisBilgisi.json(j['satis']),
      geriCagrilanLotlar = _metinler(j['geriCagrilanLotlar']),
      hurda = j['hurda'] == null ? null : HurdaBilgisi.json(j['hurda']);
  final String seriNo, urunAd;
  final DateTime tarih;
  final List<({ParcaOzet parca, Lot lot, int adet})> parcalar;
  final SatisBilgisi? satis;
  final List<String> geriCagrilanLotlar;
  final HurdaBilgisi? hurda;
}

/// Üretim geçmişinde bir seri no. Satılmadıysa musteriAd / satisTarihi, ayrılmadıysa hurda null.
class UretimGecmisi {
  UretimGecmisi.json(J j)
    : uretimId = j['uretimId'],
      seriNo = j['seriNo'],
      urunId = j['urunId'],
      urunAd = j['urunAd'],
      tarih = _tarih(j['tarih']),
      musteriAd = j['musteriAd'],
      satisTarihi = j['satisTarihi'] == null ? null : _tarih(j['satisTarihi']),
      geriCagrilanLotlar = _metinler(j['geriCagrilanLotlar']),
      hurda = j['hurda'] == null ? null : HurdaBilgisi.json(j['hurda']);
  final int uretimId, urunId;
  final String seriNo, urunAd;
  final DateTime tarih;
  final String? musteriAd;
  final DateTime? satisTarihi;
  final List<String> geriCagrilanLotlar;
  final HurdaBilgisi? hurda;
  bool get depoda => musteriAd == null && hurda == null;
}

class NumaraArama {
  NumaraArama.json(J j)
    : tur = j['tur'],
      deger = j['deger'],
      adaylar = [for (final a in j['adaylar'] as List) (tur: a['tur'] as String, deger: a['deger'] as String)];
  final String? tur, deger;
  final List<({String tur, String deger})> adaylar;
}

class Hareket {
  Hareket.json(J j)
    : tur = j['tur'],
      tarih = _tarih(j['tarih']),
      aciklama = j['aciklama'],
      numara = j['numara'],
      numaraIlk = j['numaraIlk'],
      numaraTuru = j['numaraTuru'];
  final String tur, aciklama, numara, numaraIlk, numaraTuru;
  final DateTime tarih;
}

class Satilabilir {
  Satilabilir.json(J j) : seriNo = j['seriNo'], urunAd = j['urunAd'], geriCagrilanLotlar = _metinler(j['geriCagrilanLotlar']);
  final String seriNo, urunAd;
  List<String> geriCagrilanLotlar;
}

class SatisUyarisi {
  SatisUyarisi.json(J j) : seriNo = j['seriNo'], lotlar = _metinler(j['lotlar']);
  final String seriNo;
  final List<String> lotlar;
}

class SatisGecmisi {
  SatisGecmisi.json(J j)
    : tarih = _tarih(j['tarih']),
      musteriAd = j['musteriAd'],
      urunAd = j['urunAd'],
      seriNolar = _metinler(j['seriNolar']);
  final DateTime tarih;
  final String musteriAd, urunAd;
  final List<String> seriNolar;
}

class UrunListe {
  UrunListe.json(J j)
    : id = j['id'],
      kod = j['kod'],
      ad = j['ad'],
      seriOneki = j['seriOneki'],
      parcaCesidi = j['parcaCesidi'],
      uretimSayisi = j['uretimSayisi'];
  final int id, parcaCesidi, uretimSayisi;
  final String kod, ad, seriOneki;
}

class AgacSatiri {
  AgacSatiri({
    required this.parcaId,
    required this.parcaKod,
    required this.parcaAd,
    required this.adet,
    required this.kullanilabilirStok,
    required this.minAltinda,
  });
  AgacSatiri.json(J j)
    : parcaId = j['parcaId'],
      parcaKod = j['parcaKod'],
      parcaAd = j['parcaAd'],
      adet = j['adet'],
      kullanilabilirStok = j['kullanilabilirStok'],
      minAltinda = j['minAltinda'];
  final int parcaId, kullanilabilirStok;
  final String parcaKod, parcaAd;
  final bool minAltinda;
  int adet;
}

class UrunDetay {
  UrunDetay.json(J j)
    : id = j['id'],
      kod = j['kod'],
      ad = j['ad'],
      seriOneki = j['seriOneki'],
      seriOnekiDegistirilebilir = j['seriOnekiDegistirilebilir'],
      agac = [for (final s in j['agac'] as List) AgacSatiri.json(s)];
  final int id;
  final String kod, ad, seriOneki;
  final bool seriOnekiDegistirilebilir;
  final List<AgacSatiri> agac;
}
