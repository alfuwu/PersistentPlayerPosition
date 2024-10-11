using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.ComponentModel;
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

        public static Vector2? GetPlayerPos(TagCompound tag) {
            if (tag == null)
                return null;
            if (tag.TryGet("pos:" + Main.worldID + ":" + Main.worldName, out Vector2 pos))
                return pos;
            else if (ModContent.GetInstance<PPPConfig>().UseUniqueIdForWorldIdentification && tag.TryGet("pos:" + Main.ActiveWorldFileData.UniqueId, out Vector2 pos2))
                return pos2;
            return null;
        }

        private void Spawn(ILContext il) {
            try {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, i => i.MatchStloc1());
                ILLabel vanilla = il.DefineLabel();
                c.Emit(OpCodes.Ldarg_1); // load PlayerSpawnContext
                c.EmitDelegate((PlayerSpawnContext context) => context == PlayerSpawnContext.SpawningIntoWorld);
                c.Emit(OpCodes.Brfalse_S, vanilla);
                c.Emit(OpCodes.Ldarg_0); // load player var
                c.EmitDelegate((Player player) => GetPlayerPos(player.GetModPlayer<PositionSavingPlayer>().LoadedNBT).HasValue);
                c.Emit(OpCodes.Brfalse_S, vanilla);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Player player) => {
                    player.position = GetPlayerPos(player.GetModPlayer<PositionSavingPlayer>().LoadedNBT).Value;
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
