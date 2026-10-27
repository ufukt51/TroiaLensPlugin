using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TroiaLens.Npp
{
    /// <summary>Marks a static method to be exported from the DLL under its own name (see tools\DllExport.ps1).</summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class DllExportAttribute : Attribute
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NppData
    {
        public IntPtr NppHandle;
        public IntPtr ScintillaMainHandle;
        public IntPtr ScintillaSecondHandle;
    }

    /// <summary>Docking dialog registration (tTbData in Docking.h).</summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct TbData
    {
        public IntPtr hClient;
        [MarshalAs(UnmanagedType.LPWStr)] public string pszName;
        public int dlgID;
        public uint uMask;
        public IntPtr hIconTab;
        [MarshalAs(UnmanagedType.LPWStr)] public string pszAddInfo;
        public Rect rcFloat;
        public int iPrevCont;
        [MarshalAs(UnmanagedType.LPWStr)] public string pszModuleName;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    /// <summary>SCNotification in Scintilla.h; only the field offsets are used, read from the notification in place.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ScNotification
    {
        public IntPtr hwndFrom, idFrom;
        public uint code;
        public IntPtr position;
        public int ch, modifiers, modificationType;
        public IntPtr text, length, linesAdded;
        public int message;
        public IntPtr wParam, lParam, line;
        public int foldLevelNow, foldLevelPrev, margin, listType, x, y, token;
        public IntPtr annotationLinesAdded;
        public int updated, listCompletionMethod, characterSource;

        public static readonly int ModificationTypeOffset = (int)Marshal.OffsetOf(typeof(ScNotification), nameof(modificationType));
        public static readonly int UpdatedOffset = (int)Marshal.OffsetOf(typeof(ScNotification), nameof(updated));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void NppFuncItemDelegate();

    internal static class NppMsg
    {
        private const uint NPPMSG = 0x0400 + 1000;
        private const uint RUNCOMMAND_USER = 0x0400 + 3000;

        public const uint NPPM_GETCURRENTSCINTILLA = NPPMSG + 4;
        public const uint NPPM_GETCURRENTDOCINDEX = NPPMSG + 23;
        public const uint NPPM_GETMENUHANDLE = NPPMSG + 25;
        public const int NPPPLUGINMENU = 0;
        public const uint NPPM_DMMSHOW = NPPMSG + 30;
        public const uint NPPM_DMMHIDE = NPPMSG + 31;
        public const uint NPPM_DMMREGASDCKDLG = NPPMSG + 33;
        public const uint NPPM_SWITCHTOFILE = NPPMSG + 37;
        public const uint NPPM_SETMENUITEMCHECK = NPPMSG + 40;
        public const uint NPPM_GETPLUGINSCONFIGDIR = NPPMSG + 46;
        public const uint NPPM_ALLOCATEMARKER = NPPMSG + 82;
        public const uint NPPM_GETFULLPATHFROMBUFFERID = NPPMSG + 58;
        public const uint NPPM_GETBUFFERIDFROMPOS = NPPMSG + 59;
        public const uint NPPM_GETCURRENTBUFFERID = NPPMSG + 60;
        public const uint NPPM_GETBUFFERENCODING = NPPMSG + 66;
        public const uint NPPM_ISDARKMODEENABLED = NPPMSG + 107;
        public const uint NPPM_GETSHORTCUTBYCMDID = NPPMSG + 76;
        public const uint NPPM_MENUCOMMAND = NPPMSG + 48;
        public const uint NPPM_GETDARKMODECOLORS = NPPMSG + 108;
        public const uint NPPM_ALLOCATEINDICATOR = NPPMSG + 113;
        public const uint NPPM_GETFULLCURRENTPATH = RUNCOMMAND_USER + 1;

        // Menu command ids (menuCmdID.h): IDM_FILE = IDM + 1000, IDM = 40000
        public const int IDM_FILE_EXIT = 41000 + 11;
        // IDM_SETTING = IDM + 8000
        public const int IDM_SETTING_SHORTCUT_MAPPER = 48000 + 9;

        // Shortcut Mapper dialog (ShortcutMapper_rc.h): its tab bar, its filter box and the plugin commands tab
        public const int IDC_BABYGRID_TABBAR = 2604;
        public const int IDC_BABYGRID_FILTER = 2608;
        public const int MapperPluginTab = 3;

        private const uint NPPN_FIRST = 1000;
        public const uint NPPN_READY = NPPN_FIRST + 1;
        public const uint NPPN_FILECLOSED = NPPN_FIRST + 5;
        public const uint NPPN_FILESAVED = NPPN_FIRST + 8;
        public const uint NPPN_SHUTDOWN = NPPN_FIRST + 9;
        public const uint NPPN_BUFFERACTIVATED = NPPN_FIRST + 10;
        public const uint NPPN_WORDSTYLESUPDATED = NPPN_FIRST + 12;
        public const uint NPPN_DARKMODECHANGED = NPPN_FIRST + 27;
        public const uint NPPN_SHORTCUTREMAPPED = NPPN_FIRST + 13;

        // Docking.h
        public const uint DWS_ICONTAB = 0x00000001;
        public const uint DWS_DF_CONT_RIGHT = 1u << 28; // CONT_RIGHT
        public const int DMN_CLOSE = 1050 + 1;

        // UniMode of NPPM_GETBUFFERENCODING: the encodings a saved file keeps the editor's UTF-8 bytes in
        public const int UNI_UTF8 = 1;
        public const int UNI_COOKIE = 4;
        public const int UNI_7BIT = 5;
    }

    internal static class SciMsg
    {
        public const uint SCI_GETLENGTH = 2006;
        public const uint SCI_GETCURRENTPOS = 2008;
        public const uint SCI_GOTOPOS = 2025;
        public const uint SCI_MARKERDEFINE = 2040;
        public const uint SCI_MARKERSETBACK = 2042;
        public const uint SCI_MARKERADD = 2043;
        public const uint SCI_MARKERDELETE = 2044;
        public const uint SCI_MARKERDELETEALL = 2045;
        public const uint SCI_MARKERGET = 2046;
        public const uint SCI_INDICSETSTYLE = 2080;
        public const uint SCI_INDICSETFORE = 2082;
        public const uint SCI_GETLINEENDPOSITION = 2136;
        public const uint SCI_GETCODEPAGE = 2137;
        public const uint SCI_LINEFROMPOSITION = 2166;
        public const uint SCI_POSITIONFROMLINE = 2167;
        public const uint SCI_GETFIRSTVISIBLELINE = 2152;
        public const uint SCI_GETLINECOUNT = 2154;
        public const uint SCI_GETMODIFY = 2159;
        public const uint SCI_GETTARGETSTART = 2191;
        public const uint SCI_GETTARGETEND = 2193;
        public const uint SCI_SEARCHINTARGET = 2197;
        public const uint SCI_SETSEARCHFLAGS = 2198;
        public const uint SCI_VISIBLEFROMDOCLINE = 2220;
        public const uint SCI_DOCLINEFROMVISIBLE = 2221;
        public const uint SCI_ENSUREVISIBLE = 2232;
        public const uint SCI_GETALLLINESVISIBLE = 2236;
        public const uint SCI_SETMARGINMASKN = 2244;
        public const uint SCI_GETMARGINMASKN = 2245;
        public const uint SCI_GETMARGINS = 2253;
        public const uint SCI_WORDSTARTPOSITION = 2266;
        public const uint SCI_WORDENDPOSITION = 2267;
        public const uint SCI_MARKERSETBACKTRANSLUCENT = 2295;
        public const uint SCI_LINESONSCREEN = 2370;
        public const uint SCI_GRABFOCUS = 2400;
        public const uint SCI_MARKERSETALPHA = 2476;
        public const uint SCI_STYLEGETBACK = 2482;
        public const uint SCI_SETINDICATORCURRENT = 2500;
        public const uint SCI_SETINDICATORVALUE = 2502;
        public const uint SCI_INDICATORFILLRANGE = 2504;
        public const uint SCI_INDICATORCLEARRANGE = 2505;
        public const uint SCI_INDICSETUNDER = 2510;
        public const uint SCI_GETCHARACTERPOINTER = 2520;
        public const uint SCI_INDICSETALPHA = 2523;
        public const uint SCI_INDICSETOUTLINEALPHA = 2558;
        public const uint SCI_SETFIRSTVISIBLELINE = 2613;
        public const uint SCI_GETRANGEPOINTER = 2643;
        public const uint SCI_INDICSETFLAGS = 2684;
        public const uint SCI_SETTARGETRANGE = 2686;
        public const uint SCI_MARKERSETLAYER = 2735;

        public const uint SCN_DOUBLECLICK = 2006;
        public const uint SCN_UPDATEUI = 2007;
        public const uint SCN_MODIFIED = 2008;

        public const int SC_CP_UTF8 = 65001;
        public const int SC_MARK_EMPTY = 5;
        public const int SC_MARK_BACKGROUND = 22;
        public const int SC_MARK_LEFTRECT = 27;
        public const int SC_ALPHA_NOALPHA = 256;
        public const int SC_LAYER_BASE = 0;
        public const int SC_LAYER_UNDER_TEXT = 1;
        public const int SC_LAYER_OVER_TEXT = 2;
        public const int STYLE_DEFAULT = 32;
        public const int INDIC_SQUIGGLE = 1;
        public const int INDIC_ROUNDBOX = 7;
        public const int INDIC_TEXTFORE = 17;
        public const int SC_INDICFLAG_VALUEFORE = 1;
        public const int SC_INDICVALUEBIT = 0x1000000;
        public const int SCFIND_WHOLEWORD = 2;
        public const int SCFIND_MATCHCASE = 4;
        public const int SC_MOD_INSERTTEXT = 0x1;
        public const int SC_MOD_DELETETEXT = 0x2;
        public const int SC_UPDATE_CONTENT = 0x1;
        public const int SC_UPDATE_V_SCROLL = 0x4;
    }

    internal static class Win32
    {
        public const int WM_NOTIFY = 0x004E;
        public const uint WM_COMMAND = 0x0111;

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, string lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, out int lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, byte[] lParam);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        public const uint MF_STRING = 0x0000;
        public const uint MF_POPUP = 0x0010;
        public const uint MF_BYCOMMAND = 0x0000;
        public const uint MF_BYPOSITION = 0x0400;

        [DllImport("user32.dll")]
        public static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool AppendMenu(IntPtr hMenu, uint flags, UIntPtr id, string text);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool InsertMenu(IntPtr hMenu, uint position, uint flags, IntPtr idOrPopup, string text);

        [DllImport("user32.dll")]
        public static extern bool RemoveMenu(IntPtr hMenu, uint position, uint flags);

        [DllImport("user32.dll")]
        public static extern int GetMenuItemCount(IntPtr hMenu);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSubMenu(IntPtr hMenu, int position);

        [DllImport("user32.dll")]
        public static extern uint GetMenuItemID(IntPtr hMenu, int position);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool ModifyMenu(IntPtr hMenu, uint position, uint flags, UIntPtr id, string text);

        public const uint TCM_SETCURSEL = 0x130C;
        public const int TCN_SELCHANGE = -551;

        [StructLayout(LayoutKind.Sequential)]
        public struct NMHDR
        {
            public IntPtr hwndFrom;
            public UIntPtr idFrom;
            public int code;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref NMHDR lParam);

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(uint threadId, EnumWindowsProc callback, IntPtr lParam);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, int[] lParam);

        public const uint LVM_GETHEADER = 0x101F;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string appName, string idList);

        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hWnd, int attribute, ref int value, int size);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDlgItem(IntPtr hDlg, int id);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool SetWindowText(IntPtr hWnd, string text);

        [DllImport("user32.dll")]
        public static extern bool CheckMenuRadioItem(IntPtr hMenu, uint first, uint last, uint check, uint flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder value, int size, string file);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern bool WritePrivateProfileString(string section, string key, string value, string file);
    }
}
