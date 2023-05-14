﻿using BepInEx.Configuration;

namespace SAIN.UserSettings
{
    internal class DebugConfig
    {
        public static ConfigEntry<bool> DrawVerticesDebug { get; private set; }
        public static ConfigEntry<bool> ToggleDrawCoverPoints { get; private set; }
        public static ConfigEntry<bool> ToggleDrawInputPoints { get; private set; }
        public static ConfigEntry<bool> DebugDynamicLean { get; private set; }
        public static ConfigEntry<bool> DebugLayers { get; private set; }
        public static ConfigEntry<bool> DebugLayersDraw { get; private set; }
        public static ConfigEntry<bool> DebugBotDecisions { get; private set; }
        public static ConfigEntry<bool> DebugUpdateMove { get; private set; }
        public static ConfigEntry<bool> DebugUpdateShoot { get; private set; }
        public static ConfigEntry<bool> DebugUpdateSteering { get; private set; }
        public static ConfigEntry<bool> DebugUpdateTargetting { get; private set; }
        public static ConfigEntry<bool> DebugCoverSystem { get; private set; }
        public static ConfigEntry<bool> DebugCornerProcessing { get; private set; }
        public static ConfigEntry<bool> DebugCornerLeanAngle { get; private set; }
        public static ConfigEntry<bool> DebugCoverComponent { get; private set; }

        public static void Init(ConfigFile Config)
        {
            string debugmode = "7. Debug Mode";

            DrawVerticesDebug = Config.Bind(debugmode, "DrawVerticesDebug", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 99 }));

            ToggleDrawCoverPoints = Config.Bind(debugmode, "ToggleDrawCoverPoints", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 99 }));

            ToggleDrawInputPoints = Config.Bind(debugmode, "ToggleDrawInputPoints", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 98 }));

            DebugDynamicLean = Config.Bind(debugmode, "Dynamic Lean", false,
                new ConfigDescription("Draws debug Lines and logs events",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 8 }));

            DebugLayers = Config.Bind(debugmode, "DogFight Layer", false, 
                new ConfigDescription("Draws debug Lines and logs events", 
                null, 
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 7 }));

            DebugLayersDraw = Config.Bind(debugmode, "DogFight Layer Draw Generated Points", false,
                new ConfigDescription("Draws points everywhere",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 6 }));

            DebugBotDecisions = Config.Bind(debugmode, "DebugBotDecisions", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 5 }));

            DebugUpdateMove = Config.Bind(debugmode, "DebugUpdateMove", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 4 }));

            DebugUpdateShoot = Config.Bind(debugmode, "DebugUpdateShoot", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 3 }));

            DebugUpdateSteering = Config.Bind(debugmode, "DebugUpdateSteering", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 2 }));

            DebugUpdateTargetting = Config.Bind(debugmode, "DebugUpdateTargetting", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 1 }));

            DebugCoverSystem = Config.Bind(debugmode, "DebugCoverSystem", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = 0 }));

            DebugCornerProcessing = Config.Bind(debugmode, "DebugCorners", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = -1 }));

            DebugCornerLeanAngle = Config.Bind(debugmode, "DebugCornerLeanAngle", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = -1 }));

            DebugCoverComponent = Config.Bind(debugmode, "DebugCoverComponent", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = true, Order = -1 }));
        }
    }
}