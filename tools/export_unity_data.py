"""Exportiert alle Spieldaten des Browser-Prototyps nach unity/Assets/Resources/Data/gamedata.json.
Vorher: cd prototype/tests && python build_test_page.py   (erzeugt test.html mit Test-Hook __t)
Aufruf:  python tools/export_unity_data.py"""
import os,sys,json
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0,os.path.join(ROOT,'prototype','tests'))
from harness import game
OUT=os.path.join(ROOT,'unity','Assets','Resources','Data','gamedata.json')
with game() as (pg,errs):
  d=pg.evaluate("""()=>{const t=__t,r={};
   const strip=o=>JSON.parse(JSON.stringify(o));
   r.porder=t.PORDER;
   r.planets={};for(const pid of t.PORDER){const P=t.PLANETS[pid];r.planets[pid]=strip(Object.assign({},P,{theme:Object.assign({},P.theme)}));}
   r.dirt=strip(t.DIRT);r.kinds=strip(t.KINDS);r.mats=strip(t.MATS);r.raw=t.RAW;r.prods=t.PRODS;r.stages=strip(t.STAGES);
   r.upg=t.UPG.map(u=>{const o={id:u.id,loc:!!u.loc,cat:u.cat,name:u.name,base:u.base,grow:u.grow,max:u.max,stage:u.stage||0,res:u.res||'',hide:!!u.hide,done:!!u.done,rad:!!u.rad,label:u.label,vals:[],show:[]};for(let l=0;l<=u.max+12;l++){o.vals.push(u.val(l));o.show.push(u.show(u.val(l)));}return o;});
   r.vt=strip(t.VT);r.bld={};for(const id in t.BLD){const B=t.BLD[id];r.bld[id]={name:B.name,stage:B.stage,cost:B.cost,fp:B.fp,auto:!!B.auto,st:B.st||null,mach:!!B.mach,research:B.research||'',desc:B.desc,geo:!!B.geo};}
   r.border=t.BORDER;r.stations=t.STATIONS;
   r.mach={};for(const id in t.MACH){const M=t.MACH[id];r.mach[id]={energy:M.energy,inCap:M.inCap,outCap:M.outCap};}
   r.lines={};for(const pid of t.PORDER){const L=t.PL(pid);r.lines[pid]={names:L.names,presse:L.presse,schmelze:L.schmelze,raff:L.raff};}
   r.layouts={};for(const pid of t.PORDER){const L=t.layoutOf(pid);r.layouts[pid]={pos:L.pos,yaw:L.yaw,shop:L.shop,start:L.start,park:L.park,belts:L.belts.map(b=>({id:b.id,from:b.from,to:b.to,h:b.h,stage:b.stage||0,rp:b.rp,L:b.L,port0:b.port0,port1:b.port1}))};}
   r.cells={};for(const pid of t.PORDER){const pd=t.planetData(pid);r.cells[pid]={N:pd.N,R:pd.R,base:pd.base,start:pd.start,type:pd.cells.map(c=>Object.keys(t.DIRT).indexOf(c.type)),kind:pd.cells.map(c=>Object.keys(t.KINDS).indexOf(c.kind)),c0:pd.cells[0].c,c7:pd.cells[7].c,ring:pd.cells.map(c=>c.ring)};}
   r.dirtKeys=Object.keys(t.DIRT);r.kindKeys=Object.keys(t.KINDS);
   r.quests=t.QUESTS.map(q=>({t:q.t,r:q.r}));
   r.consts={GRID:t.GRID,DONE:t.DONE,handCap0:60,SOLAR_PEAK:t.SOLAR_PEAK};r.daylen=strip(t.DAYLEN);r.res=strip(t.RES);r.drone=strip(t.DRONE);r.stormLook=strip(t.STORM_LOOK);r.powreq=t.POWREQ;r.outpost=strip(t.OUTPOST);r.geof=strip(t.GEOF);
   return r;}""")
  json.dump(d,open(OUT,'w',encoding='utf-8'),ensure_ascii=False,separators=(',',':'))
  print(len(json.dumps(d)),errs[:2])
