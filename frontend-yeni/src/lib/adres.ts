// Numara sayfalarının adresleri: stok giriş no Stok'ta, seri no ve ürün lot no Üretim geçmişinde açılır.

export const lotAdresi = (lotNo: string) => `/stok?lot=${encodeURIComponent(lotNo)}`;
export const urunLotAdresi = (lotNo: string) => `/uretim?sekme=gecmis&ara=${encodeURIComponent(lotNo)}`;
export const seriAdresi = (seriNo: string) => `/uretim?sekme=gecmis&seri=${encodeURIComponent(seriNo)}`;
