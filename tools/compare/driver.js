// Plays a level's bot run frame by frame at exactly 60 fps in the ORIGINAL game (harness.html), like test/BotRun.cs does in Godot.
// node driver.js <harness.html> <levelId> <n|e> <outDir> [every=60] [from=0] [to=all]
// RPV=1|2: play the bot's run as an old replay of that version (old level shapes; the bot may get hit)
// MODE=play: real play through step() with a fixed key script (like --play-run in Godot)
// The original runs in Electron 44, the same as the players' PC app (its V8 math differs from plain Chromium's).
// ELECTRON=<path to electron binary> (tools/setup.sh downloads it to ~/electron44); without it, Chromium is used.
const { chromium, _electron } = require('/opt/node-tools/node_modules/playwright');
const fs = require('fs'), path = require('path');
const [, , html, id, m, outDir, every0, from0, to0] = process.argv;
const PLAY = process.env.MODE === 'play';
const every = +(every0 || 60), from = +(from0 || 0), to = to0 ? +to0 : 1e9;
(async () => {
  fs.mkdirSync(outDir, { recursive: true });
  const EL = process.env.ELECTRON;
  let b, p;
  if (EL) {
    b = await _electron.launch({ executablePath: EL, args: ['--no-sandbox', path.join(__dirname, 'electron-main.js')], env: { ...process.env, MG_HARNESS: path.resolve(html) } });
    p = await b.firstWindow();
    p.on('pageerror', e => console.log('ERR', e.message));
  } else {
    b = await chromium.launch();
    p = await (await b.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 })).newPage();
    p.on('pageerror', e => console.log('ERR', e.message));
    await p.goto('file://' + path.resolve(html));
  }
  await p.waitForFunction(() => window.__mg, null, { timeout: 300000, polling: 500 });   // slow when several runs share the machine
  await p.evaluate(() => document.fonts.ready);
  await p.evaluate(async () => { for (const f of ['800 40px Tajawal', '500 40px Tajawal', '40px Audiowide']) await document.fonts.load(f, 'ابc'); });
  await p.evaluate(({ id, m, play, rpv }) => {
    window.__drv = __mg(`({
      setup(id,m,rpv){
        const mb=a=>()=>{a=(a+0x6D2B79F5)>>>0;let t=a;t=Math.imul(t^t>>>15,1|t);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296;};
        noMus=true;window.__NOW=100000;window.__T=0;
        drawRpOverlay=()=>{};  // the replay bar is the UI thread's to compare
        const L=ALL_LV().find(x=>x.id===id);
        Math.random=mb(12345);
        rp.doc=botDoc(L,m);rp.bot=true;rp.back='levels';rp.L=L;rp.m=m;rp.hide=true;
        if(rpv){rp.bot=false;rp.doc=Object.assign({},rp.doc,{v:rpv});}
        rp.saveEasy=easy;easy=m==='e';applyTheme(easy);cur=L;levelTiming(L);hitsTotal=0;deaths=0;
        rp.paused=false;rp.rate=1;rp.barT=4;rpTimeline();rpGo(0);
        this.T0=songT;this.i=0;this.log=[];this.rec();
      },
      setupPlay(id,m){
        const mb=a=>()=>{a=(a+0x6D2B79F5)>>>0;let t=a;t=Math.imul(t^t>>>15,1|t);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296;};
        window.__NOW=100000;window.__T=0;const L=ALL_LV().find(x=>x.id===id);
        Math.random=mb(12345);
        easy=m==='e';applyTheme(easy);cur=L;fromList='levels';levelTiming(L);L.buf={};noMus=false;
        requestStart(0);beginRun(0);last=performance.now();
        this.total=Math.ceil((B(L.end)+2)*60);this.i=0;this.log=[];this.rec();
      },
      playFrame(to){if(this.i+1>this.total||this.i+1>to)return false;this.i++;const i=this.i;
        const D=[[1,0],[1,1],[0,1],[-1,1],[-1,0],[-1,-1],[0,-1],[1,-1]],d=D[(Math.floor(i/40)*5)%8];
        keys.ArrowRight=d[0]>0;keys.ArrowLeft=d[0]<0;keys.ArrowDown=d[1]>0;keys.ArrowUp=d[1]<0;keys.KeyJ=i%23===0;keys.Enter=i%50===0;
        window.__T+=1/60;window.__NOW+=1000/60;step(window.__NOW);this.rec();return true;},
      runPlay(every,from,to){while(true){if(!this.playFrame(to))return -1;if(this.i>=from&&this.i%every===0)return this.i;}},
      rec(){const p=player||{x:0,y:0,hp:0};let sx=0,sy=0;for(const h of hazards){sx+=(h.x||0);sy+=(h.y||0);}
        this.log.push([this.i,songT,p.x,p.y,p.hp,hazards.length,particles.length,rngS,checkpoint,sx,sy,hitsTotal,deaths,state].join(' '));},
      sim(to){const dt=1/60,s=rp.doc.segs[rp.seg],t=this.T0+(this.i+1)*dt;
        if((t-s.t0)*rp.doc.hz>=s.n-1||this.i+1>to)return false;
        this.i++;rpApply(t,dt,true);
        for(const q of particles){q.x+=q.vx*dt;q.y+=q.vy*dt;const f=Math.pow(0.04,dt);q.vx*=f;q.vy*=f;q.life-=dt;}
        particles=particles.filter(q=>q.life>0);
        gloom+=(gloomT-gloom)*Math.min(1,dt*3);shake=Math.max(0,shake-dt*50);flash=Math.max(0,flash-dt*2.2);cpMsg-=dt;
        window.__NOW+=1000/60;this.rec();return true;},
      // runs frames until the next capture (every frame drawn); returns the captured frame number or -1 at the end
      run(every,from,to){while(true){if(!this.sim(to))return -1;draw(window.__NOW);if(this.i>=from&&this.i%every===0)return this.i;}},
    })`);
    if (play) __drv.setupPlay(id, m); else __drv.setup(id, m, rpv);
  }, { id, m, play: PLAY, rpv: +(process.env.RPV || 0) });
  while (true) {
    const f = await p.evaluate(({ every, from, to, PLAY }) => PLAY ? __drv.runPlay(every, from, to) : __drv.run(every, from, to), { every, from, to, PLAY });
    if (f < 0) break;
    const d = await p.evaluate(() => document.getElementById('c').toDataURL('image/png'));
    fs.writeFileSync(path.join(outDir, 'f' + String(f).padStart(6, '0') + '.png'), Buffer.from(d.split(',')[1], 'base64'));
  }
  const log = await p.evaluate(() => __drv.log.join('\n') + '\n');
  fs.writeFileSync(path.join(outDir, 'state.log'), log);
  console.log('done frames', log.split('\n').length - 2);
  await b.close();
})();
