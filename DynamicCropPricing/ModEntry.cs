using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace DynamicCrops
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.SaveCreated += OnSaveCreation;
            helper.Events.GameLoop.Saving += OnSave;
        }

        private void OnSaveCreation(object sender, SaveCreatedEventArgs e)
        {
            Monitor.Log($"{Game1.player.Name}'s game has loaded...", LogLevel.Debug);

            var objectModel = this.Helper.Data.ReadSaveData<ModData>("RFObjectData") ?? new ModData();
            objectModel.CropData.Add("299", "5 5 5 5/summer/39/300/-1/1/false/false/false");
            this.Helper.Data.WriteSaveData("RFObjectData", objectModel);

            Monitor.Log(objectModel.ToString(), LogLevel.Debug);
            Monitor.Log("... DynamicCrops save file data created", LogLevel.Debug);
        }

        private void OnSave(object sender, SavingEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void GetModeData(string mode)
        {
            var cropAndObjectData = ModData.getRealisticData();
               
            switch (mode.ToLower())
            {
                case "even more realistic":
                    cropAndObjectData = ModData.getEvenMoreRealisticData();
                    break;
                case "dynamic":
                    cropAndObjectData = ModData.getLightweightData();
                    break;
            }
        }
    }
}