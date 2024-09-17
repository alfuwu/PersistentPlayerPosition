using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PersistentPlayerPosition {
    public class PositionSavingPlayer : ModPlayer {
        private TagCompound loadedNBT;

        public override void OnEnterWorld() {
            Task.Run(async () => {
                while (loadedNBT == null)
                    await Task.Delay(1);
                if (loadedNBT.TryGet("pos:" + Main.worldID + ":" + Main.worldName, out Vector2 pos))
                    Player.position = pos;
            });
        }

        public override void SaveData(TagCompound tag) {
            if (loadedNBT != null)
                foreach (KeyValuePair<string, object> kvp in loadedNBT)
                    tag[kvp.Key] = kvp.Value;
            if (Player.dead)
                tag.Remove("pos:" + Main.worldID + ":" + Main.worldName);      
            else
                tag["pos:" + Main.worldID + ":" + Main.worldName] = Player.position;
            //loadedNBT = null;
        }

        public override void LoadData(TagCompound tag) {
            loadedNBT = tag;
        }
    }
}