# Changelog — XEMU LightGun Edition (By Code Flow)

## v4.0 (July 2026)

- Startup splash: added a **"Don't show this again"** checkbox (returning
  from v2) and a Close button. Ticking the box writes an `EULA.txt` marker
  next to the executable; while that file exists the splash is skipped
  entirely. Deleting the file brings it back. While the box is ticked the
  auto-close countdown pauses, so the splash can't vanish mid-decision.
- All support links (splash, configurator credits, generated
  `flowa_config.ini` header, README) now point to the project's GitHub
  profile: https://github.com/flowa1911you
- Removed the community mention from the configurator credits.
- Splash and Code Flow GunSetup rebranded to v4.0.

## v3.1 — Batocera Edition (July 2026)

### New platform: Batocera Linux (43+)
- New **evdev input backend** (`ui/xemu-evdev.c`): pointer devices are
  enumerated individually from the kernel (same per-device philosophy as
  the Windows Raw Input backend), with hotplug support. The
  platform-independent aim pipeline (button mapping, adaptive smoothing,
  sensitivity, offscreen detection) is now shared between Windows and
  Linux — Windows behavior is unchanged.
- **Frontend gun assignment**: saved bindings accept `evdev:<device>`
  aliases, so Batocera's launcher assigns detected guns to player ports
  automatically at every game launch.
- **Gun extra buttons read straight from the device** (`k<code>` mapping
  specs): lightgun D-pads and extra buttons work even when the windowing
  system never delivers them (X11 ignores joystick-class devices). The
  Sinden D-pad drives the Xbox D-pad out of the box.
- **Batocera integration package**: boot-time auto-installer (custom.sh +
  idempotent patcher) that teaches Batocera's launcher about guns, adds a
  LIGHTGUN submenu (guns count, aim smoothing, aim sensitivity) to the
  EmulationStation menus and installs **Flowa GunSetup**, a native GTK
  configurator in the Ports menu with live button-capture mapping.
- On Linux the Windows-only portable INI, machine-folder autodetect and
  welcome popup are disabled: the frontend owns the configuration.

## v3.0 (July 2026)

### Performance — Silent Scope Complete finally playable
A full profiling campaign on the NV2A emulation uncovered a chain of
per-draw CPU costs that crushed draw-call-heavy titles (~4,500+ draws
per frame). Average scenes went from ~21 fps to the game's full 30 fps
cap; worst-case scenes from ~15 fps to ~25 fps. Per-draw CPU cost cut
by half. All fixes benefit every game, not just Silent Scope:

- Vulkan: descriptor set pool enlarged 1024 → 8192 so a whole frame of
  draws fits without forcing a mid-frame GPU pipeline drain (was
  stalling ~4x per frame).
- Vulkan: texture bindings are only flagged as changed when the bound
  texture instance really differs. Games re-send identical texture
  state before every draw; this used to force a fresh descriptor set
  write and a full pipeline-dirty pass per draw. A per-creation
  sequence id makes the comparison safe against driver handle reuse.
- Texture state registers (SET_TEXTURE_*) only mark a slot dirty when
  the written value actually changes; disabled slots no longer keep the
  dirty flag stuck (which forced full texture re-validation on every
  draw forever).
- Texture content re-validation (possibly-dirty tracking) is throttled
  to once per frame per texture instead of once per draw.
- NV097 INLINE_ARRAY vertex data is consumed in bulk with a single
  memcpy per batch instead of paying the method-dispatch loop for every
  single word of geometry.
- The CTX_SWITCH register refresh in the method dispatcher is skipped
  while the subchannel is unchanged (it ran for every method word).

### Stability
- Fixed a long-standing random freeze when starting a game: an ABBA
  deadlock between IDE DMA (game loading from disc, holding the QEMU
  BQL while waiting for a GPU memory-callback flush) and the pfifo
  thread (raising a PGRAPH interrupt, waiting for the BQL). Interrupt
  raising from the GPU thread is now deferred to a main-loop bottom
  half. Affected every game, at random, since forever.
- Fixed the Windows resource compile breaking when the git tag is not
  in x.y.z form (e.g. "v0.9-beta").

### New features
- Button mapping (Mapping tab in the configurator + [mapping] section
  in flowa_config.ini): trigger, B, X, Start, Back, the D-Pad and the
  additional pad inputs (Y, White, Black, stick clicks, analog triggers,
  Guide - for service/extra modes in games like Virtua Cop) can be
  bound to any button of any detected mouse (even a different one from
  the aiming device) or to keyboard keys. Click a slot, press the
  desired input within 10 seconds, done. The AIM is deliberately not
  mappable: it always follows the raw input device selected in Players.
- 100% portable package: xemu.toml now ships next to xemu.exe (native
  xemu portable mode), so every setting lives inside the app folder; a
  game ISO placed inside the folder is saved with a relative path.
  Move or copy the folder to another PC and everything keeps working
  (machine files were already portable via the bios/mcpxbootrom/
  harddisk/eeprom folders).
- New startup splash: "XEMU LightGun Edition v3.0 By Code Flow", large
  readable text, support link, closes automatically after 8 seconds
  (replaces the old welcome popup + EULA checkbox).

### Configurator
- Rebranded to Code Flow GunSetup v3.0 (window title, header and exe
  metadata; the executable keeps the FlowaGunSetup.exe name) and
  extended with the Mapping tab described above. Disc images inside
  the app folder are shown and saved with a relative path.

## v0.9-beta (July 2026)
- First public release: Sinden/HID-mouse lightgun support via per-device
  Raw Input, EMS TopGun II (LLE) emulated device, lightgun calibration,
  adaptive 1-euro aim filter, click-protection options, Flowa GunSetup
  configurator, portable machine-file folders with EEPROM anchoring,
  Vulkan surface invalidation fix for Silent Scope 2.
