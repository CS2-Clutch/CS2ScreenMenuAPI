using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CS2ScreenMenuAPI
{
    public static class MenuAPI
    {
        private static bool s_registered;
        private static readonly Dictionary<CCSPlayerController, Menu> _activeMenus = new();

        #region Events

        private static HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            foreach (var menu in _activeMenus.Values)
                menu.OnRoundStart(@event, info);

            return HookResult.Continue;
        }
        private static HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            foreach (var menu in _activeMenus.Values)
                menu.OnPlayerDisconnect(@event, info);

            return HookResult.Continue;
        }

        private static void OnTick()
        {
            foreach (var menu in _activeMenus.Values)
            {
                menu.OnTick();
            }
        }

        private static void OnCheckTransmit(CCheckTransmitInfoList infoList)
        {
            foreach (var menu in _activeMenus.Values)
                menu.OnCheckTransmit(infoList);
        }

        #endregion

        public static void OpenResolutionMenu(CCSPlayerController player, BasePlugin plugin)
        {
            PlayerRes.CreateResolutionMenu(player, plugin);
        }
        public static Menu? GetActiveMenu(CCSPlayerController player)
        {
            _activeMenus.TryGetValue(player, out var menu);
            return menu;
        }
        public static void CloseActiveMenu(CCSPlayerController player)
        {
            GetActiveMenu(player)?.Close(player);
        }
        public static void CloseAllMenus()
        {
            foreach (var menu in _activeMenus.Values)
            {
                foreach (var p in Utilities.GetPlayers())
                {
                    GetActiveMenu(p)?.Close(p);
                }

            }
            _activeMenus.Clear();
        }
        public static void SetActiveMenu(CCSPlayerController player, Menu? menu)
        {
            if (!s_registered && menu is not null)
            {
                s_registered = true;
                menu.Plugin.RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
                menu.Plugin.RegisterEventHandler<EventRoundStart>(OnRoundStart);
                menu.Plugin.RegisterListener<Listeners.OnTick>(OnTick);
                menu.Plugin.RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);
            }

            if (menu == null)
            {
                if (_activeMenus.ContainsKey(player))
                {
                    _activeMenus[player].Close(player);
                    _activeMenus.Remove(player);
                }
            }
            else
            {
                if (_activeMenus.TryGetValue(player, out var activeMenu))
                {
                    activeMenu.Close(player);
                }
                _activeMenus[player] = menu;
            }
        }
    }
}