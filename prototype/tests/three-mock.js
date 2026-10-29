(function(){function mk(){const fn=function(){};return new Proxy(fn,{get(t,k){if(k===Symbol.toPrimitive)return()=>0;if(k==='then')return undefined;if(k===Symbol.iterator)return function*(){};if(Object.prototype.hasOwnProperty.call(t,k))return t[k];const v=mk();t[k]=v;return v;},set(t,k,v){t[k]=v;return true},apply(){return mk()},construct(){return mk()}});}
window.THREE=mk();})();
