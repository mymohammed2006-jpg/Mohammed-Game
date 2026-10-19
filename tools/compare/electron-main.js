// Electron 44 (the version the PC app ships) opening the harness page, driven by driver.js through Playwright.
const { app, BrowserWindow } = require('electron');
app.commandLine.appendSwitch('autoplay-policy', 'no-user-gesture-required');
app.commandLine.appendSwitch('force-device-scale-factor', '1');
app.disableHardwareAcceleration();
app.whenReady().then(() => {
  const w = new BrowserWindow({ width: 1280, height: 720, useContentSize: true, show: true, backgroundColor: '#000000',
    webPreferences: { backgroundThrottling: false, contextIsolation: false } });
  w.loadFile(process.env.MG_HARNESS);
});
