/*
 * flowa's xemu fork - custom INI configuration
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

#include "qemu/osdep.h"

#include <SDL3/SDL.h>

#include "flowa-config.h"
#include "xemu-input.h"
#include "xemu-settings.h"

static bool flowa_first_run;

static int match_enum(const char *val, const char *const *names, int count)
{
    for (int i = 0; i < count; i++) {
        if (strcmp(val, names[i]) == 0) {
            return i;
        }
    }
    return -1;
}

static const char *default_ini =
"; =================================================================\n"
";  flowa's xemu fork - custom settings\n"
";  Support the channel: https://www.youtube.com/@flowachannel4731\n"
";\n"
";  These values are applied at EVERY startup and override the\n"
";  corresponding settings stored in xemu.toml.\n"
";  Only change what you understand: wrong values can ruin the\n"
";  lightgun experience. Delete this file to restore the defaults\n"
";  (it will be regenerated on the next launch).\n"
";\n"
";  MACHINE FILES: drop your files into the folders next to xemu.exe\n"
";  and they are picked up automatically at startup:\n"
";    bios\\         - flash BIOS image (e.g. Complex_4627.bin)\n"
";    mcpxbootrom\\  - MCPX boot ROM (e.g. mcpx_1.0.bin)\n"
";    harddisk\\     - Xbox HDD image (e.g. xbox_hdd.qcow2)\n"
";    eeprom\\       - EEPROM image (migrated/generated here on first\n"
";                    start if the folder is empty)\n"
";  Remember: EU BIOS -> EU games, US BIOS -> US games!\n"
"; =================================================================\n"
"\n"
"[ui]\n"
"; The mouse cursor and the top menu bar are VISIBLE by default so you\n"
"; can set everything up (controllers, BIOS, game disc...).\n"
"; Once you are done, set hide_cursor = 1 and show_menu_bar = 0 for\n"
"; the clean lightgun experience.\n"
"\n"
"; 1 = never show the mouse cursor over the game (recommended for\n"
";     lightguns once setup is complete; it still appears while a\n"
";     xemu menu is open)\n"
"; 0 = cursor always visible\n"
"hide_cursor = 0\n"
"\n"
"; 1 = show the top menu bar (View/Machine/Help...) on mouse move\n"
"; 0 = keep the top menu bar always hidden (open menus with F1/F2)\n"
"show_menu_bar = 1\n"
"\n"
"; 1 = start xemu in fullscreen\n"
"; 0 = start windowed\n"
"fullscreen = 0\n"
"\n"
"; 1 = skip the Xbox boot animation (like a modded console)\n"
"; 0 = show the full boot intro\n"
"skip_boot_anim = 0\n"
"\n"
"[graphics]\n"
"; xemu graphics settings. An EMPTY value = keep whatever is saved in\n"
"; xemu itself. Use the Code Flow GunSetup configurator to change them.\n"
"; renderer: null, opengl or vulkan\n"
"renderer =\n"
"; resolution_scale: 1 to 10 (internal resolution multiplier)\n"
"resolution_scale =\n"
"; window_size: last_used, 640x480, 720x480, 1280x720, 1280x800,\n"
";   1280x960, 1920x1080, 2560x1440, 2560x1600, 2560x1920, 3840x2160\n"
"window_size =\n"
"; fullscreen_exclusive: 1 or 0\n"
"fullscreen_exclusive =\n"
"; vsync: 1 or 0\n"
"vsync =\n"
"; show_notifications: 1 or 0\n"
"show_notifications =\n"
"; ui_scale: auto, 1 or 2\n"
"ui_scale =\n"
"; animations: 1 or 0\n"
"animations =\n"
"; display_mode: center, scale or stretch\n"
"display_mode =\n"
"; aspect_ratio: native, auto, 4x3 or 16x9\n"
"aspect_ratio =\n"
"; filtering: linear or nearest\n"
"filtering =\n"
"\n"
"[lightgun]\n"
"; DO NOT TOUCH these unless you know what you are doing.\n"
"\n"
"; Aim smoothing (adaptive filter): steadies the crosshair while the\n"
"; gun is nearly still, without delaying fast moves.\n"
"; 0.0 = filter OFF (fully raw, jittery), 1.0 = maximum stability.\n"
"; Default: 0.2\n"
"aim_smoothing = 0.2\n"
"\n"
"; Aim sensitivity: 1.0 = exact 1:1 aim. Valid range 0.5 - 1.5.\n"
"; Default: 1.0\n"
"aim_sensitivity = 1.0\n"
"\n"
"; 1 = double left-click does NOT toggle fullscreen (keep at 1 for\n"
";     lightguns: the trigger IS a left click)\n"
"disable_dblclick_fullscreen = 1\n"
"\n"
"; 1 = right-click does NOT open the xemu menu (keep at 1 for\n"
";     lightguns: the secondary button IS a right click)\n"
"disable_rclick_menu = 1\n"
"\n"
"[game]\n"
"; Full path to the game ISO/XISO to boot. Leave empty to keep the\n"
"; disc already configured in xemu.\n"
"; Remember: the game region must match the BIOS region (EU/US)!\n"
"iso_path =\n"
"\n"
"[players]\n"
"; Bind a specific pointer device to each controller port. Use the\n"
"; Code Flow GunSetup configurator to fill these automatically, or leave\n"
"; empty to keep the bindings saved from the xemu Input menu.\n"
"; portN_device: device id, e.g. mouse:1a2b3c4d (or: keyboard)\n"
"; portN_driver: lightgun, duke or controller-s\n"
"port1_device =\n"
"port1_driver =\n"
"port2_device =\n"
"port2_driver =\n"
"port3_device =\n"
"port3_driver =\n"
"port4_device =\n"
"port4_driver =\n"
"\n"
"[mapping]\n"
"; Custom bindings for the lightgun port. EMPTY = default mapping:\n"
";   mouse LEFT = trigger (A), RIGHT = B, MIDDLE = Start,\n"
";   X1/side = Back, X2/side = X. Aim always follows the gun/mouse\n"
";   selected in [players] - these only remap BUTTONS.\n"
"; Use the Code Flow GunSetup configurator (Mapping tab) to fill these.\n"
"; Formats: mouse:<id>:<left|right|middle|x1|x2>  (any detected mouse)\n"
";          key:<name>  (keyboard key, e.g. key:Up, key:Return, key:A)\n"
"map_trigger =\n"
"map_b =\n"
"map_x =\n"
"map_start =\n"
"map_back =\n"
"map_dpad_up =\n"
"map_dpad_down =\n"
"map_dpad_left =\n"
"map_dpad_right =\n"
"; Additional pad buttons (service/extra modes in some games).\n"
"; ltrig/rtrig are the analog triggers: a mapped press = full pull.\n"
"map_y =\n"
"map_white =\n"
"map_black =\n"
"map_lstick =\n"
"map_rstick =\n"
"map_ltrig =\n"
"map_rtrig =\n"
"map_guide =\n";

static char *get_ini_path(void)
{
    const char *base = SDL_GetBasePath();
    return g_strdup_printf("%sflowa_config.ini", base ? base : "");
}

// Pick the first usable file from a machine-files folder next to the
// executable (bios\, mcpxbootrom\, harddisk\, eeprom\) and apply it to
// the given config path. Creates the folder if missing so users can
// simply drop their files in.
static void scan_machine_dir(const char *dir_name, const char **cfg)
{
    const char *base = SDL_GetBasePath();
    char *dpath = g_strdup_printf("%s%s", base ? base : "", dir_name);
    GDir *dir = g_dir_open(dpath, 0, NULL);
    if (dir == NULL) {
        g_mkdir_with_parents(dpath, 0755);
        g_free(dpath);
        return;
    }
    const char *name;
    while ((name = g_dir_read_name(dir)) != NULL) {
        if (g_str_has_suffix(name, ".txt")) {
            continue; // allow readme files in the folder
        }
        char *fpath =
            g_strdup_printf("%s%s%s", dpath, G_DIR_SEPARATOR_S, name);
        if (g_file_test(fpath, G_FILE_TEST_IS_REGULAR)) {
            xemu_settings_set_string(cfg, fpath);
            g_free(fpath);
            break;
        }
        g_free(fpath);
    }
    g_dir_close(dir);
    g_free(dpath);
}

// Keep the EEPROM inside the eeprom\ folder so the package is fully
// portable. If the folder is empty, migrate the currently configured
// EEPROM into it (preserving the console identity: region, serial...);
// if none exists anywhere, point the config into the folder and xemu
// will auto-generate the file there (see get_eeprom_path in vl.c).
static void flowa_anchor_eeprom(void)
{
    const char *base = SDL_GetBasePath();
    if (base == NULL) {
        return;
    }
    char *folder = g_strdup_printf("%seeprom", base);
    if (strncmp(g_config.sys.files.eeprom_path, folder, strlen(folder)) ==
        0) {
        // scan_machine_dir already found a file in the folder
        g_free(folder);
        return;
    }
    char *target = g_strdup_printf("%s%seeprom.bin", folder,
                                   G_DIR_SEPARATOR_S);

    const char *cur = g_config.sys.files.eeprom_path;
    if (cur != NULL && *cur &&
        g_file_test(cur, G_FILE_TEST_IS_REGULAR)) {
        gchar *data = NULL;
        gsize len = 0;
        if (g_file_get_contents(cur, &data, &len, NULL) && len == 256) {
            g_file_set_contents(target, data, len, NULL);
        }
        g_free(data);
    }

    xemu_settings_set_string(&g_config.sys.files.eeprom_path, target);
    g_free(folder);
    g_free(target);
}

static void flowa_scan_machine_dirs(void)
{
    scan_machine_dir("bios", &g_config.sys.files.flashrom_path);
    scan_machine_dir("mcpxbootrom", &g_config.sys.files.bootrom_path);
    scan_machine_dir("harddisk", &g_config.sys.files.hdd_path);
    scan_machine_dir("eeprom", &g_config.sys.files.eeprom_path);
    flowa_anchor_eeprom();
}

void flowa_config_load(void)
{
#ifndef _WIN32
    // The portable INI (and the machine-dir autodetect / EEPROM anchoring
    // that comes with it) is a feature of the Windows package. On Linux a
    // frontend launcher (e.g. Batocera's configgen) owns xemu.toml and
    // must not be overridden at boot.
    return;
#endif
    char *path = get_ini_path();
    FILE *f = fopen(path, "r");

    if (f == NULL) {
        // First launch: generate the ini with default values
        FILE *out = fopen(path, "w");
        if (out) {
            fputs(default_ini, out);
            fclose(out);
            flowa_first_run = true;
        }
        f = fopen(path, "r");
    }
    if (f == NULL) {
        fprintf(stderr, "flowa-config: could not open %s\n", path);
        g_free(path);
        return;
    }

    char line[256];
    while (fgets(line, sizeof(line), f)) {
        char *key = line;
        while (*key == ' ' || *key == '\t') {
            key++;
        }
        if (*key == ';' || *key == '#' || *key == '[' || *key == '\r' ||
            *key == '\n' || *key == '\0') {
            continue;
        }
        char *eq = strchr(key, '=');
        if (eq == NULL) {
            continue;
        }
        char *key_end = eq;
        *eq = '\0';
        while (key_end > key &&
               (key_end[-1] == ' ' || key_end[-1] == '\t')) {
            *--key_end = '\0';
        }

        // Trimmed value, both as string and as number
        char *val = eq + 1;
        while (*val == ' ' || *val == '\t') {
            val++;
        }
        char *val_end = val + strlen(val);
        while (val_end > val &&
               (val_end[-1] == '\n' || val_end[-1] == '\r' ||
                val_end[-1] == ' ' || val_end[-1] == '\t' ||
                val_end[-1] == '"')) {
            *--val_end = '\0';
        }
        if (*val == '"') {
            val++;
        }
        float value = strtof(val, NULL);

        if (strcmp(key, "hide_cursor") == 0) {
            g_config.display.ui.hide_cursor = value != 0;
        } else if (strcmp(key, "show_menu_bar") == 0) {
            g_config.display.ui.show_menubar = value != 0;
        } else if (strcmp(key, "fullscreen") == 0) {
            g_config.display.window.fullscreen_on_startup = value != 0;
        } else if (strcmp(key, "skip_boot_anim") == 0) {
            g_config.general.skip_boot_anim = value != 0;
        } else if (strcmp(key, "aim_smoothing") == 0) {
            g_config.input.lightgun_smoothing = MIN(MAX(value, 0.0f), 1.0f);
        } else if (strcmp(key, "aim_sensitivity") == 0) {
            g_config.input.lightgun_sensitivity =
                MIN(MAX(value, 0.5f), 1.5f);
        } else if (strcmp(key, "disable_dblclick_fullscreen") == 0) {
            g_config.input.disable_dblclick_fullscreen = value != 0;
        } else if (strcmp(key, "disable_rclick_menu") == 0) {
            g_config.input.disable_rclick_menu = value != 0;
        } else if (strcmp(key, "renderer") == 0 && *val) {
            static const char *const renderers[] = { "null", "opengl",
                                                     "vulkan" };
            int idx = match_enum(val, renderers, 3);
            if (idx >= 0) {
                g_config.display.renderer = idx;
            }
        } else if (strcmp(key, "resolution_scale") == 0 && *val) {
            int s = (int)value;
            if (s >= 1 && s <= 10) {
                g_config.display.quality.surface_scale = s;
            }
        } else if (strcmp(key, "window_size") == 0 && *val) {
            static const char *const sizes[] = {
                "last_used", "640x480",   "720x480",   "1280x720",
                "1280x800",  "1280x960",  "1920x1080", "2560x1440",
                "2560x1600", "2560x1920", "3840x2160"
            };
            int idx = match_enum(val, sizes, 11);
            if (idx >= 0) {
                g_config.display.window.startup_size = idx;
            }
        } else if (strcmp(key, "fullscreen_exclusive") == 0 && *val) {
            g_config.display.window.fullscreen_exclusive = value != 0;
        } else if (strcmp(key, "vsync") == 0 && *val) {
            g_config.display.window.vsync = value != 0;
        } else if (strcmp(key, "show_notifications") == 0 && *val) {
            g_config.display.ui.show_notifications = value != 0;
        } else if (strcmp(key, "ui_scale") == 0 && *val) {
            if (strcmp(val, "auto") == 0) {
                g_config.display.ui.auto_scale = true;
            } else if ((int)value == 1 || (int)value == 2) {
                g_config.display.ui.auto_scale = false;
                g_config.display.ui.scale = (int)value;
            }
        } else if (strcmp(key, "animations") == 0 && *val) {
            g_config.display.ui.use_animations = value != 0;
        } else if (strcmp(key, "display_mode") == 0 && *val) {
            static const char *const fits[] = { "center", "scale",
                                                "stretch" };
            int idx = match_enum(val, fits, 3);
            if (idx >= 0) {
                g_config.display.ui.fit = idx;
            }
        } else if (strcmp(key, "aspect_ratio") == 0 && *val) {
            static const char *const ratios[] = { "native", "auto", "4x3",
                                                  "16x9" };
            int idx = match_enum(val, ratios, 4);
            if (idx >= 0) {
                g_config.display.ui.aspect_ratio = idx;
            }
        } else if (strcmp(key, "filtering") == 0 && *val) {
            static const char *const filters[] = { "linear", "nearest" };
            int idx = match_enum(val, filters, 2);
            if (idx >= 0) {
                g_config.display.filtering = idx;
            }
        } else if (strcmp(key, "iso_path") == 0) {
            if (*val) {
                /* Relative paths are resolved against the folder holding
                 * xemu.exe so the whole package stays portable. */
                if (!g_path_is_absolute(val)) {
                    const char *base = SDL_GetBasePath();
                    char *abs =
                        g_strdup_printf("%s%s", base ? base : "", val);
                    xemu_settings_set_string(&g_config.sys.files.dvd_path,
                                             abs);
                    g_free(abs);
                } else {
                    xemu_settings_set_string(&g_config.sys.files.dvd_path,
                                             val);
                }
            }
        } else if (strncmp(key, "map_", 4) == 0) {
            static const struct {
                const char *name;
                const char **cfg;
            } map_keys[] = {
                { "trigger", &g_config.input.lightgun_mapping.trigger },
                { "b", &g_config.input.lightgun_mapping.b },
                { "x", &g_config.input.lightgun_mapping.x },
                { "start", &g_config.input.lightgun_mapping.start },
                { "back", &g_config.input.lightgun_mapping.back },
                { "dpad_up", &g_config.input.lightgun_mapping.dpad_up },
                { "dpad_down", &g_config.input.lightgun_mapping.dpad_down },
                { "dpad_left", &g_config.input.lightgun_mapping.dpad_left },
                { "dpad_right", &g_config.input.lightgun_mapping.dpad_right },
                { "y", &g_config.input.lightgun_mapping.y },
                { "white", &g_config.input.lightgun_mapping.white },
                { "black", &g_config.input.lightgun_mapping.black },
                { "lstick", &g_config.input.lightgun_mapping.lstick },
                { "rstick", &g_config.input.lightgun_mapping.rstick },
                { "ltrig", &g_config.input.lightgun_mapping.ltrig },
                { "rtrig", &g_config.input.lightgun_mapping.rtrig },
                { "guide", &g_config.input.lightgun_mapping.guide },
            };
            for (int i = 0; i < ARRAY_SIZE(map_keys); i++) {
                if (strcmp(key + 4, map_keys[i].name) == 0) {
                    xemu_settings_set_string(map_keys[i].cfg, val);
                    break;
                }
            }
        } else if (strncmp(key, "port", 4) == 0 && key[4] >= '1' &&
                   key[4] <= '4') {
            static const char **device_keys[4] = {
                &g_config.input.bindings.port1,
                &g_config.input.bindings.port2,
                &g_config.input.bindings.port3,
                &g_config.input.bindings.port4,
            };
            static const char **driver_keys[4] = {
                &g_config.input.bindings.port1_driver,
                &g_config.input.bindings.port2_driver,
                &g_config.input.bindings.port3_driver,
                &g_config.input.bindings.port4_driver,
            };
            int port = key[4] - '1';
            if (strcmp(key + 5, "_device") == 0 && *val) {
                xemu_settings_set_string(device_keys[port], val);
            } else if (strcmp(key + 5, "_driver") == 0 && *val) {
                const char *drv = NULL;
                if (strcmp(val, "lightgun") == 0) {
                    drv = DRIVER_LIGHTGUN;
                } else if (strcmp(val, "duke") == 0) {
                    drv = DRIVER_DUKE;
                } else if (strcmp(val, "controller-s") == 0) {
                    drv = DRIVER_S;
                }
                if (drv) {
                    xemu_settings_set_string(driver_keys[port], drv);
                }
            }
        }
    }

    fclose(f);
    g_free(path);

    flowa_scan_machine_dirs();
}

bool flowa_config_is_first_run(void)
{
    return flowa_first_run;
}

static char *get_eula_path(void)
{
    return g_strdup_printf("%sEULA.txt", xemu_settings_get_base_path());
}

bool flowa_should_show_welcome(void)
{
#ifndef _WIN32
    // No welcome popup over frontend-launched games (Batocera & co.)
    return false;
#endif
    char *path = get_eula_path();
    bool exists = g_file_test(path, G_FILE_TEST_EXISTS);
    g_free(path);
    return !exists;
}

void flowa_welcome_dismiss(bool dont_show_again)
{
    if (!dont_show_again) {
        return;
    }
    char *path = get_eula_path();
    FILE *f = fopen(path, "w");
    if (f) {
        fputs("flowa's xemu: welcome popup dismissed.\n"
              "Delete this file to see the welcome message again.\n",
              f);
        fclose(f);
    } else {
        fprintf(stderr, "flowa-config: could not write %s\n", path);
    }
    g_free(path);
}
