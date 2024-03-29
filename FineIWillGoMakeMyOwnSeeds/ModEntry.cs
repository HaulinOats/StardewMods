﻿using System;
using System.Linq;
using GenericModConfigMenu;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using SObject = StardewValley.Object;

namespace DynamicCrops
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
        string[] saplingNames = { "Cherry Sapling", "Apricot Sapling", "Orange Sapling", "Peach Sapling", "Pomegranate Sapling", "Apple Sapling", "Mango Sapling", "Banana Sapling" };
        string[] vanillaCropNames = { "Rice Shoot", "Amaranth Seeds", "Grape Starter", "Hops Starter", "Rare Seed", "Fairy Seeds", "Tulip Bulb", "Jazz Seeds", "Sunflower Seeds", "Coffee Bean", "Poppy Seeds", "Spangle Seeds", "Parsnip Seeds", "Bean Starter", "Cauliflower Seeds", "Potato Seeds", "Garlic Seeds", "Kale Seeds", "Rhubarb Seeds", "Melon Seeds", "Tomato Seeds", "Blueberry Seeds", "Pepper Seeds", "Wheat Seeds", "Radish Seeds", "Red Cabbage Seeds", "Starfruit Seeds", "Corn Seeds", "Eggplant Seeds", "Artichoke Seeds", "Pumpkin Seeds", "Bok Choy Seeds", "Yam Seeds", "Cranberry Seeds", "Beet Seeds", "Ancient Seeds", "Strawberry Seeds", "Cactus Seeds", "Taro Tuber", "Pineapple Seeds" };

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //get values from config
            this.Config = this.Helper.ReadConfig<ModConfig>();
            Monitor.Log($"harder seed maker: {this.Config.harderSeedMaker}", LogLevel.Debug);
            Monitor.Log($"inflate seed prices: {this.Config.inflateSeedPrices}", LogLevel.Debug);

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.Display.MenuChanged += OnMenuChanged;
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (!this.Config.inflateSeedPrices || e.NewMenu is not ShopMenu shopMenu) return;

            Monitor.Log("entering shop menu...", LogLevel.Debug);
            //Raise seed prices except for fruit tree saplings
            foreach (var (item, value) in shopMenu.itemPriceAndStock)
            {
                if (item is SObject { Category: SObject.SeedsCategory } && vanillaCropNames.Contains(item.Name) && !saplingNames.Contains(item.Name))
                {
                    //pseudo-randomize large seed values
                    shopMenu.itemPriceAndStock[item][0] = new Random().Next(20, 50) * 500;
                    Monitor.Log(item.Name, LogLevel.Debug);
                }
                    
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            Helper.GameContent.InvalidateCache("Data/CraftingRecipes");
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (this.Config.harderSeedMaker)
            {
                //Make Seed Maker Harder to Make
                if (e.Name.IsEquivalentTo("Data/CraftingRecipes"))
                {
                    e.Edit(asset =>
                    {
                        var data = asset.AsDictionary<string, string>().Data;
                        var assetArray = data["Seed Maker"].Split('/');
                        //1 prismatic shard, 5 Iridium Bars, 3 Diamonds
                        assetArray[0] = "74 1 337 5 72 3";
                        var newValue = string.Join('/', assetArray);
                        data["Seed Maker"] = newValue;
                    });
                }
            }
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

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Inflate Shop Seed Prices",
                tooltip: () => "If checked, shop seed prices will become unprofitable",
                getValue: () => this.Config.inflateSeedPrices,
                setValue: value => this.Config.inflateSeedPrices = value
            );

            configMenu.AddBoolOption(
              mod: this.ModManifest,
              name: () => "Harder Seed Maker Recipe",
              tooltip: () => "If checked, seed maker will require rarer materials",
              getValue: () => this.Config.harderSeedMaker,
              setValue: value => this.Config.harderSeedMaker = value
          );
        }
    }
}
