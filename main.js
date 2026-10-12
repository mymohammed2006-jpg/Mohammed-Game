// Mohammed Game: يفتح اللعبة (game/index.html) في نافذة خاصة بدون متصفح.
// من نسخة 8: أول ما يفتح يدور على تحديث في GitHub، وإذا فيه يحمله ويعيد التشغيل لحاله.
const { app, BrowserWindow, ipcMain, protocol, net, Menu } = require('electron');
const path = require('path');
const fs = require('fs');

app.commandLine.appendSwitch('autoplay-policy', 'no-user-gesture-required');

// إعدادات البرنامج (V-Sync) محفوظة في ملف، لأنها لازم تنقرى قبل ما تفتح النافذة
const SETTINGS_FILE = path.join(app.getPath('userData'), 'settings.json');
let settings = { vsync: true, fullscreen: true };
try { settings = Object.assign(settings, JSON.parse(fs.readFileSync(SETTINGS_FILE, 'utf8'))); } catch (e) {}
function saveSettings() { try { fs.writeFileSync(SETTINGS_FILE, JSON.stringify(settings)); } catch (e) {} }
if (!settings.vsync) {
  app.commandLine.appendSwitch('disable-gpu-vsync');
  app.commandLine.appendSwitch('disable-frame-rate-limit');
}

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

function createWindow() {
  win = new BrowserWindow({
    width: 1280,
    height: 720,
    useContentSize: true,
    fullscreen: settings.fullscreen !== false,
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
  const sendFS = () => win.webContents.send('fs-state', win.isFullScreen());
  // نتذكر اختيار ملء الشاشة (من الإعدادات أو F11) للمرة الجاية
  const rememberFS = () => { settings.fullscreen = win.isFullScreen(); saveSettings(); };
  win.on('enter-full-screen', rememberFS);
  win.on('leave-full-screen', rememberFS);
  win.on('enter-full-screen', sendFS);
  win.on('leave-full-screen', sendFS);
  win.webContents.on('did-finish-load', sendFS);
  win.webContents.setWindowOpenHandler(() => ({ action: 'deny' }));
  win.webContents.on('will-navigate', e => e.preventDefault());
  win.loadFile(path.join(__dirname, 'updater.html'), { query: { v: app.getVersion() } });
  win.webContents.once('did-finish-load', startUpdateCheck);
}

ipcMain.on('get-vsync', e => { e.returnValue = !!settings.vsync; });
ipcMain.on('set-vsync', (e, on) => { settings.vsync = !!on; saveSettings(); });
ipcMain.on('set-fullscreen', (e, on) => { if (win) win.setFullScreen(!!on); });

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
