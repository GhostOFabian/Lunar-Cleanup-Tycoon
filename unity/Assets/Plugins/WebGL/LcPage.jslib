// Fabian 11.11.: Die Spielseite (Feedback, Knöpfe, Ladetext) folgt der Spielsprache. Das Spiel meldet "de" oder "en" an window.lcSetLang.
mergeInto(LibraryManager.library, {
  LcPageLang: function (p) { try { var l = UTF8ToString(p); if (typeof window.lcSetLang === 'function') window.lcSetLang(l); else window.lcLang = l; } catch (e) {} }
});
