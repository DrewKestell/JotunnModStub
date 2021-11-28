using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace PlutoMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class PlutoMod : BaseUnityPlugin
    {
        private AssetBundle steelIngotBundle;

        public const string PluginGUID = "com.jotunn.plutomod";
        public const string PluginName = "PlutoMod";
        public const string PluginVersion = "0.0.1";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            LoadAssets();

            // Jotunn comes with MonoMod Detours enabled for hooking Valheim's code
            // https://github.com/MonoMod/MonoMod
            On.FejdStartup.Awake += FejdStartup_Awake;

            // To learn more about Jotunn's features, go to
            // https://valheim-modding.github.io/Jotunn/tutorials/overview.html

            SkillManager.Instance.AddSkillsFromJson(@"C:\Users\Drew\Repos\PlutoMod\PlutoMod\Assets\customSkills.json");
        }

        private void FejdStartup_Awake(On.FejdStartup.orig_Awake orig, FejdStartup self)
        {
            // This code runs before Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup is going to awake");

            // Call this method so the original game method is invoked
            orig(self);

            // This code runs after Valheim's FejdStartup.Awake
            Jotunn.Logger.LogInfo("FejdStartup has awoken");

            Jotunn.Logger.LogInfo("PlutoMod startup");
        }

        private void Update()
        {
            //var treasureHuntingSkill = SkillManager.Instance.GetSkill("com.jotunn.plutomod.treasurehunting");

            //Player.m_localPlayer.RaiseSkill(treasureHuntingSkill.m_skill, 1);
        }

        private void LoadAssets()
        {
            // Load asset bundle from embedded resources
            Jotunn.Logger.LogInfo($"Embedded resources: {string.Join(",", typeof(PlutoMod).Assembly.GetManifestResourceNames())}");
            steelIngotBundle = AssetUtils.LoadAssetBundleFromResources("steel", typeof(PlutoMod).Assembly);

            var steelPrefab = steelIngotBundle.LoadAsset<GameObject>("Steel");
            var steel = new CustomItem(steelPrefab, fixReference: false,
                new ItemConfig
                {
                    Amount = 1
                });
            ItemManager.Instance.AddItem(steel);
        }
    }
}