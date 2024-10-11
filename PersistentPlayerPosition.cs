using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Stubble.Core.Classes;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;

namespace PersistentPlayerPosition {
	public class PersistentPlayerPosition : Mod {
        public override void Load() {
            if (ModLoader.HasMod("SubworldLibrary"))
                SubworldLibraryHook.Load();
            IL_Player.Spawn += Spawn;
        }

        public override void Unload() {
            if (ModLoader.HasMod("SubworldLibrary"))
                SubworldLibraryHook.Unload();
            IL_Player.Spawn -= Spawn;
        }

        public static bool GetPlayerPos(TagCompound tag, out Vector2 vec) {
            if (tag.TryGet("pos:" + Main.worldID + ":" + Main.worldName, out Vector2 pos)) {
                vec = pos;
                return true;
            } else if (ModContent.GetInstance<PPPConfig>().UseUniqueIdForWorldIdentification && tag.TryGet("pos:" + Main.ActiveWorldFileData.UniqueId, out Vector2 pos2)) {
                vec = pos2;
                return true;
            }
            vec = default;
            return false;
        }

        public static void SetPosition(Player player, TagCompound tag) {
            if (GetPlayerPos(tag, out Vector2 vec))
                player.position = vec;
            else if (player.SpawnX >= 0 && player.SpawnY >= 0)
                typeof(Player).GetMethod("Spawn_SetPosition", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, [player.SpawnX, player.SpawnY]);
            else
                typeof(Player).GetMethod("Spawn_SetPositionAtWorldSpawn", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, []);
        }

        private void Spawn(ILContext il) {
            try {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, i => i.MatchStloc1());
                ILLabel vanilla = il.DefineLabel();
                c.Emit(OpCodes.Ldarg_1); // load PlayerSpawnContext
                c.EmitDelegate((PlayerSpawnContext context) => context == PlayerSpawnContext.SpawningIntoWorld);
                c.Emit(OpCodes.Brfalse_S, vanilla);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Player player) => {
                    PositionSavingPlayer p = player.GetModPlayer<PositionSavingPlayer>();
                    if (p.LoadedNBT == null)
                        Task.Run(async () => {
                            while (p.LoadedNBT == null)
                                await Task.Delay(1);
                            SetPosition(player, p.LoadedNBT);
                        });
                    else
                        SetPosition(player, p.LoadedNBT);
                });
                ILLabel skipPositionSetting = il.DefineLabel();
                c.Emit(OpCodes.Br_S, skipPositionSetting);
                c.MarkLabel(vanilla);
                c.GotoNext(i => i.MatchLdarg0(),
                    i => i.MatchLdcI4(0),
                    i => i.MatchStfld<Entity>("wet"));
                c.MarkLabel(skipPositionSetting);
            } catch (Exception e) {
                MonoModHooks.DumpIL(this, il);
                throw new ILPatchFailureException(this, il, e);
            }
        }
    }

    public class PPPConfig : ModConfig {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("General")]

        [DefaultValue(false)]
        public bool UseUniqueIdForWorldIdentification { get; set; }

        [Header("Subworlds")]

        [DefaultValue(true)]
        public bool SavePositionWhenEnteringSubworld { get; set; }

        [DefaultValue(true)]
        public bool ReturnToPrevPositionWhenExitingSubworld { get; set; }
    }
}
