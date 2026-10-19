# Porting Mohammed Game to Godot (C#)

The goal is the v39 game EXACTLY as it is: same levels, same look, same feel, same Easy, same replays (old ones too).
No new features and no "improvements" while porting. If the original does something odd, the port does the same odd thing.

The original is `/mnt/project-files/Mohammed_Game/Mohammed Game/index.html` (8058 lines). Line numbers below refer to it.

## Getting started in a new container

1. Copy the shared project to your own scratch folder (never build inside the shared folder):
   `cp -r /mnt/project-files/godot-port <scratch>/mg`
2. Run `bash <scratch>/mg/tools/setup.sh`. It installs the .NET 8 SDK, xvfb and Godot 4.4.1 (.NET) if they are missing, imports the project and builds it.
3. Check the setup with `bash tools/compare/run.sh L1 n` (always start scripts with `bash`: the shared folder drops the execute bit). It should print `state: 0 lines differ` (see "Checking your level" below).

## How the code is laid out

| Path | What it is | Owner |
|---|---|---|
| `src/Canvas/*` | The browser canvas rebuilt on Godot: `g.fillRect`, `g.arc`, shadows, gradients, text, clip. It is checked against Chrome. | leader |
| `src/Core/*` | JS helpers (`JS.cs`), `performance`/`localStorage` (`Web.cs`), the WebAudio engine (`AudioEngine.cs`), and the Godot node (`Main.cs`) | leader |
| `src/Game/Core.cs` | Lines 47-62 and 150-204: timing (`B`, `BT`), colours, `rnd`, `burst`, `Bullet`, `Laser`, `Bomb`, `Spinner`, `Ring`, `poly`, `circle`, `obbHit` | leader |
| `src/Game/Shared.cs` | Helpers that one level defines and other levels use: `SqBullet Sweep GapWall Snake segDist Shard Slash heartPath GREY ShardTri R2 Squeeze GC/GR/GW/GH RingSqueeze`, and `ezT` with `Tier` | leader |
| `src/Game/Play.cs`, `Loop.cs`, `Replay.cs`, `Run.cs`, `Afk.cs`, `Sound.cs`, `Input.cs`, `Window.cs`, `Boot.cs` | Running a level, Easy, the replay player, the in-game screens, AFK tables, sound, input, main loop | leader |
| `src/Game/Levels/<Level>.cs` | One file per level: its own helpers plus its `build…()` | the level's thread |
| `src/Game/Stubs.cs` | Placeholders for the menus, online, account, practice and more. Each says which original lines replace it. | UI thread |
| `src/Game/Data.cs`, `data/*.json`, `songs/*.mp3` | Energy tables, beat lists, bot runs, AFK tables, songs (all extracted from the original) | generated |
| `test/BotRun.cs`, `tools/compare/*` | The automatic check | leader |

Rules for files:
- Change only files you own. Shared files (`Core.cs`, `Shared.cs`, `Play.cs` and the others) belong to the leader thread ("التحديث الكبير").
  If you need something there, ask the coordinator to pass it on. Do not work around it with a copy.
- New helpers that only your level uses go in your level file, under the original name.
- When you deliver, copy back only your own files into `/mnt/project-files/godot-port/` (re-read the target before copying; never copy `.godot/`, `bin/` or `obj/`).

## Porting rules (JS to C#)

Everything lives in `public static partial class G` and keeps its JS name. That way a line can be checked against the original by eye.

- **Numbers**: every JS number becomes `double`. Watch integer division: `i/n` with two ints must be `(double)i/n`. `W` and `H` are double constants.
- **Rounding**: use `round(x)` from `JS` (it is `floor(x+0.5)`, like JS), never `Math.Round`.
  Use `Math.Floor`, `Math.Ceiling` and `Math.Trunc` as in JS. `x|0` becomes `i32(x)`, and `>>>0` becomes `u32`.
- **Numbers inside strings**: always use `str(x)` (JS `Number.toString`), e.g. ``$"rgba(255,42,109,{str(0.2+0.4*k)})"``.
  Never put a double straight into an interpolated string. `x.toFixed(n)` becomes `toFixed(x,n)`. Arabic digits (`toLocaleString('ar-EG')`) become `ar(x)`.
- **Truthiness**: JS `a||b` on numbers means "if a is 0 or NaN or undefined, use b". Write that out. `p.sk!==undefined` uses a nullable field (`p.sk ?? …`).
- **Randomness**: the order of `rnd()` (seeded, level logic) and `random()` (`Math.random`, effects) calls must be identical.
  Same count, same order, same place. C# evaluates arguments left to right like JS. Watch `&&` and `||` short-circuits and loops that stop early.
  Never cache, skip or add a random call.
- **Hazards**: a JS object `{x,y,update(t,dt){},draw(t){},hit(p,t){}}` becomes `var h = new Hz(); h.update = …; h.draw = …; h.hit = …; return h;`.
  `this.x` becomes `h.x`. Fields the JS sets on the object (`this.l=true`, `b.rot=…`) become captured locals.
  If other code reads them from the hazard (`h.foo` from outside), use a real `Hz` field, or `h.o`.
  If the JS object has `x`/`y` keys, set `h.x`/`h.y` the same way: the check sums them.
- **Hazard lists**: `hazards.push(a,b)` becomes `hazards.Add`/`AddRange`. A function that returns either a hazard or an array becomes one that returns `Hz[]`.
- **Closures in loops**: a JS `let` in a `for` loop makes a fresh variable each time. C# `for` variables are shared.
  If a lambda captures the loop variable, copy it to a local inside the loop body first (`int i2 = i;`).
- **Objects as options** (`Laser({...o, ta:…})`): use the option classes (`LaserO`, `SpinnerO`) with `Clone()`, or add a small class in your file.
- **Arrays of points** become `(double x, double y)` tuples or `double[]` (whichever reads closer to the original).
- **Sorting**: use `JS.sort(list, cmp)`, which is stable like JS. Never use `List.Sort`, which is not stable.
- **Duplicate function names**: when the original declares the same `function` twice, the LAST one is the real one. Earlier copies are dead code, so do not port them.
  No level actually has dead copies: L8's global `buildLevel8` at 4906 (with 4470-4905) is the v2-replay shape and 5165-5842 is the IIFE `buildLevel8pre` (live); F7 is NOT a dead copy: `buildFriend7` at 2984 and its helpers 2874-2983 are live (v2 replays and the AFKNEW knives), and `buildFriend7v30` is an IIFE with its own local copies.
- **Old shapes stay**: `buildFriend7v30` (F7 for v1/v3 replays and now), `buildFriend7` (F7 AFK version for v2 replays), `buildLevel8pre` (now) and `buildLevel8` (v2 replays) are all needed.
- **Drawing**: call the canvas exactly like the JS does, in the same order: `g.save()`, `g.fillStyle = "…"`, `g.arc(…)`, and so on. Fonts are strings like in JS: `g.font = $"800 {str(size)}px {FONT}"`.
- **AFK makers**: if the original's `AFKMK` (line 6513) has an entry for your level, register it in your level file:
  `static readonly int _afkL3 = AfkReg("buildLevel3", (x, y, b, v, w) => One(…));` (return `Hz[]`).
  L1, L2, F2 and F4 are already registered in `Afk.cs`.

`Level1.cs` is the model port. Read it before starting.

## Checking your level

`bash tools/compare/run.sh <id> <n|e>` plays the bot's S run in the original (headless Chrome) and in the port (Godot), 60 frames a second, with the same seeded `Math.random`. It compares:
- **state**: every frame's song time, square position and HP, hazard count, sum of hazard x/y, particles, `rngS`, checkpoint and hits.
  It must say `state: 0 lines differ`. The first differing line points at the first wrong frame. Find which event or hazard fired there.
- **pixels**: one frame per second.
  Small differences are expected: glow softness (Chrome's software blur), text anti-aliasing, and 1-pixel edges. The mean is about 0.1-1.5/255.
  Look at the worst frames side by side: `python3 tools/compare/sbs.py <orig.png> <port.png> out.png`. They must look the same to the eye.

Run all of these for each of your levels:
1. `run.sh <id> n` and `run.sh <id> e` (bot run, Normal and Easy)
2. `MODE=play run.sh <id> n` and `MODE=play run.sh <id> e` (real play with a key script: movement, dash, getting hit, dying, restarting from a checkpoint)
3. `RPV=2 run.sh <id> n` and `RPV=2 run.sh <id> e` (as a v37/v38 replay) for every level with AFK data (L1-L7, F1-F7) and L8
4. `RPV=1 run.sh F7 n` and `RPV=1 run.sh F7 e` (F7's oldest shape)

Useful options: `run.sh <id> <m> <every> <from> <to>` captures every `every`-th frame between `from` and `to`.
To dig into a frame, add temporary prints on both sides: in the driver through `__mg('…')`, and in Godot with `GD.Print`.

A level is done only when every run above shows `state: 0 lines differ` and the pixels look the same. Then copy your files back and tell the leader through the coordinator.

## What the UI thread ports

The UI thread ports everything in `Stubs.cs`, in the same exact way:
- the menus and screens of `draw()` (`drawMenus`) and `step()` (`stepMenus`);
- news, points, account and online sync (Firebase REST, same URLs and documents);
- board, profile, replay upload and the replay overlay, practice, transitions, the gate and splash, the level intro, and the song preview.

The settings screen gets one new row for the screen mode: Window, Borderless or Fullscreen. `Window.cs` already has `screenMode`, `setScreen` and `screenLabel`. This is the only new feature in the port.
Its check is the same idea: drive the menus with a key script in both games and compare frames. The driver's `MODE=play` shows how.
