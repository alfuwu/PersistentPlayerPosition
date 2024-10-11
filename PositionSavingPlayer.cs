using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PersistentPlayerPosition {
    public class PositionSavingPlayer : ModPlayer {
        public TagCompound LoadedNBT { get; private set; }

        public override void SaveData(TagCompound tag) {
            if (LoadedNBT != null)
                foreach (KeyValuePair<string, object> kvp in LoadedNBT)
                    tag[kvp.Key] = kvp.Value;
            UpdateData(tag);
        }

        public override void OnEnterWorld() {
            Task.Run(async () => {
                while (LoadedNBT == null)
                    await Task.Delay(1); // wait until NBT data is loaded
                Vector2? pos = PersistentPlayerPosition.GetPlayerPos(LoadedNBT);
                if (pos.HasValue) {
                    Player.position = pos.Value;
                    Player.fallStart = (int)pos.Value.Y / 16; // prevent taking fall dmg when spawning at lower positions
                }
            });
        }

        public void UpdateData(TagCompound tag) {
            tag ??= LoadedNBT;
            if (Player.dead) {
                tag.Remove("pos:" + Main.ActiveWorldFileData.UniqueId);
                tag.Remove("pos:" + Main.worldID + ":" + Main.worldName);
            } else {
                if (ModContent.GetInstance<PPPConfig>().UseUniqueIdForWorldIdentification)
                    tag["pos:" + Main.ActiveWorldFileData] = Player.position;
                else
                    tag["pos:" + Main.worldID + ":" + Main.worldName] = Player.position;
            }
        }

        public override void LoadData(TagCompound tag) => LoadedNBT = tag;
    }
}