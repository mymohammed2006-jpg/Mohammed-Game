const { _electron } = require('/opt/node-tools/node_modules/playwright');
const fs = require('fs'), path = require('path');
(async () => {
  const b = await _electron.launch({ executablePath: process.env.HOME + '/electron44/electron', args: ['--no-sandbox', '/tmp/claude-0/-home-claude-mohammed-game/6987e1d9-600c-5562-9060-2a0b2e86dbce/scratchpad/mg/tools/compare/electron-main.js'], env: { ...process.env, MG_HARNESS: path.resolve('test.html') } });
  const p = await b.firstWindow();
  await p.waitForFunction(() => document.title === 'done', null, { timeout: 120000 });
  const d = await p.evaluate(() => document.getElementById('c').toDataURL('image/png'));
  fs.writeFileSync('chrome.png', Buffer.from(d.split(',')[1], 'base64'));
  await b.close();
})();
