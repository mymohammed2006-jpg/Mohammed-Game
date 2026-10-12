// يخلي زر F11 داخل اللعبة يكبّر ويصغّر نافذة البرنامج نفسها، بنفس طريقة المتصفح.
const { ipcRenderer } = require('electron');
let full = false;
ipcRenderer.on('fs-state', (e, on) => {
  // مؤشر الماوس يبان لما تكون اللعبة نافذة
  const c = document.getElementById('c'); if (c) c.style.cursor = on ? 'none' : 'default';
  if (on === full) return;
  full = on;
  document.dispatchEvent(new Event('fullscreenchange'));
});
Object.defineProperty(Document.prototype, 'fullscreenElement', {
  configurable: true,
  get() { return full ? document.documentElement : null; }
});
Element.prototype.requestFullscreen = function () { ipcRenderer.send('set-fullscreen', true); return Promise.resolve(); };
Document.prototype.exitFullscreen = function () { ipcRenderer.send('set-fullscreen', false); return Promise.resolve(); };
// إعدادات خاصة ببرنامج الكمبيوتر (اللعبة تعرض خيار V-Sync بس إذا لقت هذا)
window.mgApp = {
  vsync: ipcRenderer.sendSync('get-vsync'),
  openSettings: ipcRenderer.sendSync('get-open-settings'),
  setVsync: on => ipcRenderer.send('set-vsync', !!on)
};
