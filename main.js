// Mohammed Game: يفتح اللعبة (game/index.html) في نافذة خاصة بدون متصفح.
// من نسخة 8: أول ما يفتح يدور على تحديث في GitHub، وإذا فيه يحمله ويعيد التشغيل لحاله.
const { app, BrowserWindow, ipcMain, protocol, net, Menu, screen } = require('electron');
const path = require('path');
const fs = require('fs');

app.commandLine.appendSwitch('autoplay-policy', 'no-user-gesture-required');

// اختيار ملء الشاشة محفوظ في ملف، عشان النافذة تنفتح على طول بنفس آخر اختيار
// (V-Sync و Framecap صارت داخل اللعبة نفسها من نسخة 11، وتتطبق بدون إعادة تشغيل)
const SETTINGS_FILE = path.join(app.getPath('userData'), 'settings.json');
let settings = { fullscreen: true };
try { settings = Object.assign(settings, JSON.parse(fs.readFileSync(SETTINGS_FILE, 'utf8'))); } catch (e) {}
function saveSettings() { try { fs.writeFileSync(SETTINGS_FILE, JSON.stringify(settings)); } catch (e) {} }

// الخطوط محفوظة داخل البرنامج عشان تشتغل بدون نت وبنفس الشكل
const FONTS = path.join(__dirname, 'fonts');
function fontResponse(file, type) {
  if (!file.startsWith(FONTS) || !fs.existsSync(file)) return new Response('', { status: 404 });
  return new Response(fs.readFileSync(file), { headers: { 'content-type': type, 'access-control-allow-origin': '*' } });
}

if (!app.requestSingleInstanceLock()) app.quit();

const CHECK_WAIT_MS = 7000; // إذا ما لقى تحديث خلال هالوقت (نت بطيء أو مافي نت) تفتح اللعبة
let win = null;
let gameLoaded = false;

function loadGame() {
  if (gameLoaded || !win || win.isDestroyed()) return;
  gameLoaded = true;
  win.loadFile(path.join(__dirname, 'game', 'index.html'));
}

function showUpdate(state, percent) {
  if (gameLoaded || !win || win.isDestroyed()) return;
  win.webContents.executeJavaScript(`setState(${JSON.stringify(state)}, ${Number(percent) || 0})`).catch(() => {});
}

function makeUpdater() {
  if (process.env.MG_UPDATE_TEST_URL) {
    // للتجربة فقط: يدور على التحديث في رابط محلي بدل GitHub
    const { NsisUpdater } = require('electron-updater');
    const u = new NsisUpdater({ provider: 'generic', url: process.env.MG_UPDATE_TEST_URL });
    u.forceDevUpdateConfig = true;
    u.logger = console;
    return u;
  }
  if (!app.isPackaged) return null;
  return require('electron-updater').autoUpdater;
}

function startUpdateCheck() {
  const updater = makeUpdater();
  if (!updater) return loadGame();
  updater.autoDownload = true;
  updater.autoInstallOnAppQuit = true;
  let found = false;
  const timer = setTimeout(() => { if (!found) loadGame(); }, CHECK_WAIT_MS);
  updater.on('update-available', () => { found = true; clearTimeout(timer); showUpdate('download', 0); });
  updater.on('update-not-available', () => { clearTimeout(timer); loadGame(); });
  updater.on('download-progress', p => showUpdate('download', p.percent));
  updater.on('update-downloaded', () => {
    if (gameLoaded) return; // لقى التحديث بعد ما فتحت اللعبة (نت بطيء): ينثبت لما يقفل اللعبة
    showUpdate('restart');
    setTimeout(() => {
      if (process.env.MG_UPDATE_TEST_URL) { console.log('MG_TEST would quitAndInstall'); return; }
      updater.quitAndInstall(true, true);
    }, 1200);
  });
  updater.on('error', () => { clearTimeout(timer); loadGame(); });
  updater.checkForUpdates().catch(() => { clearTimeout(timer); loadGame(); });
}

// حجم النافذة العادي (لما ما تكون ملء الشاشة): 1280×720 أو أصغر إذا الشاشة صغيرة، في النص
function normalBounds() {
  const wa = screen.getDisplayMatching(win.getBounds()).workArea;
  const w = Math.min(1280, Math.round(wa.width * 0.9)), h = Math.min(720, Math.round(wa.height * 0.9));
  return { x: wa.x + Math.round((wa.width - w) / 2), y: wa.y + Math.round((wa.height - h) / 2), width: w, height: h };
}

function createWindow() {
  // النافذة تنفتح عادية وبعدين تصير ملء الشاشة، عشان ويندوز يعرف حجمها العادي لما تطلع من ملء الشاشة
  win = new BrowserWindow({
    width: 1280,
    height: 720,
    center: true,
    show: false,
    backgroundColor: '#000000',
    title: 'Mohammed Game',
    autoHideMenuBar: true,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
      contextIsolation: false,
      nodeIntegration: false,
      sandbox: false,
      backgroundThrottling: false
    }
  });
  win.on('page-title-updated', e => e.preventDefault());
  win.once('ready-to-show', () => { applyFS(); win.show(); });
  // ويندوز أحياناً يرسل أحداث ملء الشاشة متأخرة أو مرتين، فبعد كل تغيير نتأكد إن النافذة على اختيار اللاعب
  win.on('enter-full-screen', syncFS);
  win.on('leave-full-screen', syncFS);
  win.webContents.on('did-finish-load', sendFS);
  win.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  win.webContents.on('will-navigate', e => e.preventDefault());
  win.loadFile(path.join(__dirname, 'updater.html'), { query: { v: app.getVersion() } });
  win.webContents.once('did-finish-load', startUpdateCheck);
}

// ملء الشاشة: اختيار اللاعب (want) هو الأساس. اللعبة تعرض الاختيار، والبرنامج يتأكد إن النافذة تطابقه
let syncTimer = null, syncTries = 0;
function want() { return settings.fullscreen !== false; }
function sendFS() { if (win && !win.isDestroyed()) win.webContents.send('fs-state', want()); }
function applyFS() {
  if (!win || win.isDestroyed()) return;
  if (win.isFullScreen() !== want()) win.setFullScreen(want());
}
function fixNormalBounds() {
  // إذا ويندوز خلى النافذة بحجم الشاشة بعد ما طلعت من ملء الشاشة، نرجعها لحجمها العادي
  if (win.isMaximized()) win.unmaximize();
  const b = win.getBounds(), wa = screen.getDisplayMatching(b).workArea;
  if (b.width >= wa.width - 2 || b.height >= wa.height - 2) win.setBounds(normalBounds());
}
function syncFS() {
  clearTimeout(syncTimer);
  syncTimer = setTimeout(() => {
    if (!win || win.isDestroyed()) return;
    if (win.isFullScreen() !== want() && syncTries < 3) { syncTries++; win.setFullScreen(want()); syncFS(); return; }
    syncTries = 0;
    if (!win.isFullScreen()) fixNormalBounds();
    sendFS();
  }, 400);
}
ipcMain.on('set-fullscreen', (e, on) => {
  if (!win) return;
  settings.fullscreen = !!on; saveSettings();
  syncTries = 0;
  applyFS();
  syncFS();
});

app.on('second-instance', () => { if (win) { if (win.isMinimized()) win.restore(); win.focus(); } });

app.whenReady().then(() => {
  Menu.setApplicationMenu(null);
  protocol.handle('https', req => {
    const u = new URL(req.url);
    if (u.hostname === 'fonts.googleapis.com') return fontResponse(path.join(FONTS, 'google.css'), 'text/css');
    if (u.hostname === 'fonts.gstatic.com') return fontResponse(path.join(FONTS, path.normalize(u.pathname)), 'font/woff2');
    return net.fetch(req, { bypassCustomProtocolHandlers: true });
  });
  createWindow();
});

app.on('window-all-closed', () => app.quit());
