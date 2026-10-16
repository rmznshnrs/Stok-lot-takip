// GS1 karekod (ilaç kutularındaki gibi) çözümü: içindeki seri (21) veya lot (10) numarasını alır.
// Web'deki frontend/src/lib/gs1.ts ile aynı kurallar.

const _gs = '\u001d';

// Sabit uzunluklu AI'ler (değer uzunluğu); diğerleri değişken uzunlukta ve GS ile biter
const _sabit = {'00': 18, '01': 14, '02': 14, '11': 6, '13': 6, '15': 6, '17': 6, '20': 2};
// Değişken uzunluklu, bilinen AI'ler; uzun olan önce denenir
const _degisken = ['240', '241', '250', '10', '21', '22', '30', '37', '90', '91', '92', '93', '94', '95', '96', '97', '98', '99'];

/// Okutulan metni GS1 alanlarına ayırır; GS1 değilse null.
Map<String, String>? gs1Coz(String metin) {
  var m = metin.trim();
  if (m.startsWith('(')) {
    final alanlar = {
      for (final e in RegExp(r'\((\d{2,4})\)([^(]*)').allMatches(m)) e.group(1)!: e.group(2)!,
    };
    return alanlar.isEmpty ? null : alanlar;
  }
  final onekli = RegExp(r'^\][dQC][0-9]').hasMatch(m);
  final gtinIle = RegExp(r'^01\d{14}').hasMatch(m);
  if (!onekli && !gtinIle && !m.contains(_gs)) return null;
  if (onekli) m = m.substring(3);
  m = m.replaceFirst(RegExp('^$_gs+'), '');

  final alanlar = <String, String>{};
  while (m.isNotEmpty) {
    final sabit = _sabit.keys.where(m.startsWith).firstOrNull;
    if (sabit != null) {
      final son = (2 + _sabit[sabit]!).clamp(0, m.length);
      alanlar[sabit] = m.substring(2, son);
      m = m.substring(son).replaceFirst(RegExp('^$_gs'), '');
      continue;
    }
    final ai = _degisken.where(m.startsWith).firstOrNull;
    if (ai == null) break;
    final son = m.indexOf(_gs);
    alanlar[ai] = son < 0 ? m.substring(ai.length) : m.substring(ai.length, son);
    m = son < 0 ? '' : m.substring(son + 1);
  }
  return alanlar.isEmpty ? null : alanlar;
}

/// Metinden aranan numarayı çıkarır: GS1 karekodsa seri (21) veya lot (10), değilse metnin kendisi.
String numaraAyikla(String metin, {String? tercih}) {
  final alanlar = gs1Coz(metin);
  if (alanlar == null) return metin.trim();
  for (final ai in tercih == 'lot' ? ['10', '21'] : ['21', '10']) {
    final d = alanlar[ai];
    if (d != null && d.trim().isNotEmpty) return d.trim();
  }
  return metin.trim();
}
