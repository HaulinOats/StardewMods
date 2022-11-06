using System;
using System.Collections.Generic;
using System.Linq;
using GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MoreLinq;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace DynamicCrops
{
    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        private ModConfig Config;
        private ModData cropsAndObjectData;
        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            //get values from config
            this.Config = this.Helper.ReadConfig<ModConfig>();
            Monitor.Log($"flowers can regrow: {this.Config.flowersCanRegrow}", LogLevel.Debug);
            Monitor.Log($"mod is active: {this.Config.activateDynamicCrops}", LogLevel.Debug);
            Monitor.Log($"only affect base game crops: {this.Config.onlyAffectBaseCrops}", LogLevel.Debug);

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (Config.activateDynamicCrops)
            {
                cropsAndObjectData = this.Helper.Data.ReadJsonFile<ModData>($"data/{Constants.SaveFolderName}.json") ?? null;
                if (cropsAndObjectData == null)
                {
                    Monitor.Log($"cropsAndObjectData null, running utility and creating save-specific json file...", LogLevel.Debug);
                    cropsAndObjectData = initUtility(Config, Monitor, Helper);
                    this.Helper.Data.WriteJsonFile($"data/{Constants.SaveFolderName}.json", cropsAndObjectData);
                    Monitor.Log($"... utility data saved!", LogLevel.Debug);
                }
                Helper.GameContent.InvalidateCache("Data/Crops");
                Helper.GameContent.InvalidateCache("Data/ObjectInformation");
                Helper.GameContent.InvalidateCache("TileSheets/crops");
            }
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (Context.IsWorldReady && Config.activateDynamicCrops && cropsAndObjectData != null)
            {
                if (e.Name.IsEquivalentTo("Data/Crops"))
                {
                    Monitor.Log("loading crop data...", LogLevel.Debug);
                    e.Edit(asset =>
                    {
                        var data = asset.AsDictionary<int, string>().Data;
                        foreach (var (item, value) in cropsAndObjectData.CropData)
                        {
                            Monitor.Log($"{item}:{value}", LogLevel.Debug);
                            data[item] = value;
                        }
                    }, AssetEditPriority.Late);
                    Monitor.Log("crop data loaded", LogLevel.Debug);
                }
                if (e.Name.IsEquivalentTo("Data/ObjectInformation"))
                {
                    Monitor.Log("loading object data...", LogLevel.Debug);
                    e.Edit(asset =>
                    {
                        var data = asset.AsDictionary<int, string>().Data;
                        foreach (var (item, value) in cropsAndObjectData.ObjectData)
                        {
                            Monitor.Log($"{item}:{value}", LogLevel.Debug);
                            data[item] = value;
                        }
                    }, AssetEditPriority.Late);
                    Monitor.Log("object data loaded", LogLevel.Debug);
                }
                if (e.Name.IsEquivalentTo("TileSheets/crops"))
                {
                    e.Edit(edit => {
                        Texture2D sourceTexture = Helper.ModContent.Load<Texture2D>("assets/Crops.png");
                        var targetTexture = edit.AsImage();
                        targetTexture.PatchImage(sourceTexture);
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
                name: () => "Activate Dynamic Crops mod",
                tooltip: () => "If unchecked, prevents mod from dynamically generating crops (i.e. turns off mod)",
                getValue: () => this.Config.activateDynamicCrops,
                setValue: value => this.Config.activateDynamicCrops = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Allow flowers to regrow",
                tooltip: () => "If checked, flowers can possibly be given the ability to regrow",
                getValue: () => this.Config.flowersCanRegrow,
                setValue: value => this.Config.flowersCanRegrow = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "Mod only affects base game crops",
                tooltip: () => "If checked, prevents mod from affecting values of crops/seeds added from other mods",
                getValue: () => this.Config.onlyAffectBaseCrops,
                setValue: value => this.Config.onlyAffectBaseCrops = value
            );
        }

        public static ModData initUtility(ModConfig config, StardewModdingAPI.IMonitor Monitor, IModHelper helper)
        {
            var cropData = new Dictionary<int, string>();
            var objectData = new Dictionary<int, string>();
            var flowerSeedIndexes = new int[] { 425, 427, 429, 453, 455, 431 };
            var vanillaCropIndexes = new int[] { 273, 299, 301, 302, 347, 425, 427, 429, 431, 433, 453, 455, 472, 473, 474, 475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 491, 492, 493, 494, 499, 745, 802, 831, 833 };
            var seasonCrops = new Dictionary<string, List<int>>
            {
                { "spring", new List<int>() },
                { "summer", new List<int>() },
                { "fall", new List<int>() },
                { "winter", new List<int>() }
            };

            //default values for crop growth ranges
            var growthRangeShortMin = 5;
            var growthRangeShortMax = 8;
            var growthRangeMediumMin = 9;
            var growthRangeMediumMax = 14;
            var growthRangeLongMin = 15;
            var growthRangeLongMax = 25;

            //crop seed and price range gold per day multipliers
            var regularSeedGPDMultiplierMin = 2;
            var regularSeedGPDMultiplierMax = 5;
            var regularCropGPDMultiplierMin = 10;
            var regularCropGPDMultiplierMax = 15;
            var regrowSeedGPDMultiplierMin = 4;
            var regrowSeedGPDMultiplierMax = 7;
            var regrowCropGPDMultiplierMin = 8;
            var regrowCropGPDMultiplierMax = 11;
            var mediumSellPriceMultiplier = 1.1;
            var longSellPriceMultiplier = 1.2;

            Monitor.Log($"Growth range (Short): {growthRangeShortMin} - {growthRangeShortMax}", LogLevel.Debug);
            Monitor.Log($"Growth range (Medium): {growthRangeMediumMin} - {growthRangeMediumMax}", LogLevel.Debug);
            Monitor.Log($"Growth range (Long): {growthRangeLongMin} - {growthRangeLongMax}", LogLevel.Debug);

            //get crop and seed/crop related objects for modification
            string[] categoryStrings = { "Basic", "Seeds", "Vegetable", "Fruit", "Flower" };
            int[] modifiedArtisanGoodIds = { 303, 346, 395, 614 };

            foreach (var (key, value) in helper.GameContent.Load<Dictionary<int, string>>("Data/Crops"))
            {
                //Monitor.Log($"{key}:{value}", LogLevel.Debug);
                if(config.onlyAffectBaseCrops)
                {
                    if(key < 931)
                    {
                        cropData.TryAdd(key, value);
                    }
                } else
                {
                    cropData.TryAdd(key, value);
                }
            }
            foreach (var (key, value) in helper.GameContent.Load<Dictionary<int, string>>("Data/ObjectInformation"))
            {
                var objectArr = value.Split('/');
                var categoryArr = objectArr[3].Split(' ');
                //Monitor.Log($"{key}:{value}", LogLevel.Debug);
                //ignore 1720 - Dummy Object
                if (((categoryStrings.Contains(categoryArr[0]) && categoryArr.Length > 1) || modifiedArtisanGoodIds.Contains(key)) && key != 1720)
                {
                    objectData.TryAdd(key, value);
                }
            }

            //separate each crop/seed into seasons
            foreach (var crop in cropData)
            {
                var seasons = crop.Value.Split('/')[1].Split(' ');
                foreach (var season in seasons)
                {
                    seasonCrops[season].Add(crop.Key);
                }
            }

            //loop through each season
            foreach (var (season, seasonValue) in seasonCrops)
            {
                Monitor.Log($"{season.ToUpper()}", LogLevel.Debug);

                var seasonCropPool = new List<int>(seasonCrops[season].Shuffle());
                var totalSeasonCrops = seasonCropPool.Count;

                //set number of crops per season that are allowed to regrow
                var totalRegrowthCropsPercentage = 0.4;
                var totalRegrowthCrops = Math.Ceiling(vanillaCropIndexes.Length * totalRegrowthCropsPercentage);
                Monitor.Log($"total regrowth crops: {vanillaCropIndexes.Length}", LogLevel.Debug);

                //set how many crops per season will fall into short, medium, and long-term harvests
                //medium crop percentage will end up being percentage difference leftover after removing long and short crop percentages from 100%
                var totalShortCropsPercentage = 0.20;
                var totalLongCropsPercentage = 0.20;
                var totalMediumCropsPercentage = 1 - (totalShortCropsPercentage + totalLongCropsPercentage);
                var totalShortCrops = Math.Ceiling(totalSeasonCrops * totalShortCropsPercentage);
                var totalLongCrops = Math.Ceiling(totalSeasonCrops * totalLongCropsPercentage);
                var totalMediumCrops = Math.Ceiling(totalSeasonCrops * totalMediumCropsPercentage);
                var totalCropTypeSum = totalShortCrops + totalMediumCrops + totalLongCrops;

                //get total crops that will be allowed to have extra yields
                var totalExtraYieldCropsPercentage = Helpers.GetRandomIntegerInRange(10, 15);
                var totalExtraYieldCrops = Math.Ceiling((totalSeasonCrops * totalExtraYieldCropsPercentage) * 0.01);
                Monitor.Log($"total extra yields for {season}: {totalExtraYieldCrops}", LogLevel.Debug);

                //if the sum of calculated harvest categories does not equal actual crops in season
                //remove/add difference from medium harvest length crops
                if (totalCropTypeSum > totalSeasonCrops) totalMediumCrops -= totalCropTypeSum - totalSeasonCrops;
                if (totalCropTypeSum < totalSeasonCrops) totalMediumCrops += totalSeasonCrops - totalCropTypeSum;
                Monitor.Log($"short:{totalShortCrops} medium:{totalMediumCrops} long:{totalLongCrops}", LogLevel.Debug);
                Monitor.Log($"total crops for: {totalShortCrops + totalMediumCrops + totalLongCrops}", LogLevel.Debug);

                //loop through each individual season's crop index array
                for (int seasonCropIdx = 0; seasonCropIdx < seasonCropPool.Count; seasonCropIdx++)
                {
                    var seedIdx = seasonCropPool[seasonCropIdx];
                    var objIdx = int.Parse(cropData[seedIdx].Split('/')[3]);
                    Monitor.Log($"seedIdx: {seedIdx}", LogLevel.Debug);
                    Monitor.Log($"objIdx: {objIdx}", LogLevel.Debug);
                    var item = new Dictionary<string, string[]>
                    {
                        { "cropData", cropData[seedIdx].Split('/') },
                        { "seedObjData", objectData[seedIdx].Split('/') },
                        { "cropObjData", objectData[objIdx].Split('/') },
                    };

                    //generate random growth (harvest) times for different crops
                    //manually set Parsnip to be a short-term crop since it's the only crop
                    //you have access to start making money from at the start of new game
                    var totalGrowthTime = Helpers.GetRandomIntegerInRange(growthRangeMediumMin, growthRangeMediumMax);
                    if (totalShortCrops > 0 || seedIdx == 472)
                    {
                        //for all crops that are NOT Parsnip
                        if (seedIdx != 472)
                        {
                            totalGrowthTime = Helpers.GetRandomIntegerInRange(growthRangeShortMin, growthRangeShortMax);
                        }
                        else
                        {
                            //for Parsnip
                            if (totalShortCrops == 0) totalMediumCrops--;
                            totalGrowthTime = 4;
                        }
                        totalShortCrops--;
                    }
                    else if (totalLongCrops > 0)
                    {
                        totalGrowthTime = Helpers.GetRandomIntegerInRange(growthRangeLongMin, growthRangeLongMax);
                        totalLongCrops--;
                    }
                    Monitor.Log($"total growth time: {totalGrowthTime} days", LogLevel.Debug);

                    //dynamically generate growth stages
                    var growthStagesArr = Array.ConvertAll(item["cropData"][0].Split(' '), s => int.Parse(s));
                    var averageGrowthStageDays = totalGrowthTime / growthStagesArr.Length;
                    for (var i = 0; i < growthStagesArr.Length; i++)
                    {
                        growthStagesArr[i] = averageGrowthStageDays;
                    }

                    //if total sum of growth stages is less than total grow time, remove the difference from last stage
                    var growthStagesSum = growthStagesArr.Aggregate((total, next) => total + next);

                    if (growthStagesSum < totalGrowthTime)
                    {
                        growthStagesArr[growthStagesArr.Length - 1] += totalGrowthTime - growthStagesSum;
                    }
                    //if last growth stage is at least 2 days higher than previous day,
                    //distribute excess to previous day
                    var growthDayDiff = growthStagesArr.Last() - growthStagesArr[growthStagesArr.Length - 2];
                    if (growthDayDiff > 1)
                    {
                        growthStagesArr[growthStagesArr.Length - 1]--;
                        growthStagesArr[growthStagesArr.Length - 2]++;
                    }
                    //shuffles array so growth stage positions are randomized
                    var newGrowthStagesArr = Array.ConvertAll(growthStagesArr, i => i.ToString()).Shuffle();
                    item["cropData"][0] = string.Join(' ', newGrowthStagesArr);

                    //set up dynamic description for seeds
                    var tCropSeasons = string.Join(", ", item["cropData"][1].Split(' ').Select((season, idx) => Helpers.Capitalize(season)));
                    var seedDescription = $"Plant these in {Helpers.ReplaceLastOccurrence(tCropSeasons, ", ", " or ")}. ";
                    var daysString = totalGrowthTime < 2 ? "day" : "days";
                    seedDescription += $"Takes {totalGrowthTime} {daysString} to mature";

                    //if crop is regrowth capable or on a trellis
                    var isTrellisCrop = Convert.ToBoolean(Helpers.Capitalize(item["cropData"][7]));
                    var isFlower = flowerSeedIndexes.Contains(seedIdx);
                    Monitor.Log($"is flower: {config.flowersCanRegrow}", LogLevel.Debug);

                    //if more crops can be given regrowth capability and aren't long-term crops, or is a trellis crop , apply regrowth values
                    if ((totalRegrowthCrops > 0 && totalGrowthTime < growthRangeLongMin) && vanillaCropIndexes.Contains(seedIdx) || isTrellisCrop) applyRegrowValues();
                    else applyRegularValues();

                    //store updated description
                    item["seedObjData"][5] = seedDescription;
                    Monitor.Log($"seed description: {seedDescription}", LogLevel.Debug);

                    // if crop is not allowed to have extra chance for multiple harvesting
                    // set extra yields field to false
                    if (totalExtraYieldCrops <= 0)
                    {
                        item["cropData"][6] = "false";
                    }
                    else
                    {
                        //balance out values to allow low-price crops to get extra yields, and higher-priced ones to not get them at all
                        var cropSellPrice = int.Parse(item["cropObjData"][1]);
                        var maxAllowedHarvest = 1;
                        var extraYieldChancePercentageMax = 5;
                        if (cropSellPrice <= 50)
                        {
                            maxAllowedHarvest = 3;
                            extraYieldChancePercentageMax = 24;
                        }
                        else if (cropSellPrice > 50 && cropSellPrice <= 125)
                        {
                            maxAllowedHarvest = 2;
                            extraYieldChancePercentageMax = 16;
                        }
                        else if (cropSellPrice > 125 && cropSellPrice <= 150)
                        {
                            maxAllowedHarvest = 2;
                            extraYieldChancePercentageMax = 8;
                        }
                        var minYieldHarvest = Helpers.GetRandomIntegerInRange(1, maxAllowedHarvest);
                        var maxYieldHarvest = Helpers.GetRandomIntegerInRange(minYieldHarvest, maxAllowedHarvest);
                        var chanceForExtraCrops = Helpers.GetRandomIntegerInRange(2, extraYieldChancePercentageMax) * 0.01;
                        item["cropData"][6] = $"true {minYieldHarvest} {maxYieldHarvest} 0 {chanceForExtraCrops}";

                        //reduce crop sell price due to extra yield chance
                        item["cropObjData"][1] = Math.Ceiling(int.Parse(item["cropObjData"][1]) * (1 - (chanceForExtraCrops * (minYieldHarvest - 1)))).ToString();

                        Monitor.Log($"** EXTRA YIELD **", LogLevel.Debug);
                        Monitor.Log($"{item["cropData"][6]}", LogLevel.Debug);
                        Monitor.Log($"updated crop sell price: {item["cropObjData"][1]}", LogLevel.Debug);
                        totalExtraYieldCrops--;
                    }

                    //Update artisan good prices to match new crop/seed prices if price of artisan good not dependent on crop price values (coffee, pale ale, etc)
                    //Hops Seeds
                    if (seedIdx == 302)
                    {
                        //303 -> Pale Ale
                        setArtisanGoodPrice(303);
                    }
                    //Wheat Seeds
                    if (seedIdx == 483)
                    {
                        //346 -> Beer
                        setArtisanGoodPrice(346);
                    }
                    //Coffee Bean
                    if (seedIdx == 433)
                    {
                        //update coffee to regrow and update price to match extra yields
                        var regrowthPercentage = Helpers.GetRandomIntegerInRange(30, 50);
                        item["cropData"][4] = Math.Ceiling(totalGrowthTime * (regrowthPercentage * 0.01)).ToString();
                        item["cropData"][6] = "true 4 4 0 .02";
                        item["cropObjData"][1] = (int.Parse(item["cropObjData"][1]) / 4).ToString();


                        //395 -> Coffee
                        setArtisanGoodPrice(395);
                    }
                    //Tea Sapling
                    if (seedIdx == 251)
                    {
                        //614 -> Green Tea
                        setArtisanGoodPrice(614);
                    }

                    //join arrays and update crop and object data
                    cropData[seedIdx] = string.Join('/', item["cropData"]);
                    objectData[seedIdx] = string.Join('/', item["seedObjData"]);
                    objectData[objIdx] = string.Join('/', item["cropObjData"]);
                    Monitor.Log("----------------------", LogLevel.Debug);

                    //calculation helper functions
                    void applyRegrowValues()
                    {
                        //if flower but flowers aren't allowed to regrow, exit regrowth function and apply regular values to flower instead
                        if (isFlower && !config.flowersCanRegrow)
                        {
                            applyRegularValues();
                            return;
                        }

                        //if more crops are allowed to be given regrowth capabilities, set regrowth time to be between 30% - 50% of total grow time.
                        var regrowthPercentage = Helpers.GetRandomIntegerInRange(30, 50);
                        var regrowthTime = Math.Ceiling(totalGrowthTime * (regrowthPercentage * 0.01));
                        item["cropData"][4] = regrowthTime.ToString();
                        Monitor.Log($"regrowth: {regrowthTime} days", LogLevel.Debug);

                        //set crop and seed sell prices
                        var seedPriceMultiplier = Helpers.GetRandomIntegerInRange(regrowSeedGPDMultiplierMin, regrowSeedGPDMultiplierMax);
                        var cropPriceMultiplier = Helpers.GetRandomIntegerInRange(regrowCropGPDMultiplierMin, regrowCropGPDMultiplierMax);
                        item["seedObjData"][1] = getSellPrice(seedPriceMultiplier, false);
                        item["cropObjData"][1] = getSellPrice(cropPriceMultiplier, true);
                        Monitor.Log($"seed price multiplier: {seedPriceMultiplier}", LogLevel.Debug);
                        Monitor.Log($"crop price multiplier: {cropPriceMultiplier}", LogLevel.Debug);
                        Monitor.Log($"seed purchase price: {item["seedObjData"][1]}g", LogLevel.Debug);
                        Monitor.Log($"crop sell price    : {item["cropObjData"][1]}g", LogLevel.Debug);
                        // (total growth time x seed price multiplier):

                        //append season text with regrowth verbiage
                        seedDescription += ", but keeps producing after that." + (isTrellisCrop ? " Grows on a trellis." : "");
                        totalRegrowthCrops--;
                    }

                    void applyRegularValues()
                    {
                        //if crop is NOT regrowth capable, set crop to not regrow
                        item["cropData"][4] = "-1";
                        Monitor.Log($"no regrowth", LogLevel.Debug);

                        //set crop and seed sell prices
                        var seedPriceMultiplier = Helpers.GetRandomIntegerInRange(regularSeedGPDMultiplierMin, regularSeedGPDMultiplierMax);
                        var cropPriceMultiplier = Helpers.GetRandomIntegerInRange(regularCropGPDMultiplierMin, regularCropGPDMultiplierMax);
                        item["seedObjData"][1] = getSellPrice(seedPriceMultiplier, false);
                        item["cropObjData"][1] = getSellPrice(cropPriceMultiplier, true);
                        Monitor.Log($"seed price multiplier: {seedPriceMultiplier}", LogLevel.Debug);
                        Monitor.Log($"crop price multiplier: {cropPriceMultiplier}", LogLevel.Debug);
                        Monitor.Log($"seed purchase price: {item["seedObjData"][1]}g", LogLevel.Debug);
                        Monitor.Log($"crop sell price    : {item["cropObjData"][1]}g", LogLevel.Debug);

                        //append period to season text
                        seedDescription += '.';
                    }

                    void setArtisanGoodPrice(int objId)
                    {
                        var artisanObjArr = objectData[objId].Split('/');
                        artisanObjArr[1] = (int.Parse(item["cropObjData"][1]) * (int)(Helpers.GetRandomIntegerInRange(115, 140) * 0.01)).ToString();
                        objectData[objId] = string.Join('/', artisanObjArr);
                    }

                    string getSellPrice(double priceMultiplier, bool isCrop)
                    {
                        var sellPrice = totalGrowthTime * priceMultiplier;

                        //if handling crop sell price, provide incentive for investing in longer term crops by applying an additional multiplier
                        if (isCrop)
                        {
                            if (totalGrowthTime > growthRangeShortMax && totalGrowthTime < growthRangeLongMin)
                            {
                                sellPrice *= mediumSellPriceMultiplier;
                            }
                            else if (totalGrowthTime > growthRangeMediumMax)
                            {
                                sellPrice *= longSellPriceMultiplier;
                            }
                        }

                        return Convert.ToInt32(sellPrice).ToString();
                    }
                }
            }
            return new ModData { CropData = cropData, ObjectData = objectData };
        }
    }
}
