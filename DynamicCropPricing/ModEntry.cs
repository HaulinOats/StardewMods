using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace DynamicCrops
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod, IAssetLoader
    {
        public string balanceMode = "realistic";
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveCreated += OnSaveCreation;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;

        }

        private void OnSaveCreation(object sender, SaveCreatedEventArgs e)
        {
            Monitor.Log($"{Game1.player.Name}'s game has loaded...", LogLevel.Debug);

            Monitor.Log("... DynamicCrops save file data created", LogLevel.Debug);
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            Monitor.Log($"{balanceMode} mode data loading...", LogLevel.Debug);
            if (e.Name.IsEquivalentTo("TileSheets/crops"))
            {
                Monitor.Log("accesing crop tilesheet...");
                e.LoadFromModFile<Texture2D>("assets/crops.png", AssetLoadPriority.Medium);
                Monitor.Log("crop spritesheet loaded");
            }
            if (e.Name.IsEquivalentTo("Data/Crops"))
            {
                Monitor.Log("accessing crop data...", LogLevel.Debug);
                switch (balanceMode)
                {
                    case "realistic":
                        loadModeData(e, "assets/realistic-crop-data.json");
                        break;
                    case "moreRealistic":
                        loadModeData(e, "assets/more-realistic-crop-data.json");
                        break;
                }
                Monitor.Log("crop data loaded!", LogLevel.Debug);
            }
            if (e.Name.IsEquivalentTo("Data/ObjectInformation"))
            {
                Monitor.Log("accessing object data...", LogLevel.Debug);
                switch (balanceMode)
                {
                    case "realistic":
                        loadModeData(e, "assets/realistic-object-data.json");
                        break;
                    case "moreRealistic":
                        loadModeData(e, "assets/more-realistic-object-data.json");
                        break;
                    case "lightweight":
                        loadModeData(e, "assets/lightweight-object-data.json");
                        break;
                }
                Monitor.Log("object data loaded!", LogLevel.Debug);
            }

            void loadModeData(AssetRequestedEventArgs e, string filepath)
            {
                e.Edit(asset =>
                {
                    var model = Helper.ModContent.Load<Dictionary<int, string>>("assets/realistic-object-data.json");
                    foreach (var (key, value) in model) model[key] = value;
                });
            }
        }
    }
}