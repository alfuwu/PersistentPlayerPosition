using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PersistentPlayerPosition {
    public class PositionSavingPlayer : ModPlayer {
        public TagCompound LoadedNBT { get; private set; }

        public override void OnEnterWorld() {
            // make sure NBT data isnt null after fully loading into a world
            // it will be null if the player character was created without this mod enabled and hasn't played a world while the mod was enabled
            LoadedNBT ??= [];

            if (PersistentPlayerPosition.GetPlayerPos(LoadedNBT, out Vector2 pos)) {
                Player.position = pos;
                Player.fallStart = (int)pos.Y / 16;
            }
        }

        public override void Initialize() {
            // more making sure nbt data isn't null
            LoadedNBT ??= [];
        }

        public override void SaveData(TagCompound tag) {
            if (LoadedNBT != null)
                foreach (KeyValuePair<string, object> kvp in LoadedNBT)
                    tag[kvp.Key] = kvp.Value;
            UpdateData(tag);
        }

        // prevent players from dying in dont dig up/zenith seed bc they're out of bounds
        // not sure this is needed anymore?
        //public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource) =>
        //    damageSource.SourceOtherIndex == 19 && (LoadedNBT != null || Player.position.X + Player.position.Y > 0);

        public void RemoveData(TagCompound tag) {
            tag ??= LoadedNBT;
            tag.Remove("pos:" + Main.ActiveWorldFileData.UniqueId.ToString());
            tag.Remove("pos:" + Main.worldID + ":" + Main.worldName);
        }

        public void UpdateData(TagCompound tag) {
            tag ??= LoadedNBT;
            if (Player.dead) {
                // remove the player's saved position data so they spawn at their spawnpoint
                RemoveData(tag);
            } else {
                string id = PersistentPlayerPosition.TagId();
                tag[id] = Player.position;
                ((TagCompound)tag[id])["facing"] = Player.direction;
            }
        }

        public override void LoadData(TagCompound tag) {
            LoadedNBT = tag;
            // likely redundant
            LoadedNBT ??= [];
        }
    }
}