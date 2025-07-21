using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PersistentPlayerPosition {
    public class PositionSavingPlayer : ModPlayer {
        public TagCompound LoadedNBT { get; private set; }

        public override void OnEnterWorld() { // some insurance
            LoadedNBT ??= [];

            if (PersistentPlayerPosition.GetPlayerPos(LoadedNBT, out Vector2 pos)) {
                Player.position = pos;
                Player.fallStart = (int)pos.Y / 16;
            }
        }

        public override void SaveData(TagCompound tag) {
            if (LoadedNBT != null)
                foreach (KeyValuePair<string, object> kvp in LoadedNBT)
                    tag[kvp.Key] = kvp.Value;
            UpdateData(tag);
        }

        // prevent players from dying in dont dig up/zenith seed bc they're out of bounds
        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource) => LoadedNBT != null || Player.position.X + Player.position.Y > 0;

        public void UpdateData(TagCompound tag) {
            tag ??= LoadedNBT;
            if (Player.dead) {
                tag.Remove("pos:" + Main.ActiveWorldFileData.UniqueId.ToString());
                tag.Remove("pos:" + Main.worldID + ":" + Main.worldName);
            } else {
                if (ModContent.GetInstance<PPPConfig>().UseUniqueIdForWorldIdentification)
                    tag["pos:" + Main.ActiveWorldFileData.UniqueId.ToString()] = Player.position;
                else
                    tag["pos:" + Main.worldID + ":" + Main.worldName] = Player.position;
            }
        }

        public override void LoadData(TagCompound tag) {
            LoadedNBT = tag;
            LoadedNBT ??= []; // insurance, in case tag is null for whatever reason (dont want to make players immortal)
        }
    }
}