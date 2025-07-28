using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace InfiniteWaterCanRework
{
    public enum GrowthMode
    {
        Disabled,
        Increase1Stage,
        InstantGrow
    }

    public class ModConfig
    {
        public bool Enabled { get; set; } = false;
        public KeybindList ToggleKey { get; set; } = new KeybindList(SButton.F5);
        public int Radius { get; set; } = 0;
        public GrowthMode CropGrowth { get; set; } = GrowthMode.Disabled;
    }

    public class ModEntry : Mod
    {
        private ModConfig Config;
        private IGenericModConfigMenuApi Gmcm;

        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            WateringCanAoEPatch.Config = Config;

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += ToggleOnHotkey;
            helper.Events.GameLoop.UpdateTicked += RefillWaterCan;

            try
            {
                var harmony = new Harmony(ModManifest.UniqueID);
                harmony.PatchAll();
                Monitor.Log("Harmony patches applied.", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed to patch: {ex}", LogLevel.Error);
            }
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            Gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (Gmcm == null)
                return;

            Gmcm.Register(
                mod: ModManifest,
                reset: () =>
                {
                    Config = new ModConfig();
                    WateringCanAoEPatch.Config = Config;
                },
                save: () =>
                {
                    Helper.WriteConfig(Config);
                    WateringCanAoEPatch.Config = Config;
                }
            );

            Gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.enable_name"),
                tooltip: () => Helper.Translation.Get("config.enable_tooltip"),
                getValue: () => Config.Enabled,
                setValue: v => Config.Enabled = v
            );

            Gmcm.AddKeybindList(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.toggle_name"),
                tooltip: () => Helper.Translation.Get("config.toggle_tooltip"),
                getValue: () => Config.ToggleKey,
                setValue: v => Config.ToggleKey = v
            );

            Gmcm.AddNumberOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.radius_name"),
                tooltip: () => Helper.Translation.Get("config.radius_tooltip"),
                min: 0,
                max: 10,
                getValue: () => Config.Radius,
                setValue: v => Config.Radius = v
            );

            var allowedGrowthModes = new[] { "Disabled", "Increase1Stage", "InstantGrow" };

            Gmcm.AddTextOption(
                mod: ModManifest,
                name: () => Helper.Translation.Get("config.cropgrowth_name"),
                tooltip: () => Helper.Translation.Get("config.cropgrowth_tooltip"),
                getValue: () => Config.CropGrowth.ToString(),
                setValue: value =>
                {
                    if (Enum.TryParse(value, out GrowthMode mode))
                        Config.CropGrowth = mode;
                },
                allowedValues: allowedGrowthModes
            );
        }

        private void ToggleOnHotkey(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.ToggleKey.JustPressed())
                return;

            Config.Enabled = !Config.Enabled;
            Helper.WriteConfig(Config);
            WateringCanAoEPatch.Config = Config;

            string message = Config.Enabled ? "Water mod enabled" : "Water mod disabled";
            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
        }

        private void RefillWaterCan(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady || !Config.Enabled)
                return;

            if (Game1.player.CurrentTool is WateringCan can && can.waterCanMax > 0)
                can.WaterLeft = can.waterCanMax;
        }
    }
}