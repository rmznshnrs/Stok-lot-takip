export const lotAdresi = (lotNo: string) => `/stok?lot=${encodeURIComponent(lotNo)}`;
export const seriAdresi = (seriNo: string) => `/musteriler?seri=${encodeURIComponent(seriNo)}`;
