using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
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

        private void Spawn(ILContext il) {
            try {
                ILCursor c = new(il);
                c.GotoNext(MoveType.After, i => i.MatchStloc1());
                ILLabel vanilla = il.DefineLabel();
                c.Emit(OpCodes.Ldarg_0); // load player var
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate(async (Player p, PlayerSpawnContext context) => {
                    return await Task.Run(async () => {
                        TagCompound loadedNBT = p.GetModPlayer<PositionSavingPlayer>().LoadedNBT;
                        while (loadedNBT == null)
                            await Task.Delay(1); // wait until NBT data is loaded
                        if (context == PlayerSpawnContext.SpawningIntoWorld && PositionSavingPlayer.GetPlayerPos(p.GetModPlayer<PositionSavingPlayer>().LoadedNBT, out Vector2 vec)) {
                            p.position = vec;
                            return true;
                        }
                        return false;
                    });
                });
                c.Emit(OpCodes.Brfalse_S, vanilla);
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
