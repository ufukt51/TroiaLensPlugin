using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TroiaLens.Npp
{
    /// <summary>Default shortcut of a menu command (struct ShortcutKey { bool ctrl, alt, shift; UCHAR key; }).</summary>
    internal readonly struct ShortcutKey
    {
        public readonly bool Ctrl, Alt, Shift;
        public readonly byte Key;

        public ShortcutKey(bool ctrl, bool alt, bool shift, char key)
        {
            Ctrl = ctrl;
            Alt = alt;
            Shift = shift;
            Key = (byte)key;
        }

        /// <summary>Ctrl+Alt+Shift with a letter or digit: the combination Notepad++ and the other plugins leave free.</summary>
        public static ShortcutKey CtrlAltShift(char key) => new ShortcutKey(true, true, true, key);

        public int Packed => (Ctrl ? 1 : 0) | (Alt ? 1 : 0) << 8 | (Shift ? 1 : 0) << 16 | Key << 24;
    }

    /// <summary>
    /// Native FuncItem array handed to Notepad++ by getFuncsArray. Notepad++ writes the command ids
    /// back into this memory, so it stays allocated for the life of the process.
    /// </summary>
    internal sealed class FuncItems
    {
        // struct FuncItem { wchar_t _itemName[64]; PFUNCPLUGINCMD _pFunc; int _cmdID; bool _init2Check; ShortcutKey* _pShKey; }
        private const int NameChars = 64;
        private static readonly int FuncOffset = NameChars * 2;
        private static readonly int CmdIdOffset = FuncOffset + IntPtr.Size;
        private static readonly int CheckOffset = CmdIdOffset + 4;
        private static readonly int ShortcutOffset = (CheckOffset + 1 + IntPtr.Size - 1) / IntPtr.Size * IntPtr.Size;
        private static readonly int ItemSize = ShortcutOffset + IntPtr.Size;

        private readonly List<(string Name, NppFuncItemDelegate Func, bool Checked, ShortcutKey? Key)> _items =
            new List<(string, NppFuncItemDelegate, bool, ShortcutKey?)>();
        private IntPtr _native = IntPtr.Zero;

        public int Count => _items.Count;

        /// <summary>
        /// Adds a menu command and returns its index. A null action adds a separator. The index is the command's id in
        /// shortcuts.xml, so a shortcut the user set in the Shortcut Mapper follows it: add new commands at the end.
        /// </summary>
        public int Add(string name, Action action, bool isChecked = false, ShortcutKey? key = null)
        {
            if (_native != IntPtr.Zero) throw new InvalidOperationException("Menu already handed to Notepad++");
            // The delegate is kept in the list so the GC never collects a callback Notepad++ still holds
            NppFuncItemDelegate func = action == null ? null : new NppFuncItemDelegate(action);
            _items.Add((name ?? "", func, isChecked, key));
            return _items.Count - 1;
        }

        public IntPtr Pointer
        {
            get
            {
                if (_native != IntPtr.Zero) return _native;
                _native = Marshal.AllocHGlobal(ItemSize * _items.Count);
                for (int i = 0; i < _items.Count; i++)
                {
                    IntPtr item = _native + i * ItemSize;
                    for (int b = 0; b < ItemSize; b++) Marshal.WriteByte(item, b, 0);
                    char[] name = _items[i].Name.ToCharArray();
                    Marshal.Copy(name, 0, item, Math.Min(name.Length, NameChars - 1));
                    Marshal.WriteIntPtr(item, FuncOffset, _items[i].Func == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(_items[i].Func));
                    Marshal.WriteByte(item, CheckOffset, (byte)(_items[i].Checked ? 1 : 0));
                    if (_items[i].Key is ShortcutKey key)
                    {
                        // Read by Notepad++ when it builds the menu; it keeps its own copy, and shortcuts.xml overrides it
                        IntPtr sk = Marshal.AllocHGlobal(4);
                        Marshal.WriteInt32(sk, key.Packed);
                        Marshal.WriteIntPtr(item, ShortcutOffset, sk);
                    }
                }
                return _native;
            }
        }

        /// <summary>The item's name, as the Shortcut Mapper lists it.</summary>
        public string Name(int index) => _items[index].Name;

        /// <summary>Whether the item is a command rather than a separator.</summary>
        public bool IsCommand(int index) => _items[index].Func != null;

        /// <summary>Command id Notepad++ assigned to the item (valid after getFuncsArray).</summary>
        public int CmdId(int index) => _native == IntPtr.Zero ? 0 : Marshal.ReadInt32(_native + index * ItemSize + CmdIdOffset);
    }
}
