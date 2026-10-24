using Kydl.Common;
using Kydl.Ui;
using UnityEngine;
using UnityEngine.UIElements;

namespace WhipItUp.Ui
{
    /// <summary>
    /// The mod's menu in game ([General] MenuKey): opens / closes with the key, Escape closes; the character does not move while it is open.
    /// Only the GAME side lives here (keys, input lock, game data). What the menu shows is built by MainMenuView (Unity project,
    /// WhipItUp-Unity/Assets/WhipItUp_UI/Runtime), which the Unity preview shows too - change the look there.
    /// </summary>
    internal static class MainMenu
    {
        const string LockName = "WhipItUp.MainMenu";
        static VisualElement _window;

        internal static bool IsOpen { get { return _window != null; } }

        /// Every frame (Plugin.Update). Expected outcome: returns whether the menu is open after this frame.
        internal static bool Tick()
        {
            var key = ModSettings.MenuKey != null ? ModSettings.MenuKey.Value : KeyCode.None;
            if (!IsOpen)
            {
                if (key == KeyCode.None || !ZInput.GetKeyDown(key, true)) return IsOpen;
                if (Chat.instance != null && Chat.instance.HasFocus() || global::Console.IsVisible() || Menu.IsVisible())
                {
                    ModLog.Verbose("MainMenu: key pressed while chat / console / game menu is open - not opened");
                    return IsOpen;
                }
                Open();
                return IsOpen;
            }
            if (ZInput.GetKeyDown(key, true) || ZInput.GetKeyDown(KeyCode.Escape, true)) Close("key");
            return IsOpen;
        }

        static void Open()
        {
            var root = UiKit.Root;
            if (root == null) { ModLog.Warn("MainMenu: UI Toolkit is not available, the menu cannot open"); return; }
            var weapons = WeaponCatalog.Build(k => Loc.T(k), s => ModLog.Verbose(s), WeaponStatsReader.Read);
            var viewer = ModSettings.Viewer();
            _window = MainMenuView.Build(k => Loc.T(k), weapons, () => Close("X"), 0, 0f, () => WeaponViewerScene.Make(viewer), viewer);
            root.Add(_window);
            UiInputLock.Hold(LockName);
            ModLog.Verbose("MainMenu: opened with " + weapons.Count + " weapon(s)");
        }

        const string DropdownName = "kydl-dropdown";   // the name the shared UiControls.Dropdown gives its list
        static void Close(string why)
        {
            if (_window != null) _window.RemoveFromHierarchy();
            _window = null;
            // D-077 F-4: an open drop-down list (shared Kydl.Ui, a sibling of the window in UiKit.Root) is not part of the window, so it would stay on screen
            int lists = RemoveDropdownLists();
            UiInputLock.Release(LockName);
            ModLog.Verbose("MainMenu: closed (" + why + "), drop-down lists removed with it: " + lists);
        }

        static int RemoveDropdownLists()
        {
            var root = UiKit.Root;
            if (root == null) { ModLog.Verbose("MainMenu: no UI root while closing, no drop-down list to remove"); return 0; }
            var found = root.Query<VisualElement>(DropdownName).ToList();
            foreach (var list in found) list.RemoveFromHierarchy();
            return found.Count;
        }
    }
}
