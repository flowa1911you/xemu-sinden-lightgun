//
// Code Flow GunSetup v4.0 - xemu LightGun Edition Configurator
//
// Made by Code Flow - https://github.com/flowa1911you
//
// Single-file WinForms app, compiled with the C# compiler bundled with
// the .NET Framework (no extra installs needed):
//   %WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
//
// Reads and writes flowa_config.ini next to xemu.exe. xemu applies the
// INI values at every startup.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("Code Flow GunSetup")]
[assembly: System.Reflection.AssemblyProduct("Code Flow GunSetup")]
[assembly: System.Reflection.AssemblyDescription(
    "xemu LightGun Edition Configurator")]
[assembly: System.Reflection.AssemblyVersion("4.0.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("4.0.0.0")]

namespace FlowaGunSetup
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    class MouseDevice
    {
        public string Path;
        public string Name;
        public string Id; // "mouse:xxxxxxxx", same hash xemu computes

        public override string ToString()
        {
            return Name;
        }
    }

    static class RawInputDevices
    {
        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        const uint RIM_TYPEMOUSE = 0;
        const uint RIDI_DEVICENAME = 0x20000007;

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetRawInputDeviceList(
            [In, Out] RAWINPUTDEVICELIST[] list, ref uint numDevices,
            uint size);

        [DllImport("user32.dll", CharSet = CharSet.Unicode,
                   SetLastError = true)]
        static extern uint GetRawInputDeviceInfoW(IntPtr hDevice,
            uint command, StringBuilder data, ref uint size);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode,
                   SetLastError = true)]
        static extern IntPtr CreateFileW(string fileName, uint access,
            uint share, IntPtr securityAttributes, uint disposition,
            uint flags, IntPtr templateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr handle);

        [DllImport("hid.dll", CharSet = CharSet.Unicode)]
        static extern bool HidD_GetProductString(IntPtr device,
            StringBuilder buffer, uint bufferLength);

        [StructLayout(LayoutKind.Sequential)]
        struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE[] devices, uint numDevices, uint size);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetRawInputData(IntPtr hRawInput, uint command,
            byte[] data, ref uint size, uint headerSize);

        // Receive WM_INPUT for mice on the given window (used by the
        // Mapping tab to capture a button press with device identity)
        public static bool RegisterForMouseInput(IntPtr hwnd)
        {
            RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x02; // generic mouse
            rid[0].dwFlags = 0;    // deliver while we have focus
            rid[0].hwndTarget = hwnd;
            return RegisterRawInputDevices(rid, 1,
                (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE)));
        }

        // Parse a WM_INPUT message; returns true when a mouse button DOWN
        // was seen, with the xemu-compatible spec ("mouse:<id>:<button>"),
        // a friendly button name and the source device path.
        public static bool TryReadMouseButtonDown(IntPtr lParam,
            out string spec, out string buttonName, out string devicePath)
        {
            spec = null;
            buttonName = null;
            devicePath = null;

            uint size = 0;
            const uint RID_INPUT = 0x10000003;
            uint headerSize = (uint)(IntPtr.Size == 8 ? 24 : 16);
            GetRawInputData(lParam, RID_INPUT, null, ref size, headerSize);
            if (size == 0)
                return false;
            byte[] buf = new byte[size];
            if (GetRawInputData(lParam, RID_INPUT, buf, ref size,
                                headerSize) != size)
                return false;

            uint dwType = BitConverter.ToUInt32(buf, 0);
            if (dwType != RIM_TYPEMOUSE)
                return false;

            IntPtr hDevice = IntPtr.Size == 8
                ? (IntPtr)BitConverter.ToInt64(buf, 8)
                : (IntPtr)BitConverter.ToInt32(buf, 8);
            // RAWMOUSE.usButtonFlags: header + usFlags(2) + pad(2)
            int flagsOffset = (int)headerSize + 4;
            if (buf.Length < flagsOffset + 2)
                return false;
            ushort buttonFlags = BitConverter.ToUInt16(buf, flagsOffset);

            string btn = null;
            if ((buttonFlags & 0x0001) != 0) btn = "left";
            else if ((buttonFlags & 0x0004) != 0) btn = "right";
            else if ((buttonFlags & 0x0010) != 0) btn = "middle";
            else if ((buttonFlags & 0x0040) != 0) btn = "x1";
            else if ((buttonFlags & 0x0100) != 0) btn = "x2";
            if (btn == null)
                return false;

            string path = GetDevicePath(hDevice);
            if (path == null)
                return false;

            spec = DeviceIdForPath(path) + ":" + btn;
            buttonName = btn.ToUpperInvariant();
            devicePath = path;
            return true;
        }

        public static string FriendlyNameForPath(string path)
        {
            string name = GetProductName(path);
            if (name == null || name.Length == 0)
                name = "HID Mouse";
            return name;
        }

        // Must match the FNV-1a hash in xemu's ui/xemu-rawinput.c,
        // computed over the UTF-8 bytes of the device path
        public static string DeviceIdForPath(string path)
        {
            uint h = 0x811c9dc5;
            byte[] bytes = Encoding.UTF8.GetBytes(path);
            for (int i = 0; i < bytes.Length; i++)
            {
                h ^= bytes[i];
                unchecked { h *= 0x01000193; }
            }
            return "mouse:" + h.ToString("x8");
        }

        static string GetDevicePath(IntPtr hDevice)
        {
            uint size = 0;
            GetRawInputDeviceInfoW(hDevice, RIDI_DEVICENAME, null, ref size);
            if (size == 0)
                return null;
            StringBuilder sb = new StringBuilder((int)size + 1);
            if (GetRawInputDeviceInfoW(hDevice, RIDI_DEVICENAME, sb,
                                       ref size) == unchecked((uint)-1))
                return null;
            return sb.ToString();
        }

        static string GetProductName(string path)
        {
            const uint GENERIC_NONE = 0;
            const uint FILE_SHARE_READ = 1, FILE_SHARE_WRITE = 2;
            const uint OPEN_EXISTING = 3;
            IntPtr h = CreateFileW(path, GENERIC_NONE,
                FILE_SHARE_READ | FILE_SHARE_WRITE, IntPtr.Zero,
                OPEN_EXISTING, 0, IntPtr.Zero);
            if (h == new IntPtr(-1))
                return null;
            try
            {
                StringBuilder sb = new StringBuilder(128);
                if (HidD_GetProductString(h, sb, 254) && sb.Length > 0)
                    return sb.ToString();
                return null;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        public static List<MouseDevice> Enumerate()
        {
            List<MouseDevice> result = new List<MouseDevice>();
            uint num = 0;
            uint structSize = (uint)Marshal.SizeOf(
                typeof(RAWINPUTDEVICELIST));
            GetRawInputDeviceList(null, ref num, structSize);
            if (num == 0)
                return result;
            RAWINPUTDEVICELIST[] list = new RAWINPUTDEVICELIST[num];
            uint n = GetRawInputDeviceList(list, ref num, structSize);
            if (n == unchecked((uint)-1))
                return result;

            for (int i = 0; i < n; i++)
            {
                if (list[i].dwType != RIM_TYPEMOUSE)
                    continue;
                string path = GetDevicePath(list[i].hDevice);
                if (path == null || path.IndexOf("RDP_MOU",
                        StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                string name = GetProductName(path);
                if (string.IsNullOrEmpty(name))
                    name = "HID Mouse";

                // Disambiguate identical product names, like xemu does
                int sameName = 0;
                foreach (MouseDevice d in result)
                    if (d.Name.StartsWith(name))
                        sameName++;
                if (sameName > 0)
                    name = name + " #" + (sameName + 1);

                MouseDevice dev = new MouseDevice();
                dev.Path = path;
                dev.Name = name;
                dev.Id = DeviceIdForPath(path);
                result.Add(dev);
            }
            return result;
        }
    }

    class MainForm : Form
    {
        // Palette
        static readonly Color BgColor = Color.FromArgb(24, 24, 27);
        static readonly Color PanelColor = Color.FromArgb(34, 34, 38);
        static readonly Color TextColor = Color.FromArgb(230, 230, 230);
        static readonly Color DimColor = Color.FromArgb(150, 150, 155);
        static readonly Color AccentColor = Color.FromArgb(129, 220, 138);
        static readonly Color WarnColor = Color.FromArgb(255, 170, 60);
        static readonly Color BadColor = Color.FromArgb(240, 90, 90);

        string baseDir;
        string iniPath;

        // Controls
        TextBox isoBox;
        Label[] machineStatus = new Label[4];
        CheckBox hideCursorCk, showMenuBarCk, fullscreenCk, skipIntroCk;
        CheckBox dblClickCk, rClickCk, unlockGunCk;
        TrackBar smoothBar, sensBar;
        Label smoothVal, sensVal;
        bool unlockGuard;
        ComboBox[] deviceCombo = new ComboBox[4];
        ComboBox[] driverCombo = new ComboBox[4];
        Label[] playerLabels = new Label[4];
        Label p1Hint;
        List<MouseDevice> mice = new List<MouseDevice>();

        // Graphics tab: mirrors xemu's Display settings. Index 0 of every
        // combo is "(keep current)" which writes an empty INI value.
        string[] gfxKeys;
        string[][] gfxValues;
        ComboBox[] gfxCombo;

        // Mapping tab: bindable gun BUTTONS ([mapping] in the INI). The
        // aim itself is NOT mappable by design: it always follows the raw
        // input device selected in Players.
        static readonly string[] MapKeys = {
            "map_trigger", "map_b", "map_x", "map_start", "map_back",
            "map_dpad_up", "map_dpad_down", "map_dpad_left",
            "map_dpad_right",
            "map_y", "map_white", "map_black", "map_lstick", "map_rstick",
            "map_ltrig", "map_rtrig", "map_guide"
        };
        static readonly string[] MapLabels = {
            "Trigger  (A)", "B  (grip / reload)", "X", "Start", "Back",
            "D-Pad Up", "D-Pad Down", "D-Pad Left", "D-Pad Right",
            "Y", "White", "Black", "L-Stick Click", "R-Stick Click",
            "Left Trigger", "Right Trigger", "Guide (xemu menu)"
        };
        static readonly string[] MapDefaults = {
            "mouse LEFT", "mouse RIGHT", "mouse X2 (side)",
            "mouse MIDDLE", "mouse X1 (side)", "", "", "", "",
            "", "", "", "", "", "", "", ""
        };
        const int MapAdditionalFrom = 9; // index of the first extra slot
        string[] mapValues = new string[17];
        string[] mapLoadedValues = new string[17]; // as loaded at startup
        Button[] mapButtons = new Button[17];
        int mapCapturing = -1;
        int mapCountdown;
        Timer mapTimer;
        DateTime mapLastCaptureEnd = DateTime.MinValue;

        static readonly string[] MachineDirs =
            { "bios", "mcpxbootrom", "harddisk", "eeprom" };
        static readonly string[] MachineLabels = {
            "BIOS (flash)", "MCPX boot ROM", "Hard disk image",
            "EEPROM (optional)" };
        static readonly string[] DriverIniValues =
            { "", "lightgun", "duke", "controller-s" };
        static readonly string[] DriverDisplay = {
            "(keep current)", "Lightgun (EMS TopGun II)",
            "Xbox Controller (Duke)", "Xbox Controller S" };

        public MainForm()
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
            iniPath = Path.Combine(baseDir, "flowa_config.ini");

            Text = "Code Flow GunSetup v4.0";
            KeyPreview = true; // capture keys for the Mapping tab
            BackColor = BgColor;
            ForeColor = TextColor;
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(780, 764);
            StartPosition = FormStartPosition.CenterScreen;
            try
            {
                // Use the icon embedded in the exe (flowa's Sinden icon)
                Icon = Icon.ExtractAssociatedIcon(
                    Application.ExecutablePath);
            }
            catch (Exception) { }

            BuildUi();
            EnsureMachineDirs();
            RefreshDevices();
            LoadIni();
            RefreshMachineStatus();

            // Receive WM_INPUT so the Mapping tab can capture mouse
            // buttons together with the device they came from
            RawInputDevices.RegisterForMouseInput(this.Handle);

            // Live-refresh the machine files status so the labels react
            // as soon as files are dropped into the folders
            Timer machineTimer = new Timer();
            machineTimer.Interval = 2000;
            machineTimer.Tick += new EventHandler(OnMachineTimer);
            machineTimer.Start();
        }

        void OnMachineTimer(object sender, EventArgs e)
        {
            RefreshMachineStatus();
        }

        // ------------------------------------------------------------ UI

        Label MakeLabel(string text, int x, int y, Color color)
        {
            Label l = new Label();
            l.Text = text;
            l.AutoSize = true;
            l.Location = new Point(x, y);
            l.ForeColor = color;
            Controls.Add(l);
            return l;
        }

        GroupBox MakeGroup(Control parent, string title, int x, int y,
                           int w, int h)
        {
            GroupBox g = new GroupBox();
            g.Text = title;
            g.Bounds = new Rectangle(x, y, w, h);
            g.ForeColor = AccentColor;
            g.BackColor = PanelColor;
            parent.Controls.Add(g);
            return g;
        }

        CheckBox MakeCheck(Control parent, string text, int x, int y)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.AutoSize = true;
            c.Location = new Point(x, y);
            c.ForeColor = TextColor;
            parent.Controls.Add(c);
            return c;
        }

        Button MakeButton(Control parent, string text, int x, int y,
                          int w, int h)
        {
            Button b = new Button();
            b.Text = text;
            b.Bounds = new Rectangle(x, y, w, h);
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Color.FromArgb(70, 70, 75);
            b.BackColor = Color.FromArgb(45, 45, 50);
            b.ForeColor = TextColor;
            parent.Controls.Add(b);
            return b;
        }

        void BuildUi()
        {
            // Header
            Label title = MakeLabel("CODE FLOW  GUNSETUP  v4.0", 20, 14,
                                    AccentColor);
            title.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
            Label sub = MakeLabel(
                "xemu LightGun Edition Configurator",
                24, 46, DimColor);
            sub.Font = new Font("Segoe UI", 9f, FontStyle.Italic);

            // ---- Tabs
            TabControl tabs = new TabControl();
            tabs.Bounds = new Rectangle(10, 72, 760, 610);
            Controls.Add(tabs);
            TabPage setupPage = new TabPage("Setup");
            setupPage.BackColor = BgColor;
            tabs.TabPages.Add(setupPage);
            TabPage gfxPage = new TabPage("Graphics");
            gfxPage.BackColor = BgColor;
            tabs.TabPages.Add(gfxPage);
            BuildGraphicsTab(gfxPage);
            TabPage mapPage = new TabPage("Mapping");
            mapPage.BackColor = BgColor;
            tabs.TabPages.Add(mapPage);
            BuildMappingTab(mapPage);

            // ---- Game group
            GroupBox game = MakeGroup(setupPage, "Game", 8, 6, 748, 84);
            isoBox = new TextBox();
            isoBox.Bounds = new Rectangle(14, 26, 610, 24);
            isoBox.ReadOnly = true;
            isoBox.BackColor = Color.FromArgb(28, 28, 32);
            isoBox.ForeColor = TextColor;
            game.Controls.Add(isoBox);
            Button browse = MakeButton(game, "Browse...", 634, 24, 100, 26);
            browse.Click += new EventHandler(OnBrowseIso);
            Label regionWarn = new Label();
            regionWarn.Text = "Remember: the game region must match the "
                + "BIOS region (EU BIOS -> EU games, US BIOS -> US games).\n"
                + "If xemu boots to \"Please insert an Xbox disc\": the "
                + "regions don't match, or the ISO format is not valid.";
            regionWarn.AutoSize = true;
            regionWarn.Location = new Point(14, 50);
            regionWarn.ForeColor = WarnColor;
            game.Controls.Add(regionWarn);

            // ---- Machine files group
            GroupBox mach = MakeGroup(setupPage,
                "Machine files (auto-detected from folders next to "
                + "xemu.exe)", 8, 98, 748, 128);
            for (int i = 0; i < 4; i++)
            {
                Label name = new Label();
                name.Text = MachineLabels[i] + "  -  " + MachineDirs[i]
                    + "\\";
                name.AutoSize = true;
                name.Location = new Point(14, 26 + i * 24);
                name.ForeColor = TextColor;
                mach.Controls.Add(name);

                Label status = new Label();
                status.AutoSize = true;
                status.Location = new Point(280, 26 + i * 24);
                status.ForeColor = BadColor;
                status.Text = "missing";
                mach.Controls.Add(status);
                machineStatus[i] = status;
            }

            // ---- Display group
            GroupBox disp = MakeGroup(setupPage, "Display", 8, 234, 364,
                                      132);
            hideCursorCk = MakeCheck(disp,
                "Hide mouse cursor (set after finishing setup)", 14, 26);
            showMenuBarCk = MakeCheck(disp,
                "Show top menu bar (View/Machine/Help...)", 14, 52);
            fullscreenCk = MakeCheck(disp, "Start in fullscreen", 14, 78);
            skipIntroCk = MakeCheck(disp, "Skip Xbox boot intro animation",
                                    14, 104);

            // ---- Lightgun group
            GroupBox gun = MakeGroup(setupPage, "Lightgun (don't touch!)",
                                     392, 234, 364, 132);
            MakeGunSliders(gun);

            // ---- Clicks group
            GroupBox clicks = MakeGroup(setupPage, "Click protection", 8,
                                        374, 364, 90);
            dblClickCk = MakeCheck(clicks,
                "Disable double-click fullscreen toggle", 14, 26);
            rClickCk = MakeCheck(clicks, "Disable right-click menu", 14,
                                 52);

            // ---- Players group
            GroupBox players = MakeGroup(setupPage, "Players", 392, 374,
                                         364, 176);
            for (int i = 0; i < 4; i++)
            {
                Label pl = new Label();
                pl.Text = "P" + (i + 1);
                pl.AutoSize = true;
                pl.Location = new Point(14, 29 + i * 30);
                pl.ForeColor = TextColor;
                players.Controls.Add(pl);
                playerLabels[i] = pl;

                ComboBox dev = new ComboBox();
                dev.DropDownStyle = ComboBoxStyle.DropDownList;
                dev.Bounds = new Rectangle(44, 25 + i * 30, 160, 24);
                dev.BackColor = Color.FromArgb(28, 28, 32);
                dev.ForeColor = TextColor;
                dev.FlatStyle = FlatStyle.Flat;
                dev.SelectedIndexChanged +=
                    new EventHandler(OnPortSelectionChanged);
                players.Controls.Add(dev);
                deviceCombo[i] = dev;

                ComboBox drv = new ComboBox();
                drv.DropDownStyle = ComboBoxStyle.DropDownList;
                drv.Bounds = new Rectangle(212, 25 + i * 30, 140, 24);
                drv.BackColor = Color.FromArgb(28, 28, 32);
                drv.ForeColor = TextColor;
                drv.FlatStyle = FlatStyle.Flat;
                for (int j = 0; j < DriverDisplay.Length; j++)
                    drv.Items.Add(DriverDisplay[j]);
                // Player 1 defaults to the lightgun; a lightgun setup
                // makes no sense without it
                drv.SelectedIndex = i == 0 ? 1 : 0;
                drv.SelectedIndexChanged +=
                    new EventHandler(OnPortSelectionChanged);
                players.Controls.Add(drv);
                driverCombo[i] = drv;
            }
            Button refresh = MakeButton(players, "Refresh devices", 14,
                                        144, 120, 24);
            refresh.Click += new EventHandler(OnRefreshDevices);

            p1Hint = new Label();
            p1Hint.Text = "P1 needs a device + controller type!";
            p1Hint.AutoSize = true;
            p1Hint.Location = new Point(144, 149);
            p1Hint.ForeColor = BadColor;
            players.Controls.Add(p1Hint);

            // ---- Bottom: credits + actions
            Label credits = MakeLabel(
                "Made by flowa.", 16, 688, DimColor);
            Label support = MakeLabel(
                "Source code, releases and updates on GitHub:",
                16, 708, TextColor);
            LinkLabel link = new LinkLabel();
            link.Text = "github.com/flowa1911you";
            link.AutoSize = true;
            link.Location = new Point(16, 728);
            link.LinkColor = AccentColor;
            link.ActiveLinkColor = Color.White;
            link.LinkClicked +=
                new LinkLabelLinkClickedEventHandler(OnLinkClicked);
            Controls.Add(link);

            Button save = MakeButton(this, "Save", 540, 706, 100, 34);
            save.Click += new EventHandler(OnSave);
            Button saveLaunch = MakeButton(this, "Save && Launch", 648,
                                           706, 116, 34);
            saveLaunch.BackColor = Color.FromArgb(40, 80, 45);
            saveLaunch.Click += new EventHandler(OnSaveLaunch);
        }

        // Minimal parser for xemu.toml, to show the values xemu is
        // actually using right now
        Dictionary<string, string> ReadXemuToml()
        {
            Dictionary<string, string> map =
                new Dictionary<string, string>();
            // Portable mode first: xemu.toml next to xemu.exe wins (this
            // is how xemu itself resolves it), AppData as fallback
            string path = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "xemu.toml");
            if (!File.Exists(path))
            {
                path = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "xemu", "xemu", "xemu.toml");
            }
            if (!File.Exists(path))
                return map;
            string section = "";
            foreach (string raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                    continue;
                if (line[0] == '[')
                {
                    int e = line.IndexOf(']');
                    if (e > 0)
                        section = line.Substring(1, e - 1).Trim();
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq < 0)
                    continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                val = val.Trim('\'', '"');
                map[section.Length > 0 ? section + "." + key : key] = val;
            }
            return map;
        }

        static string TomlGet(Dictionary<string, string> t, string key,
                              string fallback)
        {
            string v;
            return t.TryGetValue(key, out v) ? v : fallback;
        }

        // What xemu is using right now, one display string per option
        string[] CurrentGraphicsValues()
        {
            string[] cur = new string[11];
            try
            {
                Dictionary<string, string> t = ReadXemuToml();
                string r = TomlGet(t, "display.renderer",
                                   "OPENGL").ToUpperInvariant();
                cur[0] = r == "VULKAN" ? "Vulkan"
                       : r == "NULL" ? "Null (no video)" : "OpenGL";
                cur[1] = TomlGet(t, "display.quality.surface_scale", "1")
                    + "x";
                string ws = TomlGet(t, "display.window.startup_size",
                                    "1280x960");
                cur[2] = ws == "last_used" ? "Last used" : ws;
                cur[3] = TomlGet(t, "display.window.fullscreen_exclusive",
                                 "false") == "true" ? "On" : "Off";
                cur[4] = TomlGet(t, "display.window.vsync", "true")
                    == "true" ? "On" : "Off";
                cur[5] = TomlGet(t, "display.ui.show_notifications",
                                 "true") == "true" ? "On" : "Off";
                if (TomlGet(t, "display.ui.auto_scale", "true") == "true")
                {
                    cur[6] = "Auto";
                }
                else
                {
                    float sc;
                    if (!float.TryParse(
                            TomlGet(t, "display.ui.scale", "1"),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo
                                .InvariantCulture, out sc))
                        sc = 1;
                    cur[6] = ((int)sc) + "x";
                }
                cur[7] = TomlGet(t, "display.ui.use_animations", "true")
                    == "true" ? "On" : "Off";
                string fit = TomlGet(t, "display.ui.fit", "scale");
                cur[8] = fit == "center" ? "Center"
                       : fit == "stretch" ? "Stretch" : "Scale";
                string ar = TomlGet(t, "display.ui.aspect_ratio", "auto");
                cur[9] = ar == "native" ? "Native"
                       : ar == "4x3" ? "4:3"
                       : ar == "16x9" ? "16:9" : "Auto (default)";
                cur[10] = TomlGet(t, "display.filtering", "linear")
                    == "nearest" ? "Nearest" : "Linear";
            }
            catch (Exception)
            {
                for (int i = 0; i < cur.Length; i++)
                    if (cur[i] == null)
                        cur[i] = "?";
            }
            return cur;
        }

        // Graphics tab: mirrors xemu's Display settings menu 1:1
        void BuildGraphicsTab(TabPage page)
        {
            gfxKeys = new string[] {
                "renderer", "resolution_scale", "window_size",
                "fullscreen_exclusive", "vsync", "show_notifications",
                "ui_scale", "animations", "display_mode", "aspect_ratio",
                "filtering"
            };
            string[] labels = new string[] {
                "Renderer backend", "Internal resolution scale",
                "Window size", "Exclusive fullscreen",
                "Vertical refresh sync (VSync)", "Show notifications",
                "UI scale", "UI animations", "Display mode",
                "Aspect ratio", "Filtering"
            };
            string keep = "(keep current)";
            gfxValues = new string[][] {
                new string[] { "", "opengl", "vulkan", "null" },
                new string[] { "", "1", "2", "3", "4", "5", "6", "7", "8",
                               "9", "10" },
                new string[] { "", "last_used", "640x480", "720x480",
                               "1280x720", "1280x800", "1280x960",
                               "1920x1080", "2560x1440", "2560x1600",
                               "2560x1920", "3840x2160" },
                new string[] { "", "1", "0" },
                new string[] { "", "1", "0" },
                new string[] { "", "1", "0" },
                new string[] { "", "auto", "1", "2" },
                new string[] { "", "1", "0" },
                new string[] { "", "center", "scale", "stretch" },
                new string[] { "", "native", "auto", "4x3", "16x9" },
                new string[] { "", "linear", "nearest" }
            };
            string[][] display = new string[][] {
                new string[] { keep, "OpenGL", "Vulkan", "Null (no video)" },
                new string[] { keep, "1x", "2x", "3x", "4x", "5x", "6x",
                               "7x", "8x", "9x", "10x" },
                new string[] { keep, "Last used", "640x480", "720x480",
                               "1280x720", "1280x800", "1280x960",
                               "1920x1080", "2560x1440", "2560x1600",
                               "2560x1920", "3840x2160" },
                new string[] { keep, "On", "Off" },
                new string[] { keep, "On", "Off" },
                new string[] { keep, "On", "Off" },
                new string[] { keep, "Auto", "1x", "2x" },
                new string[] { keep, "On", "Off" },
                new string[] { keep, "Center", "Scale", "Stretch" },
                new string[] { keep, "Native", "Auto (default)", "4:3",
                               "16:9" },
                new string[] { keep, "Linear", "Nearest" }
            };

            // Show what xemu is using right now instead of a generic
            // "(keep current)" - selecting it still means "don't force"
            string[] current = CurrentGraphicsValues();
            for (int i = 0; i < display.Length; i++)
                display[i][0] = "(current: " + current[i] + ")";

            gfxCombo = new ComboBox[gfxKeys.Length];
            for (int i = 0; i < gfxKeys.Length; i++)
            {
                Label l = new Label();
                l.Text = labels[i];
                l.AutoSize = true;
                l.Location = new Point(18, 22 + i * 32);
                l.ForeColor = TextColor;
                page.Controls.Add(l);

                ComboBox c = new ComboBox();
                c.DropDownStyle = ComboBoxStyle.DropDownList;
                c.Bounds = new Rectangle(300, 18 + i * 32, 220, 24);
                c.BackColor = Color.FromArgb(28, 28, 32);
                c.ForeColor = TextColor;
                c.FlatStyle = FlatStyle.Flat;
                for (int j = 0; j < display[i].Length; j++)
                    c.Items.Add(display[i][j]);
                c.SelectedIndex = 0;
                page.Controls.Add(c);
                gfxCombo[i] = c;
            }

            Label note = new Label();
            note.Text = "\"(current: ...)\" shows the value xemu is using "
                + "right now; selecting it leaves the setting untouched.\n"
                + "Anything else you pick here is enforced at every "
                + "startup.";
            note.AutoSize = true;
            note.Location = new Point(18, 26 + gfxKeys.Length * 32);
            note.ForeColor = DimColor;
            page.Controls.Add(note);
        }

        // Mapping tab: click a slot, then press the desired key or mouse
        // button within 10 seconds to bind it. Buttons only - the AIM is
        // locked to the raw input device selected in Players.
        void BuildMappingTab(TabPage page)
        {
            Label head = new Label();
            head.Text = "Gun button mapping (leave empty for the defaults)";
            head.AutoSize = true;
            head.Location = new Point(18, 10);
            head.ForeColor = AccentColor;
            page.Controls.Add(head);

            const int rowH = 27;
            const int rowsTop = 34;
            const int extraGap = 22; // room for the "Additional" divider

            for (int i = 0; i < MapKeys.Length; i++)
            {
                int y = rowsTop + i * rowH
                        + (i >= MapAdditionalFrom ? extraGap : 0);

                if (i == MapAdditionalFrom)
                {
                    Label add = new Label();
                    add.Text = "ADDITIONAL  (service / extra modes in "
                        + "some games)";
                    add.AutoSize = true;
                    add.Location = new Point(18, y - extraGap + 4);
                    add.ForeColor = AccentColor;
                    page.Controls.Add(add);
                }

                Label l = new Label();
                l.Text = MapLabels[i];
                l.AutoSize = true;
                l.Location = new Point(18, y + 4);
                l.ForeColor = TextColor;
                page.Controls.Add(l);

                Button b = MakeButton(page, "", 170, y, 360, 24);
                int idx = i;
                b.Click += delegate(object s, EventArgs e)
                {
                    OnMapBindClick(idx);
                };
                mapButtons[i] = b;

                Button clr = MakeButton(page, "X", 538, y, 30, 24);
                clr.ForeColor = Color.IndianRed;
                clr.Click += delegate(object s, EventArgs e)
                {
                    OnMapClearClick(idx);
                };

                Button def = MakeButton(page, "Default", 574, y, 64, 24);
                def.Click += delegate(object s, EventArgs e)
                {
                    OnMapDefaultClick(idx);
                };
            }

            Label note = new Label();
            note.Text =
                "Click a slot, then press the key or mouse button you want "
                + "within 10 seconds (any detected\nmouse works). X = no "
                + "assignment, Default = value this window was opened "
                + "with.\nThe AIM is not mappable on purpose: it always "
                + "follows the device selected in Players.";
            note.AutoSize = true;
            note.Location = new Point(
                18, rowsTop + MapKeys.Length * rowH + extraGap + 8);
            note.ForeColor = DimColor;
            page.Controls.Add(note);

            mapTimer = new Timer();
            mapTimer.Interval = 1000;
            mapTimer.Tick += new EventHandler(OnMapTimerTick);

            RefreshMapButtons();
        }

        string PrettyMapValue(int i)
        {
            string v = mapValues[i];
            if (v == null || v.Length == 0)
            {
                return MapDefaults[i].Length > 0
                    ? "(default: " + MapDefaults[i] + ")"
                    : "(not set)";
            }
            if (v.StartsWith("key:"))
            {
                return "Keyboard  -  " + v.Substring(4);
            }
            if (v.StartsWith("mouse:"))
            {
                int c = v.LastIndexOf(':');
                if (c > 6)
                {
                    string id = v.Substring(0, c);
                    string btn = v.Substring(c + 1).ToUpperInvariant();
                    string dev = id;
                    foreach (MouseDevice m in mice)
                    {
                        if (m.Id == id) { dev = m.Name; break; }
                    }
                    return btn + "  @  " + dev;
                }
            }
            return v;
        }

        void RefreshMapButtons()
        {
            for (int i = 0; i < MapKeys.Length; i++)
            {
                if (mapButtons[i] != null && mapCapturing != i)
                    mapButtons[i].Text = PrettyMapValue(i);
            }
        }

        void OnMapBindClick(int idx)
        {
            // A capture that just ended on a mouse click also delivers the
            // Click event to this very button: swallow it so binding the
            // left button does not instantly restart the capture.
            if ((DateTime.Now - mapLastCaptureEnd).TotalMilliseconds < 400)
                return;
            if (mapCapturing >= 0)
                EndMapCapture(null); // cancel any previous capture
            mapCapturing = idx;
            mapCountdown = 10;
            mapButtons[idx].Text =
                "Press a key or mouse button...  " + mapCountdown;
            mapButtons[idx].ForeColor = Color.Orange;
            mapTimer.Start();
        }

        void OnMapClearClick(int idx)
        {
            if (mapCapturing == idx)
                EndMapCapture(null);
            mapValues[idx] = "";
            RefreshMapButtons();
        }

        void OnMapDefaultClick(int idx)
        {
            if (mapCapturing == idx)
                EndMapCapture(null);
            mapValues[idx] = mapLoadedValues[idx] == null
                ? "" : mapLoadedValues[idx];
            RefreshMapButtons();
        }

        void OnMapTimerTick(object sender, EventArgs e)
        {
            if (mapCapturing < 0)
            {
                mapTimer.Stop();
                return;
            }
            mapCountdown--;
            if (mapCountdown <= 0)
            {
                EndMapCapture(null); // timeout: keep the old binding
            }
            else
            {
                mapButtons[mapCapturing].Text =
                    "Press a key or mouse button...  " + mapCountdown;
            }
        }

        void EndMapCapture(string newValue)
        {
            mapTimer.Stop();
            mapLastCaptureEnd = DateTime.Now;
            if (mapCapturing >= 0)
            {
                if (newValue != null)
                    mapValues[mapCapturing] = newValue;
                mapButtons[mapCapturing].ForeColor = TextColor;
                int done = mapCapturing;
                mapCapturing = -1;
                mapButtons[done].Text = PrettyMapValue(done);
            }
        }

        // Arrow keys (and other dialog keys) are eaten by WinForms focus
        // navigation before KeyDown: intercept them here so the D-Pad can
        // be bound to the keyboard arrows.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (mapCapturing >= 0)
            {
                string name = SdlKeyName(keyData & Keys.KeyCode);
                if (name != null)
                {
                    EndMapCapture("key:" + name);
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Windows VK -> SDL key name (what xemu feeds into
        // SDL_GetScancodeFromName). Covers the common keys.
        static string SdlKeyName(Keys k)
        {
            if (k >= Keys.A && k <= Keys.Z)
                return k.ToString();
            if (k >= Keys.D0 && k <= Keys.D9)
                return ((int)(k - Keys.D0)).ToString();
            if (k >= Keys.F1 && k <= Keys.F12)
                return k.ToString();
            if (k >= Keys.NumPad0 && k <= Keys.NumPad9)
                return "Keypad " + (int)(k - Keys.NumPad0);
            switch (k)
            {
                case Keys.Up: return "Up";
                case Keys.Down: return "Down";
                case Keys.Left: return "Left";
                case Keys.Right: return "Right";
                case Keys.Return: return "Return";
                case Keys.Space: return "Space";
                case Keys.Escape: return "Escape";
                case Keys.Tab: return "Tab";
                case Keys.Back: return "Backspace";
                case Keys.ShiftKey: return "Left Shift";
                case Keys.ControlKey: return "Left Ctrl";
                case Keys.Menu: return "Left Alt";
                case Keys.OemMinus: return "-";
                case Keys.Oemplus: return "=";
                case Keys.Oemcomma: return ",";
                case Keys.OemPeriod: return ".";
                case Keys.PageUp: return "PageUp";
                case Keys.PageDown: return "PageDown";
                case Keys.Home: return "Home";
                case Keys.End: return "End";
                case Keys.Insert: return "Insert";
                case Keys.Delete: return "Delete";
                default: return null;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (mapCapturing >= 0)
            {
                string name = SdlKeyName(e.KeyCode);
                if (name != null)
                {
                    EndMapCapture("key:" + name);
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                }
            }
            base.OnKeyDown(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_INPUT = 0x00FF;
            if (m.Msg == WM_INPUT && mapCapturing >= 0)
            {
                string spec, btn, path;
                if (RawInputDevices.TryReadMouseButtonDown(
                        m.LParam, out spec, out btn, out path))
                {
                    EndMapCapture(spec);
                }
            }
            base.WndProc(ref m);
        }

        void MakeGunSliders(GroupBox gun)
        {
            Label sl = new Label();
            sl.Text = "Aim smoothing";
            sl.AutoSize = true;
            sl.Location = new Point(14, 26);
            sl.ForeColor = TextColor;
            gun.Controls.Add(sl);

            smoothBar = new TrackBar();
            smoothBar.Bounds = new Rectangle(130, 20, 170, 30);
            smoothBar.Minimum = 0;
            smoothBar.Maximum = 100;
            smoothBar.TickFrequency = 10;
            smoothBar.Value = 20;
            gun.Controls.Add(smoothBar);

            smoothVal = new Label();
            smoothVal.AutoSize = true;
            smoothVal.Location = new Point(308, 26);
            smoothVal.ForeColor = AccentColor;
            smoothVal.Text = "20%";
            gun.Controls.Add(smoothVal);
            smoothBar.ValueChanged += new EventHandler(OnSmoothChanged);

            Label se = new Label();
            se.Text = "Aim sensitivity";
            se.AutoSize = true;
            se.Location = new Point(14, 66);
            se.ForeColor = TextColor;
            gun.Controls.Add(se);

            sensBar = new TrackBar();
            sensBar.Bounds = new Rectangle(130, 60, 170, 30);
            sensBar.Minimum = 50;
            sensBar.Maximum = 150;
            sensBar.TickFrequency = 10;
            sensBar.Value = 100;
            gun.Controls.Add(sensBar);

            sensVal = new Label();
            sensVal.AutoSize = true;
            sensVal.Location = new Point(308, 66);
            sensVal.ForeColor = AccentColor;
            sensVal.Text = "100%";
            gun.Controls.Add(sensVal);
            sensBar.ValueChanged += new EventHandler(OnSensChanged);

            // Values are already tuned: locked by default
            smoothBar.Enabled = false;
            sensBar.Enabled = false;
            unlockGunCk = MakeCheck(gun,
                "Unlock sliders (better not to touch!)", 14, 98);
            unlockGunCk.ForeColor = WarnColor;
            unlockGunCk.CheckedChanged +=
                new EventHandler(OnUnlockGunChanged);
        }

        void OnUnlockGunChanged(object sender, EventArgs e)
        {
            if (unlockGuard)
                return;
            if (unlockGunCk.Checked)
            {
                DialogResult r = MessageBox.Show(this,
                    "These values are already tuned for the best lightgun "
                    + "experience.\r\nIt is better NOT to touch them "
                    + "unless you know what you are doing.\r\n\r\n"
                    + "Unlock anyway?",
                    "Flowa GunSetup - Warning", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (r != DialogResult.Yes)
                {
                    unlockGuard = true;
                    unlockGunCk.Checked = false;
                    unlockGuard = false;
                    return;
                }
            }
            smoothBar.Enabled = unlockGunCk.Checked;
            sensBar.Enabled = unlockGunCk.Checked;
        }

        // ------------------------------------------------------ handlers

        void OnSmoothChanged(object sender, EventArgs e)
        {
            smoothVal.Text = smoothBar.Value + "%";
        }

        void OnSensChanged(object sender, EventArgs e)
        {
            sensVal.Text = sensBar.Value + "%";
        }

        void OnLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/flowa1911you");
        }

        void OnBrowseIso(object sender, EventArgs e)
        {
            OpenFileDialog d = new OpenFileDialog();
            d.Title = "Select game disc image";
            d.Filter = "Xbox disc images (*.iso;*.xiso)|*.iso;*.xiso|"
                + "All files (*.*)|*.*";
            if (d.ShowDialog(this) == DialogResult.OK)
            {
                // Files inside the app folder are shown (and saved) with a
                // relative path so the package stays portable
                string p = d.FileName;
                if (p.StartsWith(baseDir,
                                 StringComparison.OrdinalIgnoreCase))
                    p = p.Substring(baseDir.Length);
                isoBox.Text = p;
            }
        }

        void OnRefreshDevices(object sender, EventArgs e)
        {
            RefreshDevices();
        }

        bool WarnIfP1Missing()
        {
            if (P1Ready())
                return true;
            DialogResult r = MessageBox.Show(this,
                "Player 1 has no input device selected!\r\n"
                + "The lightgun will NOT work without it.\r\n\r\n"
                + "Save anyway?",
                "Flowa GunSetup - Warning", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            return r == DialogResult.Yes;
        }

        void OnSave(object sender, EventArgs e)
        {
            if (!WarnIfP1Missing())
                return;
            SaveIni();
            MessageBox.Show(this,
                "Settings saved to flowa_config.ini.\r\n"
                + "xemu will apply them at the next launch.",
                "Flowa GunSetup", MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        void OnSaveLaunch(object sender, EventArgs e)
        {
            if (!WarnIfP1Missing())
                return;
            SaveIni();
            string exe = Path.Combine(baseDir, "xemu.exe");
            if (!File.Exists(exe))
            {
                MessageBox.Show(this,
                    "xemu.exe not found next to the configurator.\r\n"
                    + "Put FlowaGunSetup.exe in the xemu folder.",
                    "Flowa GunSetup", MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            ProcessStartInfo psi = new ProcessStartInfo(exe);
            psi.WorkingDirectory = baseDir;
            Process.Start(psi);
            Close();
        }

        // ------------------------------------------------------- devices

        void RefreshDevices()
        {
            string[] previous = new string[4];
            for (int i = 0; i < 4; i++)
                previous[i] = SelectedDeviceId(i);

            mice = RawInputDevices.Enumerate();
            for (int i = 0; i < 4; i++)
            {
                ComboBox c = deviceCombo[i];
                c.Items.Clear();
                c.Items.Add(i == 0 ? "-- select device! --"
                                   : "(keep current)");
                c.Items.Add("Keyboard");
                foreach (MouseDevice m in mice)
                    c.Items.Add(m);
                c.SelectedIndex = 0;
                if (previous[i] != null)
                    SelectDeviceId(i, previous[i]);
            }
            UpdateP1Highlight();
        }

        bool P1Ready()
        {
            return deviceCombo[0].SelectedIndex > 0 &&
                   driverCombo[0].SelectedIndex > 0;
        }

        void OnPortSelectionChanged(object sender, EventArgs e)
        {
            UpdateP1Highlight();
        }

        void UpdateP1Highlight()
        {
            if (p1Hint == null)
                return;
            bool devOk = deviceCombo[0].SelectedIndex > 0;
            bool drvOk = driverCombo[0].SelectedIndex > 0;
            playerLabels[0].ForeColor =
                (devOk && drvOk) ? AccentColor : BadColor;
            deviceCombo[0].BackColor =
                devOk ? Color.FromArgb(28, 28, 32)
                      : Color.FromArgb(88, 30, 30);
            driverCombo[0].BackColor =
                drvOk ? Color.FromArgb(28, 28, 32)
                      : Color.FromArgb(88, 30, 30);
            p1Hint.Visible = !(devOk && drvOk);
        }

        string SelectedDeviceId(int port)
        {
            ComboBox c = deviceCombo[port];
            if (c.Items.Count == 0 || c.SelectedIndex <= 0)
                return null;
            if (c.SelectedIndex == 1)
                return "keyboard";
            MouseDevice m = c.SelectedItem as MouseDevice;
            return m != null ? m.Id : null;
        }

        void SelectDeviceId(int port, string id)
        {
            ComboBox c = deviceCombo[port];
            if (string.IsNullOrEmpty(id))
            {
                c.SelectedIndex = 0;
                return;
            }
            if (id == "keyboard")
            {
                c.SelectedIndex = 1;
                return;
            }
            for (int i = 2; i < c.Items.Count; i++)
            {
                MouseDevice m = c.Items[i] as MouseDevice;
                if (m != null && m.Id == id)
                {
                    c.SelectedIndex = i;
                    return;
                }
            }
            c.SelectedIndex = 0;
        }

        // ------------------------------------------------- machine files

        void EnsureMachineDirs()
        {
            foreach (string d in MachineDirs)
            {
                try
                {
                    Directory.CreateDirectory(Path.Combine(baseDir, d));
                }
                catch (Exception) { }
            }
        }

        string FirstMachineFile(string dir)
        {
            string full = Path.Combine(baseDir, dir);
            if (!Directory.Exists(full))
                return null;
            foreach (string f in Directory.GetFiles(full))
            {
                if (f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    continue;
                return Path.GetFileName(f);
            }
            return null;
        }

        void RefreshMachineStatus()
        {
            for (int i = 0; i < 4; i++)
            {
                string found = FirstMachineFile(MachineDirs[i]);
                if (found != null)
                {
                    machineStatus[i].Text = "OK  -  " + found;
                    machineStatus[i].ForeColor = AccentColor;
                }
                else if (MachineDirs[i] == "eeprom")
                {
                    machineStatus[i].Text =
                        "empty (created here at first xemu start)";
                    machineStatus[i].ForeColor = DimColor;
                }
                else
                {
                    machineStatus[i].Text = "MISSING - drop the file in "
                        + MachineDirs[i] + "\\";
                    machineStatus[i].ForeColor = BadColor;
                }
            }
        }

        // ----------------------------------------------------------- INI

        Dictionary<string, string> ReadIniValues()
        {
            Dictionary<string, string> map =
                new Dictionary<string, string>();
            if (!File.Exists(iniPath))
                return map;
            foreach (string raw in File.ReadAllLines(iniPath))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#'
                    || line[0] == '[')
                    continue;
                int eq = line.IndexOf('=');
                if (eq < 0)
                    continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim().Trim('"');
                map[key] = val;
            }
            return map;
        }

        static float ParseFloat(Dictionary<string, string> map,
                                string key, float fallback)
        {
            string s;
            if (!map.TryGetValue(key, out s))
                return fallback;
            float v;
            if (float.TryParse(s,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out v))
                return v;
            return fallback;
        }

        static bool ParseBool(Dictionary<string, string> map, string key,
                              bool fallback)
        {
            string s;
            if (!map.TryGetValue(key, out s))
                return fallback;
            return s != "0" && s.Length > 0;
        }

        static string ParseString(Dictionary<string, string> map,
                                  string key)
        {
            string s;
            map.TryGetValue(key, out s);
            return s ?? "";
        }

        void LoadIni()
        {
            Dictionary<string, string> map = ReadIniValues();

            hideCursorCk.Checked = ParseBool(map, "hide_cursor", false);
            showMenuBarCk.Checked = ParseBool(map, "show_menu_bar", true);
            fullscreenCk.Checked = ParseBool(map, "fullscreen", false);
            skipIntroCk.Checked = ParseBool(map, "skip_boot_anim", false);
            dblClickCk.Checked =
                ParseBool(map, "disable_dblclick_fullscreen", true);
            rClickCk.Checked = ParseBool(map, "disable_rclick_menu", true);

            int smooth = (int)Math.Round(
                ParseFloat(map, "aim_smoothing", 0.2f) * 100f);
            smoothBar.Value = Math.Max(0, Math.Min(100, smooth));
            int sens = (int)Math.Round(
                ParseFloat(map, "aim_sensitivity", 1.0f) * 100f);
            sensBar.Value = Math.Max(50, Math.Min(150, sens));

            isoBox.Text = ParseString(map, "iso_path");

            for (int i = 0; i < MapKeys.Length; i++)
            {
                mapValues[i] = ParseString(map, MapKeys[i]);
                mapLoadedValues[i] = mapValues[i];
            }
            RefreshMapButtons();

            for (int i = 0; i < 4; i++)
            {
                SelectDeviceId(i,
                    ParseString(map, "port" + (i + 1) + "_device"));
                string drv = ParseString(map, "port" + (i + 1)
                                              + "_driver");
                int idx = Array.IndexOf(DriverIniValues, drv);
                if (i == 0 && idx <= 0)
                    idx = 1; // P1 defaults to the lightgun
                driverCombo[i].SelectedIndex = idx >= 0 ? idx : 0;
            }
            UpdateP1Highlight();

            for (int i = 0; i < gfxKeys.Length; i++)
            {
                string v = ParseString(map, gfxKeys[i]);
                int idx = Array.IndexOf(gfxValues[i], v);
                gfxCombo[i].SelectedIndex = idx > 0 ? idx : 0;
            }
        }

        void SaveIni()
        {
            System.Globalization.CultureInfo inv =
                System.Globalization.CultureInfo.InvariantCulture;
            StringBuilder sb = new StringBuilder();
            Action<string> w = delegate(string s)
            {
                sb.Append(s).Append('\n');
            };

            w("; =================================================================");
            w(";  flowa's xemu fork - custom settings");
            w(";  Project home: https://github.com/flowa1911you");
            w(";");
            w(";  These values are applied at EVERY startup and override the");
            w(";  corresponding settings stored in xemu.toml.");
            w(";  Only change what you understand: wrong values can ruin the");
            w(";  lightgun experience. Delete this file to restore the defaults");
            w(";  (it will be regenerated on the next launch).");
            w(";");
            w(";  MACHINE FILES: drop your files into the folders next to xemu.exe");
            w(";  and they are picked up automatically at startup:");
            w(";    bios\\         - flash BIOS image (e.g. Complex_4627.bin)");
            w(";    mcpxbootrom\\  - MCPX boot ROM (e.g. mcpx_1.0.bin)");
            w(";    harddisk\\     - Xbox HDD image (e.g. xbox_hdd.qcow2)");
            w(";    eeprom\\       - EEPROM image (optional, auto-generated if empty)");
            w(";  Remember: EU BIOS -> EU games, US BIOS -> US games!");
            w(";");
            w(";  This file was written by Flowa GunSetup.");
            w("; =================================================================");
            w("");
            w("[ui]");
            w("; 1 = never show the mouse cursor over the game, 0 = visible");
            w("hide_cursor = " + (hideCursorCk.Checked ? "1" : "0"));
            w("");
            w("; 1 = show the top menu bar on mouse move, 0 = always hidden");
            w("show_menu_bar = " + (showMenuBarCk.Checked ? "1" : "0"));
            w("");
            w("; 1 = start xemu in fullscreen, 0 = start windowed");
            w("fullscreen = " + (fullscreenCk.Checked ? "1" : "0"));
            w("");
            w("; 1 = skip the Xbox boot animation, 0 = show the full intro");
            w("skip_boot_anim = " + (skipIntroCk.Checked ? "1" : "0"));
            w("");
            w("[graphics]");
            w("; xemu graphics settings. EMPTY value = keep whatever is");
            w("; saved in xemu itself.");
            for (int i = 0; i < gfxKeys.Length; i++)
            {
                w(gfxKeys[i] + " = "
                  + gfxValues[i][Math.Max(0, gfxCombo[i].SelectedIndex)]);
            }
            w("");
            w("[lightgun]");
            w("; Aim smoothing (adaptive filter): 0.0 = raw, 1.0 = max stability");
            w("aim_smoothing = "
              + (smoothBar.Value / 100f).ToString("0.##", inv));
            w("");
            w("; Aim sensitivity: 1.0 = 1:1 aim, range 0.5 - 1.5");
            w("aim_sensitivity = "
              + (sensBar.Value / 100f).ToString("0.##", inv));
            w("");
            w("; 1 = double left-click does NOT toggle fullscreen");
            w("disable_dblclick_fullscreen = "
              + (dblClickCk.Checked ? "1" : "0"));
            w("");
            w("; 1 = right-click does NOT open the xemu menu");
            w("disable_rclick_menu = " + (rClickCk.Checked ? "1" : "0"));
            w("");
            w("[game]");
            w("; Path of the game ISO/XISO (region must match the BIOS!).");
            w("; Paths inside the xemu folder are saved relative, so the");
            w("; whole package stays 100% portable.");
            string iso = isoBox.Text;
            if (iso.Length > 0 &&
                iso.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
            {
                iso = iso.Substring(baseDir.Length);
            }
            w("iso_path = " + iso);
            w("");
            w("[players]");
            w("; portN_device: device id (mouse:xxxxxxxx or keyboard)");
            w("; portN_driver: lightgun, duke or controller-s");
            for (int i = 0; i < 4; i++)
            {
                string dev = SelectedDeviceId(i);
                string drv = DriverIniValues[
                    Math.Max(0, driverCombo[i].SelectedIndex)];
                w("port" + (i + 1) + "_device = " + (dev ?? ""));
                w("port" + (i + 1) + "_driver = " + drv);
            }
            w("");
            w("[mapping]");
            w("; Gun BUTTON bindings (aim is not mappable: it follows the");
            w("; raw input device selected above). Empty = default.");
            w("; mouse:<id>:<left|right|middle|x1|x2> or key:<name>");
            for (int i = 0; i < MapKeys.Length; i++)
            {
                w(MapKeys[i] + " = "
                  + (mapValues[i] == null ? "" : mapValues[i]));
            }

            File.WriteAllText(iniPath, sb.ToString(),
                              new UTF8Encoding(false));
        }
    }
}
