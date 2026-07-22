/*
 * flowa's xemu fork - custom INI configuration
 *
 * Loads (or generates on first run) flowas_config.ini next to the
 * executable and applies its values over the loaded settings. This gives
 * users a simple, commented file to tweak the lightgun experience without
 * digging through xemu.toml.
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

#ifndef FLOWA_CONFIG_H
#define FLOWA_CONFIG_H

#include <stdbool.h>

#ifdef __cplusplus
extern "C" {
#endif

// Load flowas_config.ini (generating it with defaults if missing) and
// apply its values to g_config. Call once at startup, after the regular
// settings have been loaded.
void flowa_config_load(void);

// True when flowas_config.ini did not exist and was just generated
// (i.e. the very first launch of this build).
bool flowa_config_is_first_run(void);

// The welcome popup shows at every startup until the user ticks
// "Don't show again", which writes EULA.txt next to the executable
// (same folder as flowa_config.ini, never AppData). Deleting that
// file brings the popup back.
bool flowa_should_show_welcome(void);
void flowa_welcome_dismiss(bool dont_show_again);

#ifdef __cplusplus
}
#endif

#endif
