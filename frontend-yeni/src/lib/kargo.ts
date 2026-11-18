// Kargo firmaları ve takip sayfası adresleri. Takip no elle girilir; bağlantı firmanın kendi takip sayfasını açar.
// Listede olmayan firma yazılabilir; onun için bağlantı gösterilmez.

export const KARGO_FIRMALARI: { ad: string; adres: (no: string) => string }[] = [
  { ad: "Yurtiçi Kargo", adres: (no) => `https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code=${no}` },
  { ad: "Aras Kargo", adres: (no) => `https://kargotakip.araskargo.com.tr/mainpage.aspx?code=${no}` },
  { ad: "PTT Kargo", adres: (no) => `https://gonderitakip.ptt.gov.tr/Track/Verify?q=${no}` },
  { ad: "Sürat Kargo", adres: (no) => `https://www.suratkargo.com.tr/KargoTakip/?kargotakipno=${no}` },
  { ad: "UPS", adres: (no) => `https://www.ups.com/track?loc=tr_TR&tracknum=${no}` },
  { ad: "DHL", adres: (no) => `https://www.dhl.com/tr-tr/home/tracking.html?tracking-id=${no}` },
  { ad: "FedEx", adres: (no) => `https://www.fedex.com/fedextrack/?trknbr=${no}` },
];

/** Firma listede ve takip no varsa takip sayfası adresi; yoksa null. */
export function takipAdresi(firma: string, takipNo: string) {
  const f = KARGO_FIRMALARI.find((k) => k.ad.toLocaleLowerCase("tr") === firma.trim().toLocaleLowerCase("tr"));
  return f && takipNo.trim() ? f.adres(encodeURIComponent(takipNo.trim())) : null;
}
