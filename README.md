<p align="center">
  <img src="configurator/FlowaGunSetup.ico" width="64" alt="XEMU LightGun Edition icon"/>
</p>

<h1 align="center">XEMU LightGun Edition — By Code Flow</h1>

<p align="center">
  A modified build of <a href="https://github.com/xemu-project/xemu">xemu</a> (Original Xbox emulator)
  with <b>native lightgun support</b>: Sinden Lightgun and any HID mouse gun.<br/>
  Born to make <b>Silent Scope Complete</b> playable — SS1, SS2 and SS3 all working,
  now at <b>full speed even on low-end PCs</b>.<br/>
  <b>Available for Windows and — new in v3.1 — for <a href="https://batocera.org">Batocera</a> Linux</b> (arcade cabinets!).
</p>

<p align="center">
  <a href="https://github.com/flowa1911you/xemu-sinden-lightgun/releases"><b>⬇ DOWNLOAD (Releases)</b></a>
  &nbsp;·&nbsp;
  <a href="https://www.youtube.com/@flowachannel4731"><b>📺 YouTube channel</b></a>
  &nbsp;·&nbsp;
  <a href="CHANGELOG.md"><b>📋 Full changelog</b></a>
</p>

---

## 🕹️ What's new in v3.1 — Batocera Edition

The LightGun Edition now runs **natively on Batocera Linux** (43+): turn a
mini PC into an Xbox lightgun arcade machine.

- **Plug-and-play guns** — Batocera detects your lightguns (Sinden, Gun4IR,
  OpenFIRE, AimTrak, ...) and assigns them to player ports **automatically at
  every game launch**. No manual binding, no stale device ids, ever.
  Player order simply follows the USB ports.
- **New Linux input backend (evdev)** — per-device gun input read straight
  from the kernel, including gun extra buttons that the window system never
  delivers (the Sinden D-pad navigates game menus out of the box)
- **White Sinden borders, audio and 16:9 handled natively by Batocera** —
  games launch from EmulationStation like any other system
- **LIGHTGUN options inside the Batocera menus**: number of guns (1 by
  default — most Xbox gun games refuse two), aim smoothing, aim sensitivity
- **Flowa GunSetup for Batocera** — a native configurator in the Ports menu
  with **live button capture**: press a gun button, a keyboard key or even an
  arcade coin button and it's mapped
- **3-step install with zero terminal**: copy the package into the Batocera
  network share, add your BIOS, reboot. A boot-time auto-installer does the
  rest, keeps itself installed across reboots and re-applies after Batocera
  updates.

**The Windows package is unchanged** — see the Windows guide below.

## 🚀 What's new in v3.0

- **Massive performance overhaul** — a deep profiling campaign inside the NV2A
  GPU core found and fixed a chain of per-draw CPU bottlenecks (texture state
  tracking, descriptor pools, bulk vertex processing and more). Per-frame CPU
  cost roughly **cut in half**: Silent Scope Complete went from a slideshow to
  its full frame rate cap on an Intel iGPU laptop. **Every game benefits.**
- **Fixed the long-standing random freeze when starting a game** — an ABBA
  deadlock between the disc DMA and the GPU thread, found with a debugger and
  fixed for good. Affected all games since forever.
- **Full button mapping** — new *Mapping* tab in the configurator: Trigger, B,
  X, Start, Back, D-Pad, plus the *Additional* pad inputs (Y, White, Black,
  stick clicks, analog triggers, Guide — for service/extra modes in games like
  Virtua Cop). Bind them to any button of **any detected mouse** or **any
  keyboard key**: click a slot, press the input within 10 seconds, done. The
  **aim is deliberately not mappable** — it always follows the raw input
  device selected in Players.
- **100% portable** — `xemu.toml` now lives next to the exe (native xemu
  portable mode) and game ISOs inside the folder are saved with relative
  paths. Copy the folder to a USB stick or another PC and it just works.
  Zero traces in AppData.
- **New startup splash** with auto-close, and the configurator rebranded to
  **Code Flow GunSetup v3.0**.

See [CHANGELOG.md](CHANGELOG.md) for the complete list.

## Features

- **Emulated EMS TopGun II lightgun** as a true low-level USB device
  (VID `0x0b9a` / PID `0x016b`, XID subtype `0x50`) — games genuinely detect a
  lightgun on the port, including in-game calibration
  (`XInputSetLightgunCalibration` reaches the emulated gun, like real hardware)
- **Per-device gun input, DemulShooter-style**: every HID pointer device is
  listed individually by name (Windows Raw Input API). Assign a specific gun to
  a specific player port — **two guns for 2-player games** work out of the box
- **Native absolute-coordinate aiming** (Sinden mouse mode), mapped to the
  actual game render area with correct offscreen (reload) reporting
- **Full button mapping** to any mouse or keyboard key (v3.0) — aim stays
  locked to the selected gun
- **Adaptive aim smoothing** (1-euro filter: steady crosshair when still, zero
  added latency on fast moves) + aim sensitivity setting
- **Code Flow GunSetup** — zero-install configurator: game ISO, gun-to-player
  assignment, button mapping, graphics settings, machine files status. One
  place for everything
- **100% portable package**: settings, BIOS, MCPX boot ROM, HDD image, EEPROM
  and game ISO all live inside the app folder
- **Lightgun-friendly UX**: always-hidden cursor, hideable menu bar, and guards
  so the trigger can't toggle fullscreen or open menus
- **Major NV2A performance fixes** (v3.0) and the **Silent Scope 2 Vulkan
  crash fix** (submitted upstream to the xemu project)
- **Batocera Linux support** (v3.1): native evdev per-device input backend,
  automatic gun-to-player assignment via Batocera, options in the
  EmulationStation menus and a native GTK configurator in the Ports menu

## 🪟 Windows — Quick start

1. Download the **Windows** zip from [Releases](https://github.com/flowa1911you/xemu-sinden-lightgun/releases) and extract it anywhere
2. Drop your files into the folders (each one contains a readme):
   - `bios\` — Xbox flash BIOS image
   - `mcpxbootrom\` — MCPX boot ROM
   - `harddisk\` — Xbox HDD image
   - `eeprom\` — optional (created automatically on first start)
3. Run **`FlowaGunSetup.exe`**: select your game ISO, assign your gun to
   Player 1 (already set to *Lightgun (EMS TopGun II)*), then **Save & Launch**
4. In game: **left click = trigger**, right click = B, middle click = Start —
   or remap everything in the **Mapping** tab

> ⚠️ BIOS, boot ROM, HDD image and games are **not included** — dump them from
> your own console and discs. The game region must match your BIOS/EEPROM
> region (EU ↔ EU, US ↔ US).

## 🕹️ Batocera (Linux) — Installation & guide

> This is a **separate package** from the Windows one: download
> `xemu-lightgun-batocera-edition-*.zip` from
> [Releases](https://github.com/flowa1911you/xemu-sinden-lightgun/releases).
> Requires **Batocera 43 or newer** (x86_64). Your lightguns must be
> supported by Batocera itself (Sinden, Gun4IR, OpenFIRE, AimTrak, ...).

### Install (3 steps, no terminal needed)

1. From your PC, open the Batocera network share in the file explorer:
   **`\\BATOCERA\share`** (or use a USB stick / SFTP to reach `/userdata`)
2. Extract the zip and copy the **content of its `share` folder** into the
   Batocera share, merging with the existing `system/` and `roms/` folders.
   Then add your own files, exactly like stock xemu on Batocera:
   - `share\bios\Complex_4627.bin` and `share\bios\mcpx_1.0.bin` (exact names)
   - your game ISOs in `share\roms\xbox\`
3. **Fully reboot Batocera**: MENU → QUIT → **RESTART SYSTEM** (restarting
   EmulationStation alone is *not* enough!). On boot the package installs
   itself in a few seconds — you will see EmulationStation restart once
   automatically: that's the installer finishing. Done.

> If you already have a `system/custom.sh` on your Batocera, merge the two
> files manually instead of overwriting.

### How it works on Batocera

- **Guns are assigned automatically**: at every game launch Batocera detects
  the connected guns and plugs them into the virtual Xbox ports (gun 1 →
  player 1, ...). To swap who is player 1, swap the USB ports. Nothing to
  configure.
- **Options live in the Batocera menus**: highlight an Xbox game → SELECT →
  advanced options (or MENU → GAMES SETTINGS → PER SYSTEM ADVANCED
  CONFIGURATION → XBOX) → **LIGHTGUN** submenu: number of guns, aim
  smoothing, aim sensitivity. Set **USE GUNS = ON** to get the white Sinden
  borders (drawn natively by Batocera).
- **Number of guns is 1 by default** on purpose: most Xbox gun games
  (e.g. Silent Scope Complete) refuse to start with two lightguns plugged
  into the console. Raise it to 2 for 2-player games (House of the Dead 3,
  Virtua Cop 3, ...).
- **Flowa GunSetup** (Ports menu): native configurator for everything else —
  full **button mapping with live capture** (17 slots: trigger, A/B/X/Y,
  Start, D-Pad, White/Black, triggers, Guide...). Click *Capture*, press any
  gun button, keyboard key or even an arcade coin button, done. The gun
  D-pad already works without any mapping. The **aim is deliberately not
  mappable** — it always follows the gun.
- In-game you can still open the xemu menu (with its aim sliders) using the
  gun/pad hotkeys that Batocera sets up automatically.

### Batocera troubleshooting

- **Game says "Only one lightgun controller can be used"** → that game wants
  a single gun: set LIGHTGUNS = 1 gun in the game options (the default)
- **A game closes by itself a few seconds after launch** → a wireless pad
  left in a stale state after an EmulationStation restart: unplug/replug the
  pad dongle
- **Installer log**: `share\system\xemu-flowa\install.log`
- **Uninstall**: delete `system/custom.sh`, `system/xemu-flowa/`,
  `system/flowa-gunsetup/` and `roms/ports/FlowaGunSetup.sh`, then remove the
  overlay (`batocera-overlay-clear` or delete `/boot/boot/overlay`) and
  reboot — everything returns to stock.

## Known limitations

- Windows: gun-to-port bindings are tied to the physical USB port (like
  DemulShooter). Batocera: assignment is automatic, order follows USB ports.
- ReShade users (Windows): hook xemu as **OpenGL** (the presentation layer is
  OpenGL even when the internal renderer is Vulkan)

## Source code

All modifications live in the [`flowa-lightgun`](https://github.com/flowa1911you/xemu-sinden-lightgun/tree/flowa-lightgun)
branch ([full diff vs upstream](https://github.com/flowa1911you/xemu-sinden-lightgun/compare/master...flowa-lightgun)),
including the Linux/evdev backend used by the Batocera Edition.
Build like regular xemu (MSYS2 UCRT64 on Windows: `./build.sh`; the Linux
AppImage is built by the repository CI). The Windows configurator compiles
with the C# compiler bundled with Windows; the Batocera configurator is
Python/GTK, no build needed.

## Credits

- Made by **Code Flow (flowa)** — [youtube.com/@flowachannel4731](https://www.youtube.com/@flowachannel4731).
  Subscribe so you don't miss future releases and updates!
- Based on [xemu](https://xemu.app) by Matt Borgerson and contributors —
  see also the upstream site for general documentation

## License

Same as upstream xemu (GPLv2). See [LICENSE](LICENSE).
