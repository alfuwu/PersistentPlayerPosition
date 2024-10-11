using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PersistentPlayerPosition {
    public class PositionSavingPlayer : ModPlayer {
        public TagCompound LoadedNBT { get; private set; }

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

        public override void SaveData(TagCompound tag) {
            if (LoadedNBT != null)
                foreach (KeyValuePair<string, object> kvp in LoadedNBT)
                    tag[kvp.Key] = kvp.Value;
            UpdateData(tag);
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