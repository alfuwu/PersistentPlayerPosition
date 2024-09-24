using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PersistentPlayerPosition {
    public class PositionSavingPlayer : ModPlayer {
        private TagCompound loadedNBT;

        private static bool GetPlayerPos(TagCompound tag, out Vector2 vec) {
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

        public override void OnEnterWorld() {
            Task.Run(async () => {
                while (loadedNBT == null)
                    await Task.Delay(1); // wait until NBT data is loaded
                if (GetPlayerPos(loadedNBT, out Vector2 pos)) {
                    Player.position = pos;
                    Player.fallStart = (int) pos.Y / 16; // prevent taking fall dmg when spawning at lower positions
                }
            });
        }

        public override void SaveData(TagCompound tag) {
            if (loadedNBT != null)
                foreach (KeyValuePair<string, object> kvp in loadedNBT)
                    tag[kvp.Key] = kvp.Value;
            UpdateData(tag);
        }

        public void UpdateData(TagCompound tag) {
            tag ??= loadedNBT;
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

        public override void LoadData(TagCompound tag) {
            loadedNBT = tag;
        }
    }
}