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
    show('meteor coverage', r); check(r and len(r) >= 6 and min(r) > .4 and sum(r)/len(r) > .7, 'meteor showers too weak')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon'),w=t.W.moon;t.S.credits=1e8;t.buyStage();t.buyStage();
      for(const id of ['research','werkstatt','u2','u3','schmelze'])t.buildBuilding(id);t.S.research.done.drones=true;t.S.research.done.outpost=true;t.buyStage();t.buildBuilding('hangar');t.buyDrone();t.buyDrone();
      t.buyVehicle('truck');const site=t.outpostSites('moon')[0],far=[pd.cells[site],...pd.cells[site].nb.map(i=>pd.cells[i])];for(const c of far.slice(0,3)){w.clean[c.id]=1;w.paid[c.id]=1;w.nClean++;}t.calcFront('moon');
      const why=t.outpostWhy('moon',far[0].id);t.buildOutpost(far[0].id);const tr=sp.veh.find(v=>v.type==='truck');tr.d=far[1].c.slice();tr.tank={metall:300};tr.mode='seek';
      let lifts=0;for(let i=0;i<1200;i++){t.simStep('moon',.25,false);}return {why,out:sp.outposts.length,drones:sp.drones.length,stage:sp.stage,droneLoad:sp.drones.map(d=>d.mode)}}""")
    show('drones/outposts', r);
    r2 = E("""()=>{const t=__t,pd=t.planetData('moon');const a=t.outpostSites('moon').slice();for(const k in t.SITES)delete t.SITES[k];const b=t.outpostSites('moon');const sp=t.S.planets.moon,w=t.W.moon;const non=pd.cells.find(c=>c.kind!=='base'&&c.ring>=5&&!b.includes(c.id));w.clean[non.id]=1;
      return {a,b,n:new Set(b).size,ring:[...new Set(b.map(i=>pd.cells[i].ring))].length,non:t.outpostWhy('moon',non.id),rigOnSite:t.rigWhy('moon',b[1])}}""")
    show('outpost sites', r2); check(r2['a'] == r2['b'] and r2['n'] == 4 and r2['ring'] == 1 and 'markierten' in r2['non'] and r2['rigOnSite'] != '', 'fixed outpost sites')
    check(r['why'] == '' and r['out'] == 1 and r['drones'] == 2, 'drones/outposts')

    r = E("""()=>{const t=__t,pid='moon',sp=t.S.planets.moon,pd=t.planetData(pid),R=pd.R,bc=pd.cells[pd.base];const nv=t.navGrid(pid);let bad=0,n=0;
      for(let i=0;i<3000;i++){t.simStep(pid,.25,false);for(const v of sp.veh){if(!(v.mode==='drive'||v.mode==='toUnload'))continue;const l=t.toLocal(bc,v.d,R);if(Math.hypot(l[0],l[1])>30)continue;
        if(t.STATIONS.some(s=>Math.hypot(l[0]-t.BLD[s].pos[0],l[1]-t.BLD[s].pos[1])<5.2))continue;n++;if(nv.g[t.navIdx(l[0],l[1])])bad++;}}return {n,bad}}""")
    r2 = E("""()=>{const t=__t,pid='moon',sp=t.S.planets.moon,pd=t.planetData(pid),R=pd.R,bc=pd.cells[pd.base];const hits={};let n=0;
      const ids=t.BORDER.filter(k=>sp.bld[k]);for(let i=0;i<4000;i++){t.simStep(pid,.25,false);
        for(const v of sp.veh){if(v.mode==='queue'||v.mode==='unload'||v.mode==='lifted'||v.mode==='liftwait')continue;const l=t.toLocal(bc,v.d,R);if(Math.hypot(l[0],l[1])>40)continue;n++;
          for(const k of ids){const B=t.BLD[k];if(B.st&&l[0]>B.pos[0]+.8)continue;if(Math.hypot(l[0]-B.pos[0],l[1]-B.pos[1])<B.fp-.4){const key=v.mode+'@'+k;hits[key]=(hits[key]||0)+1;}}}}return {n,hits}}""")
    show('inside buildings', r2); check(sum(r2['hits'].values()) <= 2, 'vehicles inside building footprints')
    show('pathing', r); check(r['bad'] <= max(3, r['n'] * .01), 'vehicles drive through obstacles')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon;const c0=t.S.credits;sp.auto.staub=true;sp.roh.staub=t.rohCap()*.5;for(let i=0;i<600;i++)t.simStep('moon',.5,false);
      return {rockets:sp.rkN||0,flow:t.flowReport('moon').filter(n=>n.lvl>0).map(n=>n.name+': '+n.why)}}""")
    show('rocket + traffic light', r); check(r['rockets'] > 0, 'rocket never launched')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon;sp.sellBuf={staub:30,metall:20,staubBrick:3};sp.order={m:'metall'};const c=t.cargoValue('moon',sp);
      const base=30*t.matValue('staub')+20*t.matValue('metall')+3*t.matValue('staubBrick');const c0=t.S.credits;sp.rk=0;t.ACT.rkgo();return {c,base,gain:t.S.credits-c0,order:sp.order.m}}""")
    show('cargo value', r); check(abs(r['c']['base'] - r['base']) < 1e-6 and r['c']['total'] > r['base'] * 1.2 and abs(r['gain'] - r['c']['total']) < 1e-3, 'cargo value / freight order')
    r = E("""()=>{const t=__t;const t0=performance.now();const rep=t.runOffline(8*3600);const sp=t.S.planets.moon;return {ms:performance.now()-t0,cr:rep.credits,moon:rep.p.moon,dbg:{sb:sp.sellBuf,rk:sp.rk,roh:sp.roh,prod:sp.prod,cap:t.sellCap(),rc:t.rohCap(),ef:sp.ef,jam:sp.jam,ps:sp.mach.presse,bld:Object.keys(sp.bld).filter(k=>sp.bld[k])}}}""")
    show('offline 8h', r); check(r['cr'] > 0 and r['ms'] < 15000, 'offline simulation')

    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon');t.S.credits+=1000;t.buyCart();const c=sp.cart;
      const far=pd.cells.find(x=>x.kind!=='base'&&x.ring===4);window.__hp=t.toDir(far,0,0,pd.R);
      sp.hand={staub:t.handCap()};const roh0=t.rohSum(sp)+Object.values(sp.st).reduce((a,s)=>a+t.sumObj(s.buf),0);const modes=[];
      t.cam.gd=null;t.cam.d=window.__hp;for(let i=0;i<600&&c.mode!=='wait';i++){t.simStep('moon',.1,true);if(modes[modes.length-1]!==c.mode)modes.push(c.mode);}
      const dist=t.arcDist(c.d,t.cartGoal(),pd.R),gd=t.arcDist(t.cartPark('moon'),t.cartGoal(),pd.R);t.unloadHand();const hand=t.sumObj(sp.hand),load=t.sumObj(c.tank);
      for(let i=0;i<900&&c.mode!=='park';i++){t.simStep('moon',.1,true);if(modes[modes.length-1]!==c.mode)modes.push(c.mode);}
      const roh1=t.rohSum(sp)+Object.values(sp.st).reduce((a,s)=>a+t.sumObj(s.buf),0);return {modes,dist,gd,hand,load,left:t.sumObj(c.tank),cap:t.handCap()}}""")
    show('collector cart', r); check(r['dist'] < 3.5 and r['hand'] < .01 and abs(r['load'] - r['cap']) < .1 and r['left'] < .05 and r['modes'][-1] == 'park', 'collector cart')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon'),w=t.W.moon;const why0=t.rigWhy('moon',pd.cells.find(c=>c.kind!=='base'&&c.ring===3).id);
      for(const c of pd.cells)if(c.kind!=='base'){w.clean[c.id]=1;w.act.delete(c.id);}t.S.done.moon=1;t.S.credits+=1e7;const n0=(sp.rigs||[]).length;for(let i=0;i<20;i++)t.buildRig(-1);
      const n=sp.rigs.length,max=t.rigMax(sp);for(const k in sp.roh)sp.roh[k]=0;sp.energy=1e4;for(let i=0;i<60;i++)t.simStep('moon',1,false);
      const mined=sp.rigs.reduce((a,r)=>a+r.mined,0);const modes=new Set();let picked=0;for(let i=0;i<600;i++){t.simStep('moon',.5,false);for(const v of sp.veh){modes.add(v.mode);}}picked=sp.rigs.reduce((a,r)=>a+r.mined-t.sumObj(r.buf),0);return {veh:sp.veh.length,rigModes:[...modes].filter(m=>m.includes('ig')),picked,why0,n,max,mined,roh:t.rohSum(sp),status:sp.rigs.map(r=>r.status).slice(0,3),cells:new Set(sp.rigs.map(r=>r.cell)).size}}""")
    show('deep drilling', r); check(r['n'] == r['max'] and r['cells'] == r['n'] and r['mined'] > r['n'] * 20 and r['picked'] > 100 and len(r['rigModes']) == 2, 'deep drilling rigs')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;t.S.done.moon=1;sp.stage=5;t.S.credits+=1e9;sp.deep=0;const why=t.expWhy('moon');sp.deep=t.expNeed('moon');
      const m0=t.rigMax(sp),c0=t.rohCap(),u0=t.upMax('rohcap'),s0=t.vehSlots('moon');t.buyExp();return {why,exp:sp.exp,dm:t.rigMax(sp)-m0,cap:t.rohCap()/c0,du:t.upMax('rohcap')-u0,ds:t.vehSlots('moon')-s0,deep:sp.deep}}""")
    show('expansion level', r); check(r['why'] != '' and r['exp'] == 1 and r['dm'] == 2 and abs(r['cap'] - 1.3) < .01 and r['du'] == 2 and r['ds'] == 1 and r['deep'] == 0, 'expansion levels')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;const b=t.BELTS.find(x=>x.id==='b_rs');const g=t.beltGoods(sp,b),route=t.rawRoute(sp,'staub');
      sp.belts.b_rs=[[1,'kristall',5],[3,'kristall',4]];t.openSheet('belt','b_rs');const txt=document.querySelector('#sheet').textContent;t.closeSheet();
      const so=[];for(let i=0;i<30;i++){t.newOrder('moon',sp);so.push(sp.order.m);}return {g,route,shows:txt.includes('Gerade auf dem Band')&&txt.includes('Kristallsplitter'),orders:[...new Set(so)]}}""")
    show('value chain + belt sheet', r); check(r['route'] and 'staub' not in r['g'] and r['shows'] and 'staub' not in r['orders'], 'value chain / belt contents')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;const L=t.planetGoals('moon'),Lm=t.planetGoals('europa');const ready=L.filter(g=>t.pgReady('moon',g)).map(g=>g.id);
      const c0=t.S.credits,g0=L.find(g=>t.pgReady('moon',g));if(g0)t.claimPg(g0.id);const gain=t.S.credits-c0;
      t.openSheet('goals');const txt=document.querySelector('#sheet').textContent;t.closeSheet();
      return {n:L.length,ne:Lm.length,spec:Lm.some(g=>g.id==='sp_eisfraese'),ready:ready.length,claimed:g0?t.pgDone('moon',g0):null,gain,again:g0?t.pgReady('moon',g0):false,shows:txt.includes('Ziele auf dem Mond')}}""")
    show('planet goals', r); check(r['n'] >= 20 and r['spec'] and r['ready'] > 0 and r['claimed'] and r['gain'] > 0 and not r['again'] and r['shows'], 'planet goals')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon'),w=t.W.moon;const types={};for(let i=0;i<400;i++){const ty=t.meteorType('moon');types[ty]=(types[ty]||0)+1;}
      for(const c of pd.cells)if(c.kind!=='base'){w.clean[c.id]=1;w.act.delete(c.id);}t.S.done.moon=1;sp.stage=5;sp.rigs=[];t.S.credits+=1e9;for(let i=0;i<6;i++)t.buildRig(-1);
      const mats=[...new Set(sp.rigs.map(r=>t.rigMat('moon',r.cell)))];const r0=sp.rigs[0];r0.buf={};sp.energy=1e4;t.rigStep('moon',sp,r0,10,1);
      t.meteor('moon',false);const dts=[...w.act.values()].filter(a=>a.dust).map(a=>a.dustType);
      t.save();const saved=JSON.parse(localStorage.getItem('lunar-cleanup-tycoon-v2')).planets.moon.act;const st=Object.values(saved).filter(x=>x.t).length;
      return {types,mats,buf:Object.keys(r0.buf),dts:[...new Set(dts)],savedTyped:st,nonStart:dts.filter(x=>x!=='dust').length}}""")
    show('mixed supply', r); check(len(r['types']) >= 3 and len(r['mats']) >= 3 and len(r['buf']) >= 3 and (r['nonStart'] == 0 or r['savedTyped'] > 0), 'all production chains get material')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;t.S.research.done.raff=true;t.S.maxStage=5;for(const id of ['u1','u2','u3','presse','schmelze','raff','energy','prod'])t.buildBuilding(id);sp.energy=1e4;
      const used={};for(let i=0;i<1800;i++){sp.dayT=t.dayLen('moon')*.25;t.simStep('moon',1,false);for(const b of t.BELTS)if(t.beltLive(sp,b)&&sp.belts[b.id].length)used[b.id]=(used[b.id]||0)+1;}
      return {used,live:t.BELTS.filter(b=>t.beltLive(sp,b)).map(b=>b.id),veh:sp.veh.map(v=>v.type+':'+v.mode+':'+v.sid),u2:t.sumObj(sp.st.u2.buf),u2b:sp.belts.b_u2.length,sell:[t.sumObj(sp.sellBuf),t.sellCap(),sp.rk,sp.bld.sell],prod:[t.prodSum(sp),t.prodCap()],auto:sp.auto,mach:sp.mach.presse.status,ef:sp.ef,en:sp.energy,roh:t.rohSum(sp),rc:t.rohCap(),jam:sp.jam.b_u2}}""")
    idle = [b for b in r['live'] if r['used'].get(b, 0) < (30 if b in ('b_rp','b_ps','b_pp','b_sp','b_rr','b_rap','b_prs') else 0) and b != 'b_rs']
    show('belts busy on finished moon', {'idle': idle, 'used': r['used'],'veh':r['veh'],'u2':r['u2'],'u2b':r['u2b'],'roh':[r['roh'],r['rc']],'jam':r['jam'],'sell':r['sell'],'prod':r['prod'],'mach':r['mach'],'ef':r['ef']}); check(not idle, 'some belts stay idle on a finished planet')
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

    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon'),w=t.W.moon;t.S.research.done.airlift=true;sp.rigs=[];t.buildBuilding('lifter');t.buyLifter();
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

    r = E("""()=>{const t=__t,pid='moon',sp=t.S.planets.moon,pd=t.planetData(pid),R=pd.R,bc=pd.cells[pd.base];t.S.credits+=1e12;t.S.maxStage=5;sp.stage=5;
      for(const k of t.BORDER)if(!t.BLD[k].geo){t.S.research.done[t.BLD[k].research]=true;t.buildBuilding(k);}for(const ty of ['buggy','sauger','truck','staubi','sauger','truck'])t.buyVehicle(ty);
      for(const r of sp.rigs||[])r.buf={staub:200};const hits={},dr={};let n=0;const ids=t.BORDER.filter(k=>sp.bld[k]);
      for(let i=0;i<4000;i++){sp.dayT=t.dayLen(pid)*.25;t.simStep(pid,.25,false);for(const d of sp.drones)dr[d.mode]=(dr[d.mode]||0)+1;
        for(const v of sp.veh){if(v.mode==='queue'||v.mode==='unload'||v.mode==='lifted'||v.mode==='liftwait')continue;const l=t.toLocal(bc,v.d,R);if(Math.hypot(l[0],l[1])>40)continue;n++;
          for(const k of ids){const B=t.BLD[k];if(B.st&&l[0]>B.pos[0]+.8)continue;if(Math.hypot(l[0]-B.pos[0],l[1]-B.pos[1])<B.fp-.4){const key=v.mode+'@'+k;hits[key]=(hits[key]||0)+1;if(!window.__hl)window.__hl=[];if(window.__hl.length<6)window.__hl.push([v.type,v.sid,l.map(x=>+x.toFixed(1)),v.nav&&v.nav.wp.length,v.nav&&v.nav.i]);}}}}return {n,hits,veh:sp.veh.length,dr,hl:window.__hl}}""")
    show('full base: inside buildings + drones', r); check(sum(r['hits'].values()) <= 3 and r['dr'].get('stdrop', 0) > 0, 'full base: vehicles inside buildings or drones never deliver to stations')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon,pd=t.planetData('moon'),w=t.W.moon;t.S.credits+=1e13;
      const why0=t.blWhy('presse');for(const u of t.BLV_REQ.presse)sp.upg[u]=t.upMax(u);const f0=t.blF('presse'),c0=t.machInCap('presse');t.buyBl('presse');const f1=t.blF('presse'),c1=t.machInCap('presse');
      const ps=t.uv('pspeed');t.S.done.moon=1;for(const c of pd.cells)if(c.kind!=='base')w.clean[c.id]=1;if(!(sp.rigs||[]).length)t.buildRig(-1);const r=sp.rigs[0];for(let i=0;i<5;i++)t.rigUp(r.cell);const rate0=t.rigRate('moon',r);t.rigToBagger(r.cell);const rate1=t.rigRate('moon',r);
      const nb=pd.cells[r.cell].nb.find(id=>pd.cells[id].kind!=='base');w.act.delete(nb);const a=t.activate('moon',nb,null,true,'dust');t.dirtyAt(a,0,0,8,.95);const s0=a.sum;r.buf={};for(let i=0;i<60;i++)t.rigStep('moon',sp,r,1,1);
      const m1=t.matsLib('moon'),m2=t.matsLib('mars');
      return {why0,f0,f1,c0,c1,ps,lv:r.lv,bag:r.bag,rate:rate1/rate0,swept:s0-(w.act.get(nb)?w.act.get(nb).sum:0),s0,col:[m1.acc[0],m2.acc[0]]}}""")
    show('press/building level/bagger/colours', r); check(r['why0'] != '' and r['f1'] > r['f0'] and r['c1'] > r['c0'] and r['lv'] == 5 and r['bag'] == 1 and abs(r['rate'] - 2.5) < .01 and r['swept'] > r['s0'] * .5 and r['col'][0] != r['col'][1], 'press upgrade / building levels / Erzbagger / planet colours')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon,M=sp.mach.presse;const run=l=>{sp.upg.pvol=l;M.in={staub:1e3};M.out={staubBrick:0,metallBrick:0};M.cur=-1;M.prog=0;sp.energy=1e4;let made=0;
        for(let i=0;i<120;i++){const b0=M.out.staubBrick;t.machStep(sp,'presse',.25,1);made+=Math.max(0,M.out.staubBrick-b0);M.out.staubBrick=0;M.out.metallBrick=0;M.in.staub=1e3;}return made;};
      const a=run(0),b=run(4);sp.upg.pvol=0;return {a,b}}""")
    show('press volume', r); check(r['b'] >= r['a'] * 4, 'press volume upgrade')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;if(!sp.outposts.length)return {skip:1};const o=sp.outposts[0];const run=l=>{sp.upg.pipe=l;o.buf={staub:300};for(const k in sp.roh)sp.roh[k]=0;const b0=t.sumObj(o.buf);t.outpostStep('moon',sp,o,1,1);return b0-t.sumObj(o.buf);};
      const a=run(0),b=run(4);return {a,b}}""")
    show('pipeline pumps', r); check(r.get('skip') or r['b'] >= r['a'] * 2.9, 'pipeline upgrade')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;if(!sp.outposts.length)return {skip:1};const o=sp.outposts[0];sp.upg.pipe=0;let stuck=0,maxT=0;
      for(let i=0;i<1200;i++){o.buf={staub:t.stBuf(o.key)};sp.dayT=t.dayLen('moon')*.25;t.simStep('moon',.5,false);let n=0;for(const v of sp.veh)if(v.sid===o.key&&(v.mode==='unload'||v.mode==='queue')){n++;v._st=(v._st||0)+.5;maxT=Math.max(maxT,v._st);}else v._st=0;stuck=Math.max(stuck,n);}
      return {stuck,maxT}}""")
    show('vehicles leave a full outpost', r); check(r.get('skip') or r['maxT'] <= 15, 'vehicles camp at a full outpost')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;t.S.credits+=1e9;sp.spec=null;const p0=t.specF('price','moon');t.setSpec('trade');const p1=t.specF('price','moon');const c0=t.S.credits;t.setSpec('ind');const paid=c0-t.S.credits;
      const m1=t.specF('mach','moon');sp.spec='trade';sp.flowB=1;sp.sellBuf={staubBrick:10};sp.order={m:'barren'};const cv=t.cargoValue('moon',sp);
      t.newWorkOrder('moon',sp);const w=sp.wo,d0=sp.woDone||0;t.woCount('moon',sp,w.m,w.n,false);
      return {p0,p1,paid,m1,flow:cv.flow,spec:cv.spec,done:(sp.woDone||0)-d0,next:!!sp.wo&&sp.wo.got===0}}""")
    show('specialisation / flow bonus / work orders', r); check(r['p0'] == 1 and abs(r['p1'] - 1.3) < 1e-9 and r['paid'] > 0 and r['m1'] > 1.29 and abs(r['flow'] - 1.3) < 1e-9 and r['spec'] > 1.29 and r['done'] == 1 and r['next'], 'v6 long-term systems')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;if(!sp.outposts.length||!sp.bld.hangar)return {skip:1};const o=sp.outposts[0];for(const v of sp.veh)v.tank={};
      const kinds={};let moved=0;for(let i=0;i<1600;i++){o.buf={staub:t.stBuf(o.key)*.9};for(const r of sp.rigs||[])r.buf={metall:t.rigBuf(r)*.9};sp.dayT=t.dayLen('moon')*.25;t.simStep('moon',.25,false);for(const d of sp.drones){if(d.mode==='load'&&d.tk)kinds[d.tk]=(kinds[d.tk]||0)+1;}}
      return {kinds,drones:sp.drones.length}}""")
    show('drone pick-up points', r); check(r.get('skip') or r['kinds'].get('o', 0) > 0, 'drones collect from outposts')
    r = E("""()=>{const t=__t,sp=t.S.planets.moon;t.S.credits+=1e12;t.S.maxStage=5;sp.stage=5;if(!sp.bld.werkstatt)t.buildBuilding('werkstatt');sp.autoMaint=false;const v=sp.veh[0];
      v.wear=1.01;v.mode='seek';t.vehStep('moon',sp,v,.1,1,false);const broke=v.mode;const reps=[];
      for(let k=0;k<3;k++){v.wear=1.01;v.mode='broken';const ok=t.sendToShop('moon',sp,v,true);for(let i=0;i<4000&&(v.mode==='toShop'||v.mode==='repair');i++){sp.dayT=t.dayLen('moon')*.25;t.vehStep('moon',sp,v,.25,1,false);}reps.push([ok,v.rep,+(v.wear||0).toFixed(2),v.mode]);}
      const n0=sp.veh.length;v.wear=1.2;v.mode='broken';for(let i=0;i<200;i++)t.vehWear('moon',sp,v,1,false);const offlineKept=sp.veh.length===n0;
      t.replaceVeh('moon',sp,v,true);const fresh=[v.rep,v.wear];sp.autoMaint=true;
      // rig fault
      for(const c of t.planetData('moon').cells)if(c.kind!=='base')t.W.moon.clean[c.id]=1;t.S.done.moon=1;if(!(sp.rigs||[]).length)t.buildRig(-1);const rg=sp.rigs[0];rg.buf={};rg.fault={k:'jam',t:5};const b0=rg.mined||0;sp.energy=1e4;
      for(let i=0;i<4;i++)t.rigStep('moon',sp,rg,1,1);const stopped=(rg.mined||0)===b0&&!!rg.fault;t.rigStep('moon',sp,rg,2,1);const auto=!rg.fault;rg.fault={k:'heat',t:99};const c0=t.S.credits;t.fixFault(rg.cell);
      const manual=!rg.fault&&t.S.credits<c0;sp.hzOn=0;
      return {broke,reps,offlineKept,fresh,stopped,auto,manual}}""")
    show('maintenance / repairs / faults', r); check(r['broke'] == 'broken' and r['reps'][0][1] == 1 and r['reps'][1][1] == 2 and r['reps'][2][0] is False and r['reps'][0][2] == 0 and r['offlineKept'] and r['fresh'] == [0, 0] and r['stopped'] and r['auto'] and r['manual'], 'vehicle maintenance / rig faults')
    E("()=>{__t.openSheet('settings')}"); pg.wait_for_timeout(300)
    r = E("()=>{const h=document.querySelector('#sheet').textContent;__t.closeSheet();return {idle:h.includes('Idle-Test'),off:!!document.querySelector('[data-act=off]')}}")
    show('no idle test button', r); check(not r['idle'] and not r['off'], 'idle test removed from settings')
    E("()=>{window.__c0=__t.S.credits;window.__ok=__t.startOffline(2*3600,rep=>{window.__rep=rep;});}")
    pg.wait_for_timeout(500)
    r0 = E("()=>!!__t.offRun")
    for _ in range(60):
        if not E("()=>!!__t.offRun"): break
        pg.wait_for_timeout(250)
    r = E("()=>({ok:window.__ok,busy0:false,done:!__t.offRun,secs:window.__rep&&window.__rep.secs,gain:__t.S.credits-window.__c0,track:__t.TRACK===null})")
    show('async offline', r); check(r['ok'] and r['done'] and r['secs'] == 7200 and r['track'], 'async offline simulation')
    s1 = E("()=>{const t=__t,w=t.W.moon,pd=t.planetData('moon');let id=-1;for(const c of pd.cells)if(c.kind!=='base'&&w.clean[c.id]){id=c.id;break;}w.act.delete(id);const a=t.activate('moon',id,null,true,'crystal');a.saveDirty=true;t.dirtyAt(a,0,0,6,.9);t.S.planets.moon.upg.pspeed=3;__t.save();return {ps:3,cr:Math.round(t.S.credits),dt:id+':'+a.dustType,n:__t.W.moon.clean.reduce((a,b)=>a+b,0),d:__t.S.planets.moon.drones.length,o:__t.S.planets.moon.outposts.length}}")
    E("""()=>{const t=__t;if(!t.S.planets.mars){t.S.planets.mars=t.freshPlanet('mars');t.initRuntime('mars');}t.travelTo('mars');}""")
    for _ in range(40):
        pg.wait_for_timeout(200)
        if not E("()=>!!__t.travelFx"): break
    r = E("()=>({p:__t.S.planet,fx:!!__t.travelFx})")
    show('seamless travel', r); check(r['p'] == 'mars' and not r['fx'], 'seamless travel')
    E("()=>{__t.travelTo('moon')}")
    for _ in range(40):
        pg.wait_for_timeout(200)
        if not E("()=>!!__t.travelFx"): break
    pg.reload(); pg.wait_for_timeout(1200)
    s2 = E("()=>({ps:__t.S.planets.moon.upg.pspeed||0,cr:Math.round(__t.S.credits),dt:(()=>{for(const[id,a]of __t.W.moon.act)if(a.dustType==='crystal')return id+':crystal';return 'none';})(),n:__t.W.moon.clean.reduce((a,b)=>a+b,0),d:__t.S.planets.moon.drones.length,o:__t.S.planets.moon.outposts.length})")
    show('save/load', [s1, s2]); check(s1['d'] == s2['d'] and s1['o'] == s2['o'] and s2['n'] == s1['n'] and s1['dt'] == s2['dt'] and s2['ps'] == 0 and s2['cr'] > s1['cr'], 'save/load roundtrip')

    for e in errs[:20]:
        print(e)
    if errs:
        fails.append('js errors')

print('ALL OK' if not fails else 'FAILED: ' + ', '.join(fails))
sys.exit(1 if fails else 0)
