export type Tema = "acik" | "koyu";

const ANAHTAR = "tema";

export const TEMA_BETIGI = `(function(){try{var t=localStorage.getItem("${ANAHTAR}");if(t!=="koyu")t="acik";document.documentElement.dataset.tema=t}catch(e){}})()`;

export function simdikiTema(): Tema {
  return document.documentElement.dataset.tema === "koyu" ? "koyu" : "acik";
}

export function temaUygula(tema: Tema) {
  document.documentElement.dataset.tema = tema;
  try {
    localStorage.setItem(ANAHTAR, tema);
  } catch {
  }
}
