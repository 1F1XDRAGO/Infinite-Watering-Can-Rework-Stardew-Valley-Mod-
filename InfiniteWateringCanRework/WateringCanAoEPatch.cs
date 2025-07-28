using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace InfiniteWaterCanRework
{
    [HarmonyPatch(typeof(WateringCan), nameof(WateringCan.DoFunction))]
    internal static class WateringCanAoEPatch
    {
        public static ModConfig Config { get; set; }

        private static void Postfix(
            WateringCan __instance,
            GameLocation location,
            int x,
            int y,
            int power,
            Farmer who
        )
        {
            if (Config == null || !Config.Enabled)
                return;
            if (who.CurrentTool != __instance)
                return;

            int centerX = x / Game1.tileSize;
            int centerY = y / Game1.tileSize;
            int r = Config.Radius;

            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    Vector2 tile = new(centerX + dx, centerY + dy);
                    if (!location.isTileOnMap(tile))
                        continue;

                    if (
                        location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature)
                        && feature is HoeDirt dirt
                    )
                    {
                        dirt.state.Value = HoeDirt.watered;

                        if (dirt.crop == null)
                            continue;

                        switch (Config.CropGrowth)
                        {
                            case GrowthMode.InstantGrow:
                                dirt.crop.growCompletely();
                                dirt.crop.updateDrawMath(tile);
                                break;

                            case GrowthMode.Increase1Stage:
                                if (dirt.crop.currentPhase.Value < dirt.crop.phaseDays.Count - 1)
                                {
                                    dirt.crop.currentPhase.Value++;
                                    dirt.crop.dayOfCurrentPhase.Value = 0;
                                    dirt.crop.updateDrawMath(tile);
                                }
                                break;

                            case GrowthMode.Disabled:
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}