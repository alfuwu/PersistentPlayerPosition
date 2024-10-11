using Terraria.DataStructures;
using Terraria.ModLoader;

namespace PersistentPlayerPosition;

public class HiddenPlayerDrawLayer : PlayerDrawLayer {
    public override bool IsHeadLayer => true;

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<PositionSavingPlayer>().LoadedNBT == null;

    public override Position GetDefaultPosition() => new Between(PlayerDrawLayers.Skin, PlayerDrawLayers.Head);

    protected override void Draw(ref PlayerDrawSet drawInfo) => drawInfo.hideEntirePlayer = true;
}
