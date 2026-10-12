// يخلي زر F11 داخل اللعبة يكبّر ويصغّر نافذة البرنامج نفسها، بنفس طريقة المتصفح.
const { ipcRenderer } = require('electron');
let full = false;
function setFull(on) {
  // مؤشر الماوس يبان لما تكون اللعبة نافذة
  const c = document.getElementById('c'); if (c) c.style.cursor = on ? 'none' : 'default';
  if (on === full) return;
  full = on;
  document.dispatchEvent(new Event('fullscreenchange'));
}
// البرنامج يرسل اختيار اللاعب بعد ما يتأكد إن النافذة طابقته
ipcRenderer.on('fs-state', (e, on) => setFull(!!on));
Object.defineProperty(Document.prototype, 'fullscreenElement', {
  configurable: true,
  get() { return full ? document.documentElement : null; }
});
// المربع في الإعدادات يتغير على طول مع الضغطة، والبرنامج يكبّر أو يصغّر النافذة
Element.prototype.requestFullscreen = function () { setFull(true); ipcRenderer.send('set-fullscreen', true); return Promise.resolve(); };
Document.prototype.exitFullscreen = function () { setFull(false); ipcRenderer.send('set-fullscreen', false); return Promise.resolve(); };
// اللعبة تعرف إنها داخل برنامج الكمبيوتر من هذا
window.mgApp = {};
