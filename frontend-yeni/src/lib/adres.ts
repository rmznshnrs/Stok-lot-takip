// Numara sayfalarının adresleri: stok giriş no Stok'ta, seri no Müşteriler'de açılır.

export const lotAdresi = (lotNo: string) => `/stok?lot=${encodeURIComponent(lotNo)}`;
export const seriAdresi = (seriNo: string) => `/musteriler?seri=${encodeURIComponent(seriNo)}`;
