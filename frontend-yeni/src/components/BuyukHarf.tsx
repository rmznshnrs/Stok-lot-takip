"use client";

import { useEffect } from "react";

const ATLA = "script, style, textarea, input, code, .mono, .yazi-normal, [contenteditable]";
const KELIME = /(^|[\s(“"«/·–—-])(\p{Ll})/gu;

function cevir(metin: string) {
  return metin.split(/(\s+)/).map((parca) => {
    if (/@|:\/\/|^www\./i.test(parca)) return parca;
    return parca.replace(KELIME, (_, once: string, harf: string) => once + harf.toLocaleUpperCase("tr"));
  }).join("");
}

const hazir = (e: Element) => Object.keys(e).some((k) => k.startsWith("__reactFiber$"));

function duzelt(dugum: Node) {
  if (dugum.nodeType === Node.TEXT_NODE) {
    const ebeveyn = dugum.parentElement;
    if (!ebeveyn || !hazir(ebeveyn) || ebeveyn.closest(ATLA)) return;
    const eski = dugum.nodeValue ?? "";
    const yeni = cevir(eski);
    if (yeni !== eski) dugum.nodeValue = yeni;
    return;
  }
  if (dugum.nodeType !== Node.ELEMENT_NODE || (dugum as Element).closest(ATLA)) return;
  const gezgin = document.createTreeWalker(dugum, NodeFilter.SHOW_TEXT);
  for (let d = gezgin.nextNode(); d; d = gezgin.nextNode()) duzelt(d);
}

export default function BuyukHarf() {
  useEffect(() => {
    duzelt(document.body);
    const gozcu = new MutationObserver((kayitlar) => {
      for (const k of kayitlar) {
        if (k.type === "characterData") duzelt(k.target);
        else k.addedNodes.forEach(duzelt);
      }
    });
    gozcu.observe(document.body, { subtree: true, childList: true, characterData: true });
    const zamanlayicilar = [50, 250, 800, 2000].map((ms) => window.setTimeout(() => duzelt(document.body), ms));
    return () => {
      gozcu.disconnect();
      zamanlayicilar.forEach((z) => window.clearTimeout(z));
    };
  }, []);
  return null;
}
