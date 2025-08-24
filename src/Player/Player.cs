using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace CS2ScreenMenuAPI
{
    public static class CCSPlayer
    {
        // Dictionary to store original velocity modifiers for frozen players
        private static readonly Dictionary<CCSPlayerController, float> _originalVelocityModifiers = new();
        private static readonly HashSet<CCSPlayerController> _frozenPlayers = new();
        private static readonly HashSet<CCSPlayerController> _frozenInResolutionMenu = new();
        public static string Localizer(this CCSPlayerController player, string key, params string[] args)
        {
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            Config config = ConfigLoader.Load();

            if (config.Lang.TryGetValue(cultureInfo.Name, out var lang) && lang.TryGetValue(key, out var text))
            {
                return string.Format(text, args);
            }

            string shortName = cultureInfo.TwoLetterISOLanguageName.ToLower();
            if (config.Lang.TryGetValue(shortName, out lang) && lang.TryGetValue(key, out text))
            {
                return string.Format(text, args);
            }

            if (config.Lang.TryGetValue("en", out lang) && lang.TryGetValue(key, out text))
            {
                return string.Format(text, args);
            }
            return key;
        }
        public static CCSPlayerPawn? GetPlayerPawn(this CCSPlayerController player)
        {
            return player.PlayerPawn.Value;
        }
        public static CCSPlayerPawnBase? GetPlayerPawnBase(this CCSPlayerController player)
        {
            return player.GetPlayerPawn() as CCSPlayerPawnBase;
        }

        public static CPointOrient? EnsureCustomView(this CCSPlayerController player, int index)
        {
            return CreateOrGetPointOrient(player);
        }

        public static readonly Dictionary<CCSPlayerController, CPointOrient> PointOrients = new();

        public static CPointOrient? CreateOrGetPointOrient(this CCSPlayerController player)
        {
            if (PointOrients.TryGetValue(player, out var pointOrient))
                return pointOrient;

            var pawn = player.Pawn.Value!;

            var entOrient = Utilities.CreateEntityByName<CPointOrient>("point_orient");
            if (entOrient == null || !entOrient.IsValid)
                return null;

            entOrient.Active = true;
            entOrient.GoalDirection = PointOrientGoalDirectionType_t.eEyesForward;
            entOrient.DispatchSpawn();

            System.Numerics.Vector3 vecPos = (System.Numerics.Vector3)pawn.AbsOrigin! with { Z = pawn.AbsOrigin!.Z + pawn.ViewOffset.Z};
            entOrient.Teleport(vecPos, null, null);
            entOrient.AcceptInput("SetParent", pawn, null, "!activator");
            entOrient.AcceptInput("SetTarget", pawn, null, "!activator");
            //entOrient.AcceptInput("SetParentAttachmentMaintainOffset", pawn, null, "look_straight_ahead_stand");

            PointOrients[player] = entOrient;
            return entOrient;
        }

        public static void Freeze(this CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null) return;

            if (!_originalVelocityModifiers.ContainsKey(player))
            {
                _originalVelocityModifiers[player] = pawn.VelocityModifier;
            }

            _frozenPlayers.Add(player);
        }

        public static void Unfreeze(this CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null) return;

            _frozenPlayers.Remove(player);

            if (_originalVelocityModifiers.TryGetValue(player, out float originalVelocity))
            {
                pawn.VelocityModifier = originalVelocity;
                _originalVelocityModifiers.Remove(player);
            }
        }

        public static void FreezeInResolutionMenu(this CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null) return;

            Config config = ConfigLoader.Load();
            if (!config.Settings.FreezePlayerInResolutionMenu) return;

            if (!_originalVelocityModifiers.ContainsKey(player))
            {
                _originalVelocityModifiers[player] = pawn.VelocityModifier;
            }

            _frozenInResolutionMenu.Add(player);
        }

        public static void UnfreezeFromResolutionMenu(this CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null) return;

            _frozenInResolutionMenu.Remove(player);

            if (!_frozenPlayers.Contains(player) && _originalVelocityModifiers.TryGetValue(player, out float originalVelocity))
            {
                pawn.VelocityModifier = originalVelocity;
                _originalVelocityModifiers.Remove(player);
            }
        }

        public static void UpdateFrozenPlayers()
        {
            foreach (var player in _frozenPlayers.Concat(_frozenInResolutionMenu))
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null)
                {
                    pawn.VelocityModifier = 0f;
                }
            }
            foreach (var player in Utilities.GetPlayers())
            {
                if (MenuAPI.GetActiveMenu(player)!.MenuType == MenuType.Scrollable && MenuAPI.GetActiveMenu(player)!._config.Settings.FreezePlayer)
                {
                    var pawn = player.PlayerPawn.Value;
                    if (pawn != null)
                    {
                        pawn.VelocityModifier = 0f;
                    }
                }
            }
        }

        public static void CleanupFrozenPlayer(CCSPlayerController player)
        {
            _frozenPlayers.Remove(player);
            _frozenInResolutionMenu.Remove(player);
            _originalVelocityModifiers.Remove(player);
        }
        public static void ChangeMoveType(this CBasePlayerPawn pawn, MoveType_t movetype)
        {
            if (pawn.Handle == IntPtr.Zero)
                return;

            pawn.MoveType = movetype;
            Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", movetype);
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
        }
    }
}