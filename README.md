# Mohammed Game

لعبة إيقاع وتفادي. حمّل آخر نسخة من [Releases](../../releases/latest): الملف **Mohammed-Game-Setup.exe**، وشغله مرة وحدة.
بعدها اللعبة تتحدث لحالها كل ما تفتحها.

أول مرة ويندوز يطلع تحذير أزرق: اضغط **More info** وبعدها **Run anyway**.

---

For maintainers: since v40 the game is a Godot 4.4.1 .NET (C#) project (`project.godot`, code in `src/`, songs in `songs/`).
`config/version` in `project.godot` must be bumped for every release (40.0.0 = game v40). `bash tools/release/build.sh` builds
the game, the installer (`installer/installer.nsi`) and `latest.yml`; the file names are the old Electron app's, so its
auto-update installs the Godot game in place and the saves move over. Workflow "تجربة بناء" builds on every push (no publish);
workflow "حدث للكل" publishes a GitHub Release. It is run by hand only. Porting notes: `PORTING.md`.
