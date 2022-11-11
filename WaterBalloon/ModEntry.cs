

using System.IO;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace WaterBalloon
{
    public interface IJsonAssetsApi
    {
        int GetObjectId(string name);
        void LoadAssets(string path);
    }
    /// <summary>The mod entry point.</summary>
    public class ModEntry : StardewModdingAPI.Mod
    {
        private IJsonAssetsApi JsonAssets;
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            //helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            JsonAssets = Helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");
            if (JsonAssets == null)
            {
                Monitor.Log("Can't load Json Assets API, which is needed for test mod to function", LogLevel.Debug);
            }
            else
            {
                JsonAssets.LoadAssets(Path.Combine(Helper.DirectoryPath, "assets"));
                Monitor.Log("JSON assets loaded", LogLevel.Debug);
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            Monitor.Log("save file loaded for Water Balloon mod", LogLevel.Debug);
            if (JsonAssets != null)
            {
                int testID = JsonAssets.GetObjectId("Water Balloon");

                if (testID == -1)
                {
                    Monitor.Log("Can't get ID for Test item. Some functionality will be lost.", LogLevel.Debug);
                }
                else
                {
                    Monitor.Log($"Test item ID is {testID}.", LogLevel.Debug);
                }
            }
        }

        //private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        //{
        //    if (e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA)
        //    {
        //        // The normal interact button was pressed.
        //        if (Game1.player.CurrentItem != null)
        //        {
        //            if (Game1.player.CurrentItem.Name.Equals(""))
        //            {
        //                // Do thing.
        //            }
        //        }
        //    }
        //}
    }
}
