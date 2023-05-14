﻿using BepInEx.Configuration;

namespace SAIN.UserSettings
{
    internal class CoverSystemConfig
    {
        public static ConfigEntry<bool> ResetGenerator { get; private set; }
        public static ConfigEntry<bool> TogglePointGenerator { get; private set; }
        public static ConfigEntry<bool> SaveCoverPoints { get; private set; }
        public static ConfigEntry<bool> StartFiltering { get; private set; }
        public static ConfigEntry<int> RayCastAccuracy { get; private set; }
        public static ConfigEntry<int> CoverPointBatchSize { get; private set; }
        public static ConfigEntry<int> MaxGeneratorBatchSize { get; private set; }
        public static ConfigEntry<int> MaxVerificationBatch { get; private set; }
        public static ConfigEntry<int> MaxRandomGenIterations { get; private set; }
        public static ConfigEntry<int> CheckVertBatchSize { get; private set; }
        public static ConfigEntry<float> FilterDistance { get; private set; }
        public static ConfigEntry<float> NavMeshSampleRange { get; private set; }

        public static void Init(ConfigFile Config)
        {
            string coversystem = "1. Cover System";

            ResetGenerator = Config.Bind(coversystem, "ResetGenerator", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 499 }));

            SaveCoverPoints = Config.Bind(coversystem, "Save Cover Points", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 399 }));

            StartFiltering = Config.Bind(coversystem, "StartFiltering", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 299 }));

            RayCastAccuracy = Config.Bind(coversystem, "RayCastAccuracy", 2,
                new ConfigDescription("",
                new AcceptableValueRange<int>(0, 5),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 10 }));

            CoverPointBatchSize = Config.Bind(coversystem, "CoverPointBatchSize", 10,
                new ConfigDescription("",
                new AcceptableValueRange<int>(3, 100),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 9 }));

            string pointGenerator = "2. Point Generator";

            TogglePointGenerator = Config.Bind(pointGenerator, "Toggle Point Generator", false,
                new ConfigDescription("Start the lag machine",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 999 }));

            ResetGenerator = Config.Bind(pointGenerator, "ResetGenerator", false,
                new ConfigDescription("",
                null,
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 499 }));

            MaxGeneratorBatchSize = Config.Bind(pointGenerator, "Max Generator Batch Size", 10,
                new ConfigDescription("",
                new AcceptableValueRange<int>(1, 200),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 8 }));

            MaxVerificationBatch = Config.Bind(pointGenerator, "Max Verification Batch", 200,
                new ConfigDescription("",
                new AcceptableValueRange<int>(10, 500),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 7 }));

            MaxRandomGenIterations = Config.Bind(pointGenerator, "Max Random Gen Iterations", 100,
                new ConfigDescription("",
                new AcceptableValueRange<int>(10, 200),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 6 }));

            CheckVertBatchSize = Config.Bind(pointGenerator, "Check Vertices Batch Size", 200,
                new ConfigDescription("",
                new AcceptableValueRange<int>(5, 1000),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 4 }));

            FilterDistance = Config.Bind(pointGenerator, "Filter Min Distance", 0.25f,
                new ConfigDescription("",
                new AcceptableValueRange<float>(0.1f, 1f),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));

            NavMeshSampleRange = Config.Bind(pointGenerator, "NavMeshSampleRange", 1f,
                new ConfigDescription("",
                new AcceptableValueRange<float>(0.25f, 5f),
                new ConfigurationManagerAttributes { IsAdvanced = false, Order = 2 }));
        }
    }
}