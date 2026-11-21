"use client";

import { useEffect } from "react";

type Yer = { x: number; w: number; h: number; y: number };

const sonYerler = new Map<string, Yer>();

function anahtar(kap: Element) {
  return kap.getAttribute("aria-label") ?? "";
}

function yerlestir(kap: HTMLElement) {
  if (!kap.classList.contains("sekmeler--kayan")) kap.classList.add("sekmeler--kayan");
  const aktif = kap.querySelector<HTMLElement>(".sekme--aktif, [aria-checked='true'].sekme");
  let isaret = kap.querySelector<HTMLElement>(":scope > .sekme-isaret");
  if (!aktif) {
    if (isaret) isaret.style.opacity = "0";
    return;
  }
  const yer: Yer = { x: aktif.offsetLeft, y: aktif.offsetTop, w: aktif.offsetWidth, h: aktif.offsetHeight };
  if (!isaret) {
    isaret = document.createElement("span");
    isaret.className = "sekme-isaret";
    isaret.setAttribute("aria-hidden", "true");
    kap.prepend(isaret);
    const eski = sonYerler.get(anahtar(kap));
    const bas = eski ?? yer;
    isaret.style.transition = "none";
    isaret.style.transform = `translate(${bas.x}px, ${bas.y}px)`;
    isaret.style.width = `${bas.w}px`;
    isaret.style.height = `${bas.h}px`;
    void isaret.offsetWidth;
    isaret.style.transition = "";
  }
  const simdi = isaret.dataset.yer;
  const yeni = `${yer.x},${yer.y},${yer.w},${yer.h}`;
  if (simdi !== yeni) {
    isaret.dataset.yer = yeni;
    isaret.style.transform = `translate(${yer.x}px, ${yer.y}px)`;
    isaret.style.width = `${yer.w}px`;
    isaret.style.height = `${yer.h}px`;
  }
  isaret.style.opacity = "1";
  sonYerler.set(anahtar(kap), yer);
}

export default function SekmeKaydiraci() {
  useEffect(() => {
    let bekliyor = 0;
    const hepsi = () => {
      bekliyor = 0;
      document.querySelectorAll<HTMLElement>(".sekmeler").forEach(yerlestir);
    };
    const zamanla = () => { if (!bekliyor) bekliyor = window.setTimeout(hepsi, 16); };
    hepsi();
    const gozcu = new MutationObserver(zamanla);
    gozcu.observe(document.body, { subtree: true, childList: true, attributes: true, attributeFilter: ["class", "aria-checked"] });
    window.addEventListener("resize", zamanla);
    return () => {
      gozcu.disconnect();
      window.removeEventListener("resize", zamanla);
      window.clearTimeout(bekliyor);
    };
  }, []);
  return null;
}
