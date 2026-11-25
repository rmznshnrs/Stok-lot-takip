"use client";

import { useEffect } from "react";

function kapat(kok: ParentNode) {
  kok.querySelectorAll<HTMLElement>("form, input:not([type=password]), textarea").forEach((e) => {
    if (e.getAttribute("autocomplete") !== "off") e.setAttribute("autocomplete", "off");
  });
}

export default function OnerisizGirdiler() {
  useEffect(() => {
    kapat(document);
    const gozcu = new MutationObserver((kayitlar) => {
      for (const k of kayitlar) k.addedNodes.forEach((d) => { if (d instanceof HTMLElement) { kapat(d); if (d.matches("form, input, textarea")) kapat(d.parentElement ?? d); } });
    });
    gozcu.observe(document.body, { subtree: true, childList: true });
    return () => gozcu.disconnect();
  }, []);
  return null;
}
