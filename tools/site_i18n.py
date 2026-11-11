"""Builds the English dictionary (site/en.py) and the DE/EN switch into site/page.html.
Usage: python tools/site_i18n.py   (then: python tools/standalone.py site/page.html index.html)
The page stays German in its source; the script swaps text nodes, alt/aria-label texts, title and description."""
import json, re, sys, os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', 'site'))
from en import EN

P = os.path.join(os.path.dirname(__file__), '..', 'site', 'page.html')
s = open(P, encoding='utf-8').read()
A, B = '<!-- i18n:start -->', '<!-- i18n:end -->'
s = re.sub(re.escape(A) + '.*?' + re.escape(B) + '\n?', '', s, flags=re.S)

block = A + r'''
<button id="langBtn" type="button" aria-label="Sprache wechseln / switch language"><b data-l="de">DE</b><i>/</i><b data-l="en">EN</b></button>
<style>
#langBtn{position:fixed;top:calc(12px + env(safe-area-inset-top,0px));right:12px;z-index:50;display:flex;gap:4px;align-items:center;padding:7px 12px;border-radius:999px;
  border:1px solid var(--line);background:rgba(10,14,24,.82);backdrop-filter:blur(8px);-webkit-backdrop-filter:blur(8px);color:var(--ink-3);font:600 14px/1 var(--f-display);letter-spacing:.06em;cursor:pointer}
#langBtn b{font-weight:600}#langBtn b.on{color:var(--clean)}#langBtn i{font-style:normal;opacity:.5}
#langBtn:hover{border-color:var(--clean)}#langBtn:focus-visible{outline:2px solid var(--clean);outline-offset:2px}
</style>
<script>
/* Fabian 11.11.: ganze Webseite auf Deutsch und Englisch. Knopf oben rechts; Wahl bleibt gespeichert und gilt auch für die Spielseite. */
(function(){
  var EN=''' + json.dumps(EN, ensure_ascii=False) + r''';
  var orig=new Map(), attrs=['alt','aria-label'], K='lc-site-lang';
  function each(f){
    var w=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT,{acceptNode:function(n){var p=n.parentNode&&n.parentNode.nodeName;return (p==='SCRIPT'||p==='STYLE')?2:1;}});
    for(var n;(n=w.nextNode());) f(n);
  }
  function set(l){
    var en=l==='en';
    each(function(n){ if(!orig.has(n)) orig.set(n,n.nodeValue); var o=orig.get(n), t=o.trim(); if(!t) return;
      n.nodeValue = en&&EN[t]!=null ? o.replace(t,EN[t]) : o; });
    document.querySelectorAll('[alt],[aria-label]').forEach(function(e){ if(e.id==='langBtn') return; attrs.forEach(function(a){ if(!e.hasAttribute(a)) return;
      var k='data-de-'+a; if(!e.hasAttribute(k)) e.setAttribute(k,e.getAttribute(a)); var o=e.getAttribute(k); e.setAttribute(a, en&&EN[o]!=null?EN[o]:o); }); });
    var m=document.querySelector('meta[name=description]'); if(m){ if(!m.dataset.de) m.dataset.de=m.content; m.content=en&&EN[m.dataset.de]?EN[m.dataset.de]:m.dataset.de; }
    document.documentElement.lang=l;
    document.querySelectorAll('#langBtn b').forEach(function(b){ b.classList.toggle('on',b.dataset.l===l); });
    try{ localStorage.setItem(K,l); }catch(e){}
    cur=l;
  }
  var cur, saved=null; try{ saved=localStorage.getItem(K); }catch(e){}
  set(saved||(/^de/i.test(navigator.language||'de')?'de':'en'));
  document.getElementById('langBtn').onclick=function(){ set(cur==='en'?'de':'en'); };
})();
</script>
''' + B + '\n'
i = s.rindex('</footer>') + len('</footer>')
s = s[:i] + '\n\n' + block + s[i:]
open(P, 'w', encoding='utf-8').write(s)
print('ok', len(EN), 'texts')
