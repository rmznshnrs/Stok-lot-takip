// Lot ve seri no sayfalarının adresleri: lot Stok'ta, seri no Üretim geçmişinde açılır.

export const lotAdresi = (lotNo: string) => `/stok?lot=${encodeURIComponent(lotNo)}`;
export const seriAdresi = (seriNo: string) => `/uretim?sekme=gecmis&seri=${encodeURIComponent(seriNo)}`;
