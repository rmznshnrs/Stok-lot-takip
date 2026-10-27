import type { Metadata } from "next";
import { IBM_Plex_Sans, JetBrains_Mono, Montserrat } from "next/font/google";
import { TEMA_BETIGI } from "@/lib/tema";
import "./globals.css";

const plexSans = IBM_Plex_Sans({
  variable: "--font-plex-sans",
  subsets: ["latin", "latin-ext"],
  weight: ["400", "500", "600", "700"],
});

// Lot / seri numaraları, kodlar ve miktarlar için
const jetbrainsMono = JetBrains_Mono({
  variable: "--font-jetbrains-mono",
  subsets: ["latin", "latin-ext"],
  weight: ["400", "500", "600"],
});

// Yalnız firma adı (logo) için
const montserrat = Montserrat({
  variable: "--font-logo",
  subsets: ["latin", "latin-ext"],
  weight: ["600", "800"],
});

export const metadata: Metadata = {
  title: { default: "MKC Bilişim", template: "%s – MKC Bilişim" },
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    // data-tema, sayfa çizilmeden betikle atanır; sunucu çıktısıyla farkı beklenen bir durum
    <html lang="tr" className={`${plexSans.variable} ${jetbrainsMono.variable} ${montserrat.variable}`} suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: TEMA_BETIGI }} />
      </head>
      <body>{children}</body>
    </html>
  );
}
