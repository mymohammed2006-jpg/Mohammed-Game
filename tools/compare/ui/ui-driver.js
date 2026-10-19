// The UI check, browser side: runs the ORIGINAL game from the gate with a fixed key script at exactly 60 fps,
// fake Firebase answers (fakenet.json) and fixed song lengths, and writes a state line per frame, the requests sent, and PNGs.
// node ui-driver.js <harness.html> <outDir> <durations.json>
const { chromium } = require('/opt/node-tools/node_modules/playwright');
const fs = require('fs'), path = require('path');
const [, , html, outDir, durF] = process.argv;
const HERE = __dirname;
const script = JSON.parse(fs.readFileSync(path.join(HERE, 'script.json')));
const net = fs.readFileSync(path.join(HERE, 'fakenet.json'), 'utf8');
const dur = fs.readFileSync(durF, 'utf8');
(async () => {
  fs.mkdirSync(outDir, { recursive: true });
  // the PC program: window.mgApp is set, and a few best letters are already on the device
  const pre = `<script>window.mgApp={};localStorage.clear();localStorage.setItem('mg_best_المرحلة الأولى','A');localStorage.setItem('mg_beste_المرحلة الأولى','S');localStorage.setItem('mg_best_ليش ضيعت الوفا','S');</script>`;
  const src = fs.readFileSync(html, 'utf8'); const i = src.indexOf('<script');
  const uiHtml = path.join(path.dirname(html), 'ui-harness.html'); fs.writeFileSync(uiHtml, src.slice(0, i) + pre + src.slice(i));
  const b = await chromium.launch();
  const p = await (await b.newContext({ viewport: { width: 1280, height: 720 }, deviceScaleFactor: 1 })).newPage();
  p.on('pageerror', e => console.log('ERR', e.message));
  await p.goto('file://' + uiHtml);
  await p.waitForFunction(() => window.__mg);
  await p.evaluate(() => document.fonts.ready);
  await p.evaluate(async () => { for (const f of ['800 40px Tajawal', '500 40px Tajawal', '40px Audiowide']) await document.fonts.load(f, 'ابc'); });
  await p.evaluate(({ net, dur, script }) => {
    window.__NET = JSON.parse(net); window.__DUR = JSON.parse(dur); window.__SCRIPT = script; window.__REQ = [];
    window.__drv = __mg(`({
      setup(){
        const mb=a=>()=>{a=(a+0x6D2B79F5)>>>0;let t=a;t=Math.imul(t^t>>>15,1|t);t=t+Math.imul(t^t>>>7,61|t)^t;return((t^t>>>14)>>>0)/4294967296;};
        Math.random=mb(12345);
        MENUS.normal.buf={duration:__DUR.menu};MENUS.easy.buf={duration:__DUR.ez};applyMenu();
        for(const L of ALL_LV()){L.buf={duration:__DUR[L.id]};L.err=false;}
        window.fetch=async(url,opt)=>{opt=opt||{};const m=opt.method||'GET';window.__REQ.push(m+' '+url+(opt.headers&&opt.headers.Authorization?' auth='+opt.headers.Authorization:'')+(opt.body!==undefined?' body='+opt.body:''));
          let st=404,body={error:{code:404,message:'NOT_FOUND',status:'NOT_FOUND'}};
          for(const[rm,ru,rs,rb]of __NET.rules)if(rm===m&&url.includes(ru)){st=rs;body=rb;break;}
          if(url.includes('signInWithPassword')){const bad=JSON.parse(opt.body).password==='wrongpw1';st=bad?400:200;body=bad?__NET.signin_bad:__NET.signin_ok;}
          return{ok:st>=200&&st<300,status:st,json:async()=>JSON.parse(JSON.stringify(body))};};
        last=performance.now();this.i=0;this.log=[];this.ups=[];
      },
      frame(){const i=++this.i;
        for(const[k,at]of this.ups)if(at===i)dispatchEvent(new KeyboardEvent('keyup',{code:k}));
        for(const[f,a,arg]of __SCRIPT.steps)if(f===i){
          if(a==='key'){dispatchEvent(new KeyboardEvent('keydown',{code:arg}));this.ups.push([arg,i+3]);}
          else if(a==='type')inEl.value=(inEl.value+arg).slice(0,inEl.maxLength);
          else if(a==='bs')inEl.value=inEl.value.slice(0,-1);
          else if(a==='rpwin'){const b=[];for(let j=0;j<100;j++)b.push(10000+j*37,18000+(j*53)%9000,(j%7===0?1:0)|(5<<1));
            rec={segs:[{cp:0,seed:12345,t0:-0.1,b,n:100},{cp:100,seed:-5,t0:43.2,b:b.slice(0,3),n:1}],cur:null};cur=LEVELS[1];easy=false;
            cur.best='S';try{localStorage.setItem('mg_best_'+cur.name,'S');}catch(e){}rpWin('S');markDirty();}
          else if(a==='end')return false;}
        window.__T+=1/60;window.__NOW+=1000/60;step(window.__NOW);this.rec();return true;},
      rec(){const P=prof.p;this.log.push([this.i,state,menuSel,lvlSel,friSel,setSel,accSel,nameVal,passVal.length,authMsg,board.idx,board.mode,board.sel,board.list.length,
        prof.row,prof.col,P?P.name:'-',pr.sel,pr.t,pr.spd,pr.let,pr.inf,pr.lv,pr.ghost,newsPg,easy,acc?acc.name:'-',syncing,netState,rp.seg,rp.paused,rp.rate,rp.hide,
        Math.round(songT*1000),toast,toastT>0,Math.round(pulse*1e6),vol,Object.keys(rpq).length].join('|'));},
    })`);
    __drv.setup();
  }, { net, dur, script });
  const caps = new Set(script.caps);
  while (true) {
    const ok = await p.evaluate(() => __drv.frame());
    if (!ok) break;
    const i = await p.evaluate(() => __drv.i);
    if (caps.has(i)) { const d = await p.evaluate(() => document.getElementById('c').toDataURL('image/png')); fs.writeFileSync(path.join(outDir, 'f' + String(i).padStart(6, '0') + '.png'), Buffer.from(d.split(',')[1], 'base64')); }
  }
  fs.writeFileSync(path.join(outDir, 'state.log'), await p.evaluate(() => __drv.log.join('\n') + '\n'));
  fs.writeFileSync(path.join(outDir, 'net.log'), await p.evaluate(() => window.__REQ.join('\n') + '\n'));
  console.log('js done');
  await b.close();
})();
