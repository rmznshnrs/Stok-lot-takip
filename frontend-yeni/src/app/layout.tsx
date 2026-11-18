import type { Metadata } from "next";
import { Montserrat, Nunito } from "next/font/google";
import { TEMA_BETIGI } from "@/lib/tema";
import "./globals.css";

const nunito = Nunito({
  variable: "--font-govde",
  subsets: ["latin", "latin-ext"],
  weight: ["400", "600", "700", "800"],
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
    <html lang="tr" className={`${nunito.variable} ${montserrat.variable}`} suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: TEMA_BETIGI }} />
      </head>
      <body>{children}</body>
    </html>
  );
}
