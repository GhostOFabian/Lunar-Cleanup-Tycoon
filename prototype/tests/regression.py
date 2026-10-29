"""Headless regression for the core systems. Run build_test_page.py first.
Exits non-zero on failed checks or JS errors."""
import sys
from harness import game, show

fails = []
def check(cond, msg):
    if not cond:
        fails.append(msg)
        print('FAIL', msg)

with game() as (pg, errs):
    E = pg.evaluate

    r = E("""()=>{const t=__t,pd=t.planetData('moon'),w=t.W.moon,far=pd.cells.find(c=>c.ring===6);
      return {front:w.front.size,start:w.front.has(pd.start),far:t.workable('moon',far.id)}}""")
    show('frontier', r); check(r['front'] == 17 and r['start'] and not r['far'], 'frontier')

    r = E("""()=>{const t=__t,pd=t.planetData('moon'),w=t.W.moon,st=pd.cells[pd.start],nb=st.nb.find(b=>w.front.has(b));t.ensureAct('moon',nb);
      const a1=w.act.get(pd.start),a2=w.act.get(nb),s1=a1.sum,s2=a2.sum;__ss({last:null,acc:0,ft:0,ld:null});
      for(let i=0;i<=40;i++){const q=i/40;t.stampManual(t.vnorm(t.vadd(t.vsc(st.c,1-q),t.vsc(pd.cells[nb].c,q))));}__ss(null);
      return {a:s1-a1.sum,b:s2-a2.sum}}""")
    show('one parcel per stroke', r); check(r['a'] > 1 and r['b'] == 0, 'stroke cleaned two parcels')

    r = E("""()=>{const t=__t,pd=t.planetData('moon'),w=t.W.moon,sp=t.S.planets.moon,a=w.act.get(pd.start);const H=a.cl.H;__ss({last:null,acc:0,ft:0,ld:null});t.stampManual(a.cl.c);
      for(let p=0;p<3;p++)for(let y=-H;y<=H;y+=.5)for(let x=-H;x<=H;x+=.3)t.stampManual(t.toDir(a.cl,x,y,pd.R));__ss(null);
      const full=t.sumObj(sp.hand),cap=t.handCap(),roh0=t.rohSum(sp);t.unloadHand();return {full,cap,roh0,roh1:t.rohSum(sp),left:t.sumObj(sp.hand)}}""")
    show('hand collector', r); check(abs(r['full'] - r['cap']) < 1 and r['roh1'] > r['roh0'] + r['cap'] * .9 and r['left'] < .1, 'hand collector')
    r = E("""()=>{const t=__t,w=t.W.moon,sp=t.S.planets.moon;t.S.credits=30000;t.buyStage();t.buildBuilding('u1');t.buildBuilding('presse');t.buildBuilding('energy');
      t.buyVehicle('buggy');t.buyVehicle('buggy');t.buyVehicle('staubi');const n0=w.nClean;for(let i=0;i<2400;i++)t.simStep('moon',.5,false);
      return {n0,n:w.nClean,rk:sp.rkN||0,veh:sp.veh.map(v=>v.status)}}""")
    show('vehicles 20 min', r); check(r['n'] > r['n0'] + 2, 'vehicles do not clean the frontier')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon;let charged=0,team=0,low=0;for(let i=0;i<4000;i++){t.simStep('moon',.5,false);for(const v of sp.veh){if(v.charging)charged++;if((v.status||'').includes('Team'))team++;if((v.status||'').includes('Akku fast leer'))low++;}}
      return {charged,team,low,bat:sp.veh.filter(v=>t.VT[v.type].batt).map(v=>Math.round(v.bat))}}""")
    show('battery + teamwork', r); check(r['charged'] > 0 and r['low'] > 0, 'battery charging cycle')
    r = E("""()=>{const t=__t,w=t.W.moon,pd=t.planetData('moon');for(const c of pd.cells)if(c.ring>=3&&c.ring<=4){w.clean[c.id]=1;w.nClean++;}t.calcFront('moon');const out=[];
      for(let i=0;i<5;i++){const b=new Set(w.act.keys());t.meteor('moon',false);for(const[id,a]of w.act)if(a.dust&&!b.has(id))out.push(a.sum/a.cl.nIn);}return out}""")
    show('meteor coverage', r); check(r and min(r) > .35, 'meteor showers too weak')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon'),w=t.W.moon;t.S.credits=1e8;t.buyStage();t.buyStage();
      for(const id of ['research','werkstatt','u2','u3','schmelze'])t.buildBuilding(id);t.S.research.done.drones=true;t.S.research.done.outpost=true;t.buyStage();t.buildBuilding('hangar');t.buyDrone();t.buyDrone();
      t.buyVehicle('truck');const far=pd.cells.filter(c=>c.ring===6&&c.kind==='normal');for(const c of far.slice(0,3)){w.clean[c.id]=1;w.paid[c.id]=1;w.nClean++;}t.calcFront('moon');
      const why=t.outpostWhy('moon',far[0].id);t.buildOutpost(far[0].id);const tr=sp.veh.find(v=>v.type==='truck');tr.d=far[1].c.slice();tr.tank={metall:300};tr.mode='seek';
      let lifts=0;for(let i=0;i<1200;i++){t.simStep('moon',.25,false);}return {why,out:sp.outposts.length,drones:sp.drones.length,stage:sp.stage,droneLoad:sp.drones.map(d=>d.mode)}}""")
    show('drones/outposts', r); check(r['why'] == '' and r['out'] == 1 and r['drones'] == 2, 'drones/outposts')

    r = E("""()=>{const t=__t,pid='moon',sp=t.S.planets.moon,pd=t.planetData(pid),R=pd.R,bc=pd.cells[pd.base];const nv=t.navGrid(pid);let bad=0,n=0;
      for(let i=0;i<3000;i++){t.simStep(pid,.25,false);for(const v of sp.veh){if(!(v.mode==='drive'||v.mode==='toUnload'))continue;const l=t.toLocal(bc,v.d,R);if(Math.hypot(l[0],l[1])>30)continue;
        if(t.STATIONS.some(s=>Math.hypot(l[0]-t.BLD[s].pos[0],l[1]-t.BLD[s].pos[1])<5.2))continue;n++;if(nv.g[t.navIdx(l[0],l[1])])bad++;}}return {n,bad}}""")
    show('pathing', r); check(r['bad'] <= max(3, r['n'] * .01), 'vehicles drive through obstacles')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon;const c0=t.S.credits;sp.auto.staub=true;sp.roh.staub=t.rohCap()*.5;for(let i=0;i<600;i++)t.simStep('moon',.5,false);
      return {rockets:sp.rkN||0,flow:t.flowReport('moon').filter(n=>n.lvl>0).map(n=>n.name+': '+n.why)}}""")
    show('rocket + traffic light', r); check(r['rockets'] > 0, 'rocket never launched')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon;sp.sellBuf={staub:30,metall:20,staubBrick:3};sp.order={m:'metall'};const c=t.cargoValue('moon',sp);
      const base=30*t.matValue('staub')+20*t.matValue('metall')+3*t.matValue('staubBrick');const c0=t.S.credits;sp.rk=0;t.ACT.rkgo();return {c,base,gain:t.S.credits-c0,order:sp.order.m}}""")
    show('cargo value', r); check(abs(r['c']['base'] - r['base']) < 1e-6 and r['c']['total'] > r['base'] * 1.2 and abs(r['gain'] - r['c']['total']) < 1e-3, 'cargo value / freight order')
    r = E("""()=>{const t=__t;const t0=performance.now();const rep=t.runOffline(8*3600);return {ms:performance.now()-t0,cr:rep.credits,moon:rep.p.moon}}""")
    show('offline 8h', r); check(r['cr'] > 0 and r['ms'] < 15000, 'offline simulation')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon,L=t.dayLen('moon');sp.dayT=L*.25;const noon=t.dayF('moon',sp),pn=t.solarProd(Object.assign({},sp,{solar:noon}));sp.dayT=L*.75;const night=t.dayF('moon',sp);
      let avg=0;for(let i=0;i<100;i++){sp.dayT=L*i/100;avg+=t.dayF('moon',sp)/100;}sp.dayT=L*.75;t.simStep('moon',1,false);return {noon,night,avg,solarNight:sp.solar,di:t.dayInfo('moon',sp)}}""")
    show('day/night', r); check(r['noon'] > .99 and r['night'] == 0 and r['solarNight'] == 0 and .3 < r['avg'] * 2.6 / 2.6 < .5 and not r['di']['day'], 'day/night solar')
    r = E("""()=>{const t=__t;t.S.ctr.cells=30;t.S.ach={};const c0=t.S.credits;const n0=t.achClaimable();t.claimAch('cells');t.claimAch('cells');t.claimAch('cells');
      t.openSheet('goals');const rows=document.querySelectorAll('#sheet .qline').length;t.closeSheet();return {n0,lvl:t.S.ach.cells,gain:t.S.credits-c0,rows,q:t.QUESTS.length}}""")
    show('achievements', r); check(r['lvl'] == 2 and abs(r['gain'] - 1500) < 1 and r['rows'] == r['q'] and r['q'] >= 40, 'achievements / goals sheet')
    r = E("""()=>{const t=__t;t.S.stats.earned=5e7;t.S.planets.mars=t.freshPlanet('mars');t.grantAuto(t.S.planets.mars);t.initRuntime('mars');return {gain:t.prestigeGain(),mars:t.powReq('mars')}}""")
    show('prestige gain', r); check(r['gain'] > 0, 'prestige')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon;const e0=t.energyProd(sp);t.S.research.done.rtg=true;t.S.research.done.nuclear=true;t.S.maxStage=5;t.buildBuilding('rtg');t.buildBuilding('reactor');t.buildBuilding('geo');
      return {e0,e1:t.energyProd(sp),geo:!!sp.bld.geo,cap:t.energyCap(sp),locked:t.vehLocked('eisfraese')}}""")
    show('energy + vehicle lock', r); check(r['e1'] >= r['e0'] + 159 and not r['geo'] and r['locked'], 'energy buildings / vehicle lock')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon'),w=t.W.moon;t.S.research.done.airlift=true;t.buildBuilding('lifter');t.buyLifter();
      for(const c of pd.cells)if(c.kind!=='base'&&c.ring<=6){w.clean[c.id]=1;}w.nClean=pd.cells.filter(c=>c.kind!=='base'&&w.clean[c.id]).length;for(const c of pd.cells)if(c.ring>6)w.clean[c.id]=0;for(const[id,a]of[...w.act])w.act.delete(id);t.calcFront('moon');
      sp.veh=[];t.buyVehicle('truck');const v=sp.veh[0];v.d=t.basePlanDir(pd,2.5,13);v.mode='seek';const seen=new Set();for(let i=0;i<800;i++){t.simStep('moon',.25,false);seen.add(v.mode);}
      return {modes:[...seen],lifter:sp.lifters[0].mode}}""")
    show('airlift', r); check('lifted' in r['modes'], 'heavy-lift drone never carried a vehicle')
    for k in ['vehicles', 'production', 'base', 'upgrades', 'planets', 'settings', 'daily', 'stats', 'hazard']:
        E(f"()=>__t.openSheet('{k}')"); pg.wait_for_timeout(40)
    for bid in ['hq', 'roh', 'prod', 'sell', 'u1', 'u2', 'u3', 'presse', 'schmelze', 'energy', 'research', 'werkstatt', 'hangar', 'rtg', 'geo', 'reactor', 'lifter']:
        E(f"()=>__t.openSheet('building','{bid}')"); pg.wait_for_timeout(30)
    E("()=>__t.closeSheet()")
    r = E("""()=>{const t=__t;t.closeSheet();t.updLabels();const vis=()=>{const o=[];for(const id in t.buildViews)t.buildViews[id].g.traverse(n=>{if(n.userData.label&&n.visible)o.push(id);});return o;};
      const a=vis();t.openSheet('building','roh');t.updLabels();const b=vis();t.closeSheet();t.updLabels();return {closed:a,open:b,after:vis()}}""")
    show('labels', r); check(r['closed'] == [] and r['open'] in ([], ['roh']) and r['after'] == [], 'building labels')  # with the three.js mock traverse() is a no-op, so 'open' stays empty; verified with real three.js

    s1 = E("()=>{__t.save();return {n:__t.W.moon.clean.reduce((a,b)=>a+b,0),d:__t.S.planets.moon.drones.length,o:__t.S.planets.moon.outposts.length}}")
    pg.reload(); pg.wait_for_timeout(1200)
    s2 = E("()=>({n:__t.W.moon.clean.reduce((a,b)=>a+b,0),d:__t.S.planets.moon.drones.length,o:__t.S.planets.moon.outposts.length})")
    show('save/load', [s1, s2]); check(s1['d'] == s2['d'] and s1['o'] == s2['o'] and s2['n'] == s1['n'], 'save/load roundtrip')

    for e in errs[:20]:
        print(e)
    if errs:
        fails.append('js errors')

print('ALL OK' if not fails else 'FAILED: ' + ', '.join(fails))
sys.exit(1 if fails else 0)
