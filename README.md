# Mohammed Game

لعبة إيقاع وتفادي. حمّل آخر نسخة من [Releases](../../releases/latest): الملف **Mohammed-Game-Setup.exe**، وشغله مرة وحدة.
بعدها اللعبة تتحدث لحالها كل ما تفتحها.

أول مرة ويندوز يطلع تحذير أزرق: اضغط **More info** وبعدها **Run anyway**.

---

For maintainers: `game/` is the unchanged HTML game, the rest wraps it in Electron (`main.js`, `preload.js`, `updater.html`).
`package.json` "version" must be bumped for every release (8.0.0 = game v8). Workflow "تجربة بناء" builds on every push (no publish);
workflow "حدث للكل" publishes a GitHub Release that installed apps pick up via electron-updater. It is run by hand only.
