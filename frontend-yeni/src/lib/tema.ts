// Açık / koyu tema. Seçim tarayıcıda saklanır; seçim yoksa açık tema.

export type Tema = "acik" | "koyu";

const ANAHTAR = "tema";

/** Sayfa çizilmeden önce çalışır (layout'ta), böylece açılışta renk sıçraması olmaz. */
export const TEMA_BETIGI = `(function(){try{var t=localStorage.getItem("${ANAHTAR}");if(t!=="koyu")t="acik";document.documentElement.dataset.tema=t}catch(e){}})()`;

export function simdikiTema(): Tema {
  return document.documentElement.dataset.tema === "koyu" ? "koyu" : "acik";
}

export function temaUygula(tema: Tema) {
  document.documentElement.dataset.tema = tema;
  try {
    localStorage.setItem(ANAHTAR, tema);
  } catch {
    // Gizli pencere vb.: seçim yalnız bu sayfa için geçerli olur
  }
}
