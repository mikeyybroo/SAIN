﻿using EFT;
using Newtonsoft.Json;
using SAIN.Attributes;
using SAIN.Helpers;
using SAIN.Preset.GlobalSettings.Categories;
using System.Collections.Generic;

namespace SAIN.Preset.GlobalSettings
{
    public class GeneralSettings
    {
        [Name("Global Difficulty Modifier")]
        [Description("Higher number = harder bots. Affects bot accuracy, recoil, fire-rate, full auto burst lenght, scatter, reaction-time")]
        [Default(1f)]
        [MinMax(0.1f, 5f, 100f)]
        public float GlobalDifficultyModifier = 1f;

        [Name("Bot Grenades")]
        [Default(true)]
        public bool BotsUseGrenades = true;

        [Name("HeadShot Protection")]
        [Description("Experimental, will move bot's aiming target if it ends up on the player's head. NOT FOOLPROOF. It's more of a strong suggestion rather than a hard limit. If you find you are dying to headshots too frequently still, I recommend increasing your head health with another mod.")]
        [Default(false)]
        public bool HeadShotProtection = false;

        [Name("Bot Reaction and Accuracy Changes Toggle - Experimental")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("Experimental: Bots will have slightly reduced accuracy and vision speed if you are not looking in their direction. " +
            "So if a bot notices and starts shooting you while your back is turned, they will be less accurate and notice you more slowly.")]
        [Default(true)]
        public bool NotLookingToggle = true;

        [Name("Bot Reaction and Accuracy Changes Time Limit")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("The Maximum Time that a bot can be shooting at you before the reduced spread not longer has an affect. " +
            "So if a bot is shooting at you from the back for X seconds, after that time it will no longer reduce their accuracy to give you a better chance to react.")]
        [Default(4f)]
        [MinMax(0.5f, 20f, 100f)]
        public float NotLookingTimeLimit = 4f;

        [Name("Bot Reaction and Accuracy Changes Angle")]
        [Section("Unseen Bot")]
        [Experimental]
        [Advanced]
        [Description("The Maximum Angle for the player to be considered looking at a bot.")]
        [Default(15f)]
        [MinMax(5f, 45f, 1f)]
        public float NotLookingAngle = 15f;

        [Name("Bot Reaction Multiplier When Out of Sight")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("How much to multiply bot vision speed by if you aren't looking at them when they notice you. Higher = More time before reacting.")]
        [Default(1.33f)]
        [MinMax(1.1f, 5f, 100f)]
        public float NotLookingVisionSpeedModifier = 1.33f;

        [Name("Bot Accuracy and Spread Increase When Out of Sight")]
        [Section("Unseen Bot")]
        [Experimental]
        [Description("How much additional random Spread to add to a bot's aim if the player isn't look at them." +
            " 1 means it will randomize in a 1 meter sphere around their original aim target in addition to existing random spread." +
            " Higher = More spread and less accurate bots.")]
        [Default(0.33f)]
        [MinMax(0.1f, 1.5f, 100f)]
        public float NotLookingAccuracyAmount = 0.33f;

        [Name("Disable Talking Patches")]
        [Description("Disable all SAIN based handling of bot talking. No more squad chatter, no more quiet bots, completely disables SAIN's handling of bot voices")]
        [Default(false)]
        public bool DisableBotTalkPatching = false;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [Default(24)]
        [MinMax(0, 100)]
        [Hidden]
        public int SAINCombatSquadLayerPriority = 24;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [Default(22)]
        [MinMax(0, 100)]
        [Hidden]
        public int SAINExtractLayerPriority = 22;

        [Description("Requires Restart. Dont touch unless you know what this is")]
        [Advanced]
        [Default(20)]
        [MinMax(0, 100)]
        [Hidden]
        public int SAINCombatSoloLayerPriority = 20;
    }
}