using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace PersistentPlayerPosition {
	public class PersistentPlayerPosition : Mod {
        public override void Load() {
            if (ModLoader.HasMod("SubworldLibrary"))
                SubworldLibraryHook.Load();
        }

        public override void Unload() {
            if (ModLoader.HasMod("SubworldLibrary"))
                SubworldLibraryHook.Unload();
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
