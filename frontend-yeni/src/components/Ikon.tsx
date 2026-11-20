const CIZIMLER: Record<string, React.ReactNode> = {
  "/": <><path d="M3 11l9-7 9 7" /><path d="M5 10v10h14V10" /><path d="M10 20v-6h4v6" /></>,
  "/siparisler": <><path d="M8 4h8l1 3H7l1-3z" /><rect x="5" y="7" width="14" height="13" rx="2" /><path d="M9 12h6M9 16h4" /></>,
  "/uretim": <><circle cx="12" cy="12" r="3" /><path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1" /></>,
  "/stok": <><path d="M3 7l9-4 9 4-9 4-9-4z" /><path d="M3 7v10l9 4 9-4V7" /><path d="M12 11v10" /></>,
  "/sevkiyat": <><path d="M3 6h11v10H3z" /><path d="M14 9h4l3 3v4h-7" /><circle cx="7" cy="18" r="1.8" /><circle cx="17" cy="18" r="1.8" /></>,
  "/takvim": <><rect x="4" y="5" width="16" height="15" rx="2" /><path d="M8 3v4M16 3v4M4 10h16" /><path d="M8 14h2M12 14h2M8 17h2" /></>,
  "/musteriler": <><circle cx="9" cy="8" r="3.5" /><path d="M2.5 20c.8-3.5 3.4-5.5 6.5-5.5s5.7 2 6.5 5.5" /><path d="M16 4.5a3.5 3.5 0 010 7M18 14.5c2 .7 3.2 2.6 3.5 5.5" /></>,
  "/tedarikciler": <><path d="M3 21V9l6 4V9l6 4V5l6 4v12z" /><path d="M7 17h2M13 17h2" /></>,
  "/urunler": <><rect x="4" y="4" width="16" height="16" rx="2" /><path d="M9 4v6l3-2 3 2V4" /></>,
  "/parcalar": <><rect x="7" y="7" width="10" height="10" rx="1" /><path d="M10 3v4M14 3v4M10 17v4M14 17v4M3 10h4M3 14h4M17 10h4M17 14h4" /></>,
  "/yonetim": <><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.6 1.6 0 00.3 1.8l.1.1a2 2 0 11-2.8 2.8l-.1-.1a1.6 1.6 0 00-2.7 1.1V21a2 2 0 11-4 0v-.1a1.6 1.6 0 00-2.7-1.1l-.1.1a2 2 0 11-2.8-2.8l.1-.1A1.6 1.6 0 003 15.4H3a2 2 0 110-4h.1a1.6 1.6 0 001.1-2.7l-.1-.1a2 2 0 112.8-2.8l.1.1A1.6 1.6 0 009.6 4.6V4.5a2 2 0 114 0v.1a1.6 1.6 0 002.7 1.1l.1-.1a2 2 0 112.8 2.8l-.1.1a1.6 1.6 0 001.1 2.7h.3a2 2 0 110 4h-.1a1.6 1.6 0 00-1.1.8z" /></>,
  profil: <><circle cx="12" cy="8" r="4" /><path d="M4 21c1.2-4 4.3-6 8-6s6.8 2 8 6" /></>,
  ay: <path d="M20 14.5A8 8 0 019.5 4a8 8 0 1010.5 10.5z" />,
  gunes: <><circle cx="12" cy="12" r="4" /><path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4" /></>,
  cikis: <><path d="M15 4h3a2 2 0 012 2v12a2 2 0 01-2 2h-3" /><path d="M10 8l-4 4 4 4M6 12h10" /></>,
};

export default function Ikon({ ad }: { ad: string }) {
  return (
    <svg className="ikon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"
      strokeLinejoin="round" aria-hidden="true">
      {CIZIMLER[ad] ?? CIZIMLER["/"]}
    </svg>
  );
}
