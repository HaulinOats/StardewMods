using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GenericModConfigMenu;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace DynamicCrops
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private string balanceMode;
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //get values from config
            //this.Config = this.Helper.ReadConfig<ModConfig>();
            //balanceMode = Config.balanceMode;
            //Monitor.Log($"balance mode = {balanceMode}", LogLevel.Debug);

            helper.Events.GameLoop.SaveCreated += OnSaveCreation;
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnSaveCreation(object sender, SaveCreatedEventArgs e)
        {
            Monitor.Log($"{Game1.player.Name}'s game has loaded...", LogLevel.Debug);

            Monitor.Log("... DynamicCrops save file data created", LogLevel.Debug);
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.Name.IsEquivalentTo("TileSheets/crops"))
            {
                e.LoadFromModFile<Texture2D>("assets/crops.png", AssetLoadPriority.Medium);
            }
            if (e.Name.IsEquivalentTo("Data/Crops"))
            {

            }
            if (e.Name.IsEquivalentTo("Data/ObjectInformation"))
            {

            }

            //void loadModeData(AssetRequestedEventArgs e, string filepath)
            //{
            //    e.Edit(asset =>
            //    {
            //        var model = Helper.ModContent.Load<Dictionary<int, string>>(filepath);
            //        var data = asset.AsDictionary<int, string>().Data;
            //        foreach (var (k, v) in model) data[k] = v;
            //    });
            //}
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // add some config options
            //configMenu.AddBoolOption(
            //    mod: this.ModManifest,
            //    name: () => "Example checkbox",
            //    tooltip: () => "An optional description shown as a tooltip to the player.",
            //    getValue: () => this.Config.ExampleCheckbox,
            //    setValue: value => this.Config.ExampleCheckbox = value
            //);
            //configMenu.AddTextOption(
            //    mod: this.ModManifest,
            //    name: () => "Example string",
            //    getValue: () => this.Config.ExampleString,
            //    setValue: value => this.Config.ExampleString = value
            //);
            //tooltip: () => "An optional description shown as a tooltip to the player.",

            //configMenu.AddTextOption(
            //    mod: this.ModManifest,
            //    name: () => "Balance Mode",
            //    getValue: () => this.Config.balanceMode,
            //    setValue: value => {
            //        this.Config.balanceMode = value;
            //        balanceMode = value;
            //    },
            //    allowedValues: new string[] { "Realistic", "More Realistic", "Lightweight", "Dynamic" }
            //);
        }
    }
}