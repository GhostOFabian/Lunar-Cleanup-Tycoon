"""Checks the base layout on all 10 planets: no building overlaps (>= 2 m gap), docks/approach points free,
belts do not cross foreign buildings, every point lies on base cells, every navigation target is reachable."""
import sys
from harness import game, show

with game() as (pg, errs):
    r = pg.evaluate("""()=>{const t=__t,B=t.BLD,ids=Object.keys(B),issues=[];
      for(let i=0;i<ids.length;i++)for(let j=i+1;j<ids.length;j++){const a=B[ids[i]],b=B[ids[j]],d=Math.hypot(a.pos[0]-b.pos[0],a.pos[1]-b.pos[1]);if(d<a.fp+b.fp+2)issues.push(ids[i]+'-'+ids[j]+' gap '+(d-a.fp-b.fp).toFixed(1));}
      for(const s of t.STATIONS){const pts=[];for(let k=0;k<B[s].st.docks+2;k++)pts.push(t.dockPos(s,k));pts.push(t.approachPos(s));
        for(const p of pts)for(const id of ids){if(id===s)continue;const d=Math.hypot(p[0]-B[id].pos[0],p[1]-B[id].pos[1]);if(d<B[id].fp+1.5)issues.push(s+' dock near '+id);}}
      for(const bl of t.BELTS){const P=bl.rp;for(let i=0;i<P.length-1;i++)for(let k=0;k<=4;k++){const x=P[i][0]+(P[i+1][0]-P[i][0])*k/4,y=P[i][1]+(P[i+1][1]-P[i][1])*k/4;
        for(const id of ids){if(id===bl.from||id===bl.to)continue;if(Math.hypot(x-B[id].pos[0],y-B[id].pos[1])<B[id].fp+.8)issues.push(bl.id+' through '+id);}}}
      const off=[];for(const pid of t.PORDER){const pd=t.planetData(pid);const chk=(n,x,y)=>{const c=t.cellOf(pd,t.basePlanDir(pd,x,y),pd.base);if(pd.cells[c].kind!=='base')off.push(pid+':'+n);};
        for(const id in B){chk(id,B[id].pos[0],B[id].pos[1]);}for(const b of t.BELTS)for(const q of b.path)chk(b.id,q[0],q[1]);}
      let nofail=0;for(const pid of t.PORDER){if(!t.S.planets[pid]){t.S.planets[pid]=t.freshPlanet(pid);t.initRuntime(pid);}const sp=t.S.planets[pid];sp.stage=5;t.S.maxStage=5;for(const id of t.BORDER)sp.bld[id]=1;
        const pts=t.STATIONS.map(s=>t.approachPos(s)).concat([[2.5,13],[13,-16.2]]);for(let i=0;i<12;i++)pts.push([19+(i%4)*1.8,13+Math.floor(i/4)*1.8]);
        for(let k=0;k<16;k++){const a=[Math.cos(k/16*6.283)*30,Math.sin(k/16*6.283)*30];for(const q of pts)if(!t.navPath(pid,a,q))nofail++;}}
      const gaps=[];for(const bl of t.BELTS){const A=t.BLD[bl.from],B=t.BLD[bl.to],s=bl.path[0],e=bl.path[bl.path.length-1];if(Math.hypot(s[0]-A.pos[0],s[1]-A.pos[1])>A.fp*.5+.05)gaps.push(bl.id+' start');if(Math.hypot(e[0]-B.pos[0],e[1]-B.pos[1])>B.fp*.5+.05)gaps.push(bl.id+' end');}
      return {issues:[...new Set(issues)],off:[...new Set(off)],unreachable:nofail,gaps}}""")
    show('layout', r)
    ok = not r['issues'] and not r['off'] and r['unreachable'] == 0 and not r['gaps'] and not errs
    for e in errs: print(e)
print('ALL OK' if ok else 'FAILED')
sys.exit(0 if ok else 1)
