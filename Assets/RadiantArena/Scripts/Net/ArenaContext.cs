#nullable enable
// ArenaContext — static cross-boundary snapshot of the duel state.
//
// Gameplay components (UI panels, weapons, states) MUST NOT reach into
// NetClient.Room or DuelState directly. Instead they read from ArenaContext,
// which NetClient hydrates on every OnStateChange. This keeps the networking
// layer mock-swappable for tests + decouples gameplay from Colyseus types.
//
// For D.U2 only the bootstrap-relevant fields are populated. WeaponDb /
// PrefabRegistry / per-trajectory bookkeeping are added by D.U3+ as needed.

namespace RadiantArena.Net
{
    public static class ArenaContext
    {
        public static string MyDiscordId { get; set; } = "";
        public static string OpponentDiscordId { get; set; } = "";
        public static string SessionId { get; set; } = "";
        public static string CurrentPhase { get; set; } = "waiting";
        public static int CurrentRound { get; set; } = 0;
        public static PlayerSnapshot? MyPlayer { get; private set; }
        public static PlayerSnapshot? OpponentPlayer { get; private set; }

        /// <summary>
        /// Pull a fresh snapshot from a Colyseus state diff. Caller (NetClient)
        /// fires events AFTER this returns — read order: hydrate → emit.
        /// </summary>
        public static void HydrateFrom(DuelState state)
        {
            SessionId = state.session_id;
            CurrentPhase = state.phase;
            CurrentRound = state.round;

            MyPlayer = null;
            OpponentPlayer = null;
            OpponentDiscordId = "";

            // MapSchema<PlayerSchema>.Keys is non-generic ICollection — cast each.
            foreach (var keyObj in state.players.Keys)
            {
                if (!(keyObj is string key)) continue;
                var player = state.players[key];
                if (player == null) continue;
                if (key == MyDiscordId)
                {
                    MyPlayer = new PlayerSnapshot(player);
                }
                else
                {
                    OpponentPlayer = new PlayerSnapshot(player);
                    OpponentDiscordId = key;
                }
            }
        }

        /// <summary>Wipe to defaults — used by NetClient on Disconnect.</summary>
        public static void Reset()
        {
            OpponentDiscordId = "";
            SessionId = "";
            CurrentPhase = "waiting";
            CurrentRound = 0;
            MyPlayer = null;
            OpponentPlayer = null;
            // MyDiscordId preserved — set by BootState / ManualRoomConnect,
            // survives reconnect attempts in the same Editor session.
        }
    }

    /// <summary>
    /// Plain-C# copy of PlayerSchema's bootstrap-relevant fields. Avoids
    /// gameplay code holding a reference to the live schema (which Colyseus
    /// mutates on every diff).
    /// </summary>
    public class PlayerSnapshot
    {
        public string DiscordId = "";
        public string DisplayName = "";
        public float X;
        public float Y;
        public int Hp = 100;
        public int HpMax = 100;
        public string SelectedWeaponSlug = "";
        public bool Ready;
        public bool Connected = true;
        public long SignatureCdUntil;

        public WeaponSnapshot[] AvailableWeapons = System.Array.Empty<WeaponSnapshot>();
        public WeaponSnapshot? LockedWeapon;

        public PlayerSnapshot() { }

        public PlayerSnapshot(PlayerSchema p)
        {
            DiscordId = p.discord_id;
            DisplayName = p.display_name;
            X = p.x;
            Y = p.y;
            Hp = p.hp;
            HpMax = p.hp_max;
            SelectedWeaponSlug = p.selected_weapon_slug;
            Ready = p.ready;
            Connected = p.connected;
            SignatureCdUntil = p.signature_cd_until;

            if (p.available_weapons != null && p.available_weapons.Count > 0)
            {
                // ArraySchema<T> doesn't implement IEnumerable<T>; iterate via int indexer.
                var list = new System.Collections.Generic.List<WeaponSnapshot>(p.available_weapons.Count);
                for (int i = 0; i < p.available_weapons.Count; i++)
                {
                    var w = p.available_weapons[i];
                    if (w != null) list.Add(new WeaponSnapshot(w));
                }
                AvailableWeapons = list.ToArray();
            }

            LockedWeapon = (p.weapon != null && !string.IsNullOrEmpty(p.weapon.slug))
                ? new WeaponSnapshot(p.weapon)
                : null;
        }
    }

    /// <summary>
    /// Plain-C# copy of WeaponSchema's display-relevant fields. Lobby + HUD read
    /// from this; never from the live schema. Skills + full stats deferred to
    /// D.U4 (TurnInput) and D.U7 (signature skills).
    /// </summary>
    public class WeaponSnapshot
    {
        public string Slug = "";
        public string DisplayName = "";
        /// <summary>'blunt' | 'pierce' | 'spirit'</summary>
        public string Category = "blunt";
        /// <summary>'ban_menh' | 'pham' | 'dia' | 'thien' | 'tien'</summary>
        public string Tier = "pham";
        /// <summary>Hex color like "#ffaa00", applied as MaterialPropertyBlock tint in D.U8.</summary>
        public string Hue = "#ffffff";

        public WeaponSnapshot() { }

        public WeaponSnapshot(WeaponSchema w)
        {
            Slug = w.slug;
            DisplayName = w.display_name;
            Category = w.category;
            Tier = w.tier;
            Hue = w.visual != null ? w.visual.hue : "#ffffff";
        }
    }
}
