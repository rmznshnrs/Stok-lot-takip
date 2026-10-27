import type { Metadata } from "next";
import { Montserrat, Nunito } from "next/font/google";
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
    <html lang="tr" className={`${nunito.variable} ${montserrat.variable}`}>
      <body>{children}</body>
    </html>
  );
}
