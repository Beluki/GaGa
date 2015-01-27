
// LowKey.
// A simple low-level keyboard hooker for .NET.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Threading;


namespace LowKey
{
    /// <summary>
    /// All the exceptions that KeyboardHook raises are of this type.
    /// </summary>
    public class KeyboardHookException : Exception
    {
        public KeyboardHookException(String message) : base(message)
        {

        }
    }

    /// <summary>
    /// Gives information about a hotkey when an event is fired.
    /// </summary>
    public class KeyboardHookEventArgs : EventArgs
    {
        public readonly Keys Key;
        public readonly Keys Modifiers;
        public readonly String Name;

        /// <summary>
        /// Information about the current hotkey pressed.
        /// </summary>
        /// <param name="name">
        /// Hotkey name.
        /// </param>
        /// <param name="key">
        /// Base key that was pressed when the event was fired.
        /// </param>
        /// <param name="modifiers">
        /// Modifiers pressed.
        /// </param>
        public KeyboardHookEventArgs(String name, Keys key, Keys modifiers)
        {
            Key = key;
            Modifiers = modifiers;
            Name = name;
        }
    }

    /// <summary>
    /// The LowKey keyboard hooker.
    /// </summary>
    public class KeyboardHook : IDisposable
    {
        ///
        /// Events
        ///

        /// <summary>
        /// Fired when a registered hotkey is released.
        /// </summary>
        public event EventHandler<KeyboardHookEventArgs> HotkeyUp;

        /// <summary>
        /// Fired when a registered hotkey is pressed.
        /// </summary>
        public event EventHandler<KeyboardHookEventArgs> HotkeyDown;

        ///
        /// Helpers
        ///

        /// <summary>
        /// Retrieve the last Windows error as a readable string.
        /// </summary>
        private static String LastWin32Error()
        {
            return new Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        /// <summary>
        /// Determine which modifiers (Keys.Alt, Keys.Control, Keys.Shift)
        /// are currently pressed.
        /// </summary>
        private static Keys PressedModifiers
        {
            get
            {
                Keys modifiers = Keys.None;

                if ((GetAsyncKeyState(VK_MENU) & 0x8000) != 0)
                    modifiers |= Keys.Alt;

                if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
                    modifiers |= Keys.Control;

                if ((GetAsyncKeyState(VK_SHIFT) & 0x8000) != 0)
                    modifiers |= Keys.Shift;

                return modifiers;
            }
        }

        ///
        /// Private data
        ///

        private struct Hotkey
        {
            public readonly Keys Key;
            public readonly Keys Modifiers;

            /// <summary>
            /// Represents a combination of a base key and additional modifiers.
            /// </summary>
            /// <param name="key">
            /// Base key.
            /// </param>
            /// <param name="modifiers">
            /// A bitwise combination of additional modifiers
            /// e.g: Keys.Control | Keys.Alt.
            /// </param>
            public Hotkey(Keys key, Keys modifiers = Keys.None)
            {
                Key = key;
                Modifiers = modifiers;
            }
        }

        /// <summary>
        /// Virtual key code -> set of modifiers for all the hotkeys.
        /// </summary>
        private readonly Dictionary<Int32, HashSet<Keys>> hotkeys;

        /// <summary>
        /// A map from hotkeys to names.
        /// </summary>
        private readonly Dictionary<Hotkey, String> hotkeysToNames;

        /// <summary>
        /// A map from names to hotkeys.
        /// </summary>
        private readonly Dictionary<String, Hotkey> namesToHotkeys;

        /// <summary>
        /// A map from hotkeys to a boolean indicating whether
        /// we should forward the keypress to further applications.
        /// </summary>
        private readonly Dictionary<Hotkey, Boolean> hotkeysForward;

        /// <summary>
        /// Current dispatcher.
        /// </summary>
        private readonly Dispatcher dispatcher;

        /// <summary>
        /// Hook ID.
        /// Will be IntPtr.Zero when not currently hooked.
        /// </summary>
        private IntPtr hookID;

        /// <summary>
        /// Needed to avoid the delegate being garbage-collected.
        /// </summary>
        private static HOOKPROC hookedCallback;

        /// <summary>
        /// Hooker instance.
        /// </summary>
        private static KeyboardHook instance;

        /// <summary>
        /// Create a new keyboard hooker instance.
        /// </summary>
        private KeyboardHook()
        {
            hotkeys = new Dictionary<Int32, HashSet<Keys>>();

            hotkeysToNames = new Dictionary<Hotkey, String>();
            namesToHotkeys = new Dictionary<String, Hotkey>();
            hotkeysForward = new Dictionary<Hotkey, Boolean>();

            dispatcher = Dispatcher.CurrentDispatcher;
            hookID = IntPtr.Zero;
            hookedCallback = Callback;
        }

        ///
        /// Public interface
        ///

        /// <summary>
        /// Get the hooker instance.
        /// </summary>
        public static KeyboardHook Hooker
        {
            get
            {
                if (instance == null)
                {
                    instance = new KeyboardHook();
                }

                return instance;
            }
        }

        /// <summary>
        /// Dispose the hooker.
        /// </summary>
        public void Dispose()
        {
            if (hookID != IntPtr.Zero)
            {
                Unhook();
            }

            instance = null;
        }

        /// <summary>
        /// Add the specified hotkey to the hooker.
        /// </summary>
        /// <param name="name">
        /// Hotkey name.
        /// </param>
        /// <param name="key">
        /// Base key.
        /// </param>
        /// <param name="modifiers">
        /// A bitwise combination of additional modifiers
        /// e.g: Keys.Control | Keys.Alt.
        /// </param>
        /// <param name="forward">
        /// Whether the keypress should be forwarded to
        /// other applications.
        /// </param>
        public void Add(String name, Keys key, Keys modifiers = Keys.None, Boolean forward = false)
        {
            // check name:
            if (name == null)
                throw new KeyboardHookException("Invalid hotkey name.");

            if (namesToHotkeys.ContainsKey(name))
                throw new KeyboardHookException(String.Format("Duplicate hotkey name: {0}.", name));

            // check key code and modifiers:
            Int32 vkCode = (Int32) key;

            // known base key:
            if (hotkeys.ContainsKey(vkCode))
            {
                // check that modifiers are new:
                HashSet<Keys> currentModifiers = hotkeys[vkCode];
                if (currentModifiers.Contains(modifiers))
                {
                    Hotkey previousHotkey = new Hotkey(key, modifiers);
                    throw new KeyboardHookException(
                        String.Format(
                            "Hotkey: {0} already registered as: {1}.",
                            name,
                            hotkeysToNames[previousHotkey]
                        )
                    );
                }

                currentModifiers.Add(modifiers);
            }
            // new base key:
            else
            {
                hotkeys[vkCode] = new HashSet<Keys>() { modifiers };
            }

            // add it to the lookup dicts:
            Hotkey hotkey = new Hotkey(key, modifiers);

            hotkeysToNames[hotkey] = name;
            namesToHotkeys[name] = hotkey;
            hotkeysForward[hotkey] = forward;
        }

        /// <summary>
        /// Remove the specified hotkey.
        /// </summary>
        /// <param name="name">
        /// Hotkey name that was specified when calling Add().
        /// </param>
        public void Remove(String name)
        {
            // check the name:
            if (name == null)
                throw new KeyboardHookException("Invalid hotkey name.");

            if (!namesToHotkeys.ContainsKey(name))
                throw new KeyboardHookException(String.Format("Unknown hotkey name: {0}.", name));

            Hotkey hotkey = namesToHotkeys[name];

            // remove from all dicts:
            Int32 vkCode = (Int32) hotkey.Key;
            Keys modifiers = hotkey.Modifiers;

            hotkeys[vkCode].Remove(modifiers);
            hotkeysToNames.Remove(hotkey);
            namesToHotkeys.Remove(name);
            hotkeysForward.Remove(hotkey);
        }

        /// <summary>
        /// Remove all the registered hotkeys.
        /// </summary>
        public void Clear()
        {
            hotkeys.Clear();
            hotkeysToNames.Clear();
            namesToHotkeys.Clear();
            hotkeysForward.Clear();
        }

        /// <summary>
        /// Modify a hotkey binding.
        /// </summary>
        /// <param name="name">
        /// Hotkey name that was specified when calling Add().
        /// </param>
        /// <param name="key">
        /// New base key.
        /// </param>
        /// <param name="modifiers">
        /// New modifiers.
        /// </param>
        /// <param name="forward">
        /// Whether the keypress should be forwarded to
        /// other applications.
        /// </param>
        public void Rebind(String name, Keys key, Keys modifiers = Keys.None, Boolean forward = false)
        {
            Remove(name);
            Add(name, key, modifiers, forward);
        }

        /// <summary>
        /// Start looking for key presses.
        /// </summary>
        public void Hook()
        {
            // don't hook twice:
            if (hookID != IntPtr.Zero)
            {
                throw new KeyboardHookException("Keyboard hook already active. Call Unhook() first.");
            }

            using (Process process = Process.GetCurrentProcess())
            {
                using (ProcessModule module = process.MainModule)
                {
                    IntPtr hMod = GetModuleHandle(module.ModuleName);
                    hookID = SetWindowsHookEx(WH_KEYBOARD_LL, hookedCallback, hMod, 0);

                    // when SetWindowsHookEx fails, the result is NULL:
                    if (hookID == IntPtr.Zero)
                    {
                        throw new KeyboardHookException("SetWindowsHookEx() failed: " + LastWin32Error());
                    }
                }
            }
        }

        /// <summary>
        /// Stop looking for key presses.
        /// </summary>
        public void Unhook()
        {
            // not hooked:
            if (hookID == IntPtr.Zero)
            {
                throw new KeyboardHookException("Keyboard hook not currently active. Call Hook() first.");
            }

            // when UnhookWindowsHookEx fails, the result is false:
            if (!UnhookWindowsHookEx(hookID))
            {
                throw new KeyboardHookException("UnhookWindowsHookEx() failed: " + LastWin32Error());
            }

            hookID = IntPtr.Zero;
        }

        ///
        /// Actual hooker callback
        ///

        /// <summary>
        /// Callback that intercepts key presses.
        /// </summary>
        private IntPtr Callback(Int32 nCode, IntPtr wParam, IntPtr lParam)
        {
            // assume the hotkey won't match and will be forwarded:
            Boolean forward = true;

            if (nCode >= 0)
            {
                Int32 msg = wParam.ToInt32();

                // we care about keyup/keydown messages:
                if ((msg == WM_KEYUP) || (msg == WM_SYSKEYUP) || (msg == WM_KEYDOWN) || (msg == WM_SYSKEYDOWN))
                {
                    // the virtual key code is the first KBDLLHOOKSTRUCT member:
                    Int32 vkCode = Marshal.ReadInt32(lParam);

                    // base key matches?
                    if (hotkeys.ContainsKey(vkCode))
                    {
                        Keys modifiers = PressedModifiers;

                        // modifiers match?
                        if (hotkeys[vkCode].Contains(modifiers))
                        {
                            Keys key = (Keys) vkCode;
                            Hotkey hotkey = new Hotkey(key, modifiers);
                            String name = hotkeysToNames[hotkey];

                            // override forward with the current hotkey option:
                            forward = hotkeysForward[hotkey];

                            KeyboardHookEventArgs e = new KeyboardHookEventArgs(name, key, modifiers);

                            // call the appropriate event handler using the current dispatcher:
                            if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
                            {
                                if (HotkeyUp != null)
                                {
                                    dispatcher.BeginInvoke(HotkeyUp, new Object[] { instance, e });
                                }
                            }
                            else
                            {
                                if (HotkeyDown != null)
                                {
                                    dispatcher.BeginInvoke(HotkeyDown, new Object[] { instance, e });
                                }
                            }
                        }
                    }
                }
            }

            // forward or return a dummy value other than 0:
            if (forward)
            {
                return CallNextHookEx(hookID, nCode, wParam, lParam);
            }
            else
            {
                return new IntPtr(1);
            }
        }

        ///
        /// Private Windows API declarations
        ///

        private const Int32 VK_SHIFT = 0x10;
        private const Int32 VK_CONTROL = 0x11;
        private const Int32 VK_MENU = 0x12;

        private const Int32 WH_KEYBOARD_LL = 13;

        private const Int32 WM_SYSKEYDOWN = 0x0104;
        private const Int32 WM_SYSKEYUP = 0x0105;
        private const Int32 WM_KEYDOWN = 0x0100;
        private const Int32 WM_KEYUP = 0x0101;

        private delegate IntPtr HOOKPROC(Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(Int32 idHook, HOOKPROC lpfn, IntPtr hMod, UInt32 dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern Boolean UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, Int32 nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(String lpModuleName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern Int16 GetAsyncKeyState(Int32 vKey);
    }
}

