﻿using EFT;
using UnityEngine.UIElements;
using System;
using System.Linq;
using EFT.Interactive;
using Comfort.Common;

namespace SAIN.Layers
{
    internal class ExtractLayer : SAINLayer
    {
        public static readonly string Name = BuildLayerName<ExtractLayer>();

        public ExtractLayer(BotOwner bot, int priority) : base(bot, priority, Name)
        {
        }

        public override Action GetNextAction()
        {
            return new Action(typeof(ExtractAction), "Extract");
        }

        public override bool IsActive()
        {
            if (SAIN == null) return false;
            //if (SAIN.SAINEnabled == false) return false;

            if (!SAIN.Info.FileSettings.Mind.EnableExtracts || !SAIN.Info.GlobalSettings.Extract.EnableExtractsGlobal)
            {
                return false;
            }

            if (!Components.BotController.BotExtractManager.IsBotAllowedToExfil(SAIN))
            {
                return false;
            }

            if (!ExtractFromTime() && !ExtractFromInjury() && !ExtractFromLoot() && !ExtractFromExternal())
            {
                return false;
            }

            if (SAIN.Memory.ExfilPosition == null)
            {
                BotController.BotExtractManager.TryFindExfilForBot(SAIN);
                return false;
            }

            // If the bot can no longer use its selected extract and isn't already in the extract area, select another one. This typically happens if
            // the bot selects a VEX but the car leaves before the bot reaches it.
            if (!BotController.BotExtractManager.CanBotsUseExtract(SAIN.Memory.ExfilPoint) && !IsInExtractArea())
            {
                SAIN.Memory.ExfilPoint = null;
                SAIN.Memory.ExfilPosition = null;

                return false;
            }

            return true;
        }

        private bool IsInExtractArea()
        {
            float distance = (BotOwner.Position - SAIN.Memory.ExfilPosition.Value).sqrMagnitude;
            return distance < ExtractAction.MinDistanceToStartExtract;
        }

        private bool ExtractFromTime()
        {
            float percentageLeft = BotController.BotExtractManager.PercentageRemaining;
            if (percentageLeft <= SAIN.Info.PercentageBeforeExtract)
            {
                if (!Logged)
                {
                    Logged = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract with [{percentageLeft}] of the raid remaining.");
                }
                if (SAIN.Enemy == null)
                {
                    return true;
                }
                else if (BotController.BotExtractManager.TimeRemaining < 120)
                {
                    return true;
                }
            }
            return false;
        }

        private bool ExtractFromInjury()
        {
            if (SAIN.Memory.Dying && !BotOwner.Medecine.FirstAid.HaveSmth2Use)
            {
                if (!Logged)
                {
                    Logged = true;
                    Logger.LogInfo($"[{BotOwner.name}] Is Moving to Extract because of heavy injury and lack of healing items.");
                }
                if (SAIN.Enemy == null)
                {
                    return true;
                }
                else if (SAIN.Enemy.TimeSinceSeen > 30f)
                {
                    return true;
                }
            }
            return false;
        }

        // Looting Bots Integration
        private bool ExtractFromLoot()
        {
            // If extract from loot is disabled, or no Looting Bots interop, not active
            if (SAINPlugin.LoadedPreset.GlobalSettings.LootingBots.ExtractFromLoot == false  || !LootingBots.LootingBotsInterop.Init())
            {
                return false;
            }

            // No integration setup yet, set it up
            if (SAINLootingBotsIntegration == null)
            {
                SAINLootingBotsIntegration = new SAINLootingBotsIntegration(BotOwner, SAIN);
            }

            SAINLootingBotsIntegration?.Update();
            return FullOnLoot && HasActiveThreat() == false;
        }

        private bool FullOnLoot => SAINLootingBotsIntegration?.FullOnLoot == true;

        private SAINLootingBotsIntegration SAINLootingBotsIntegration;

        private bool HasActiveThreat()
        {
            if (SAIN.Enemy == null)
            {
                return false;
            }
            else if (SAIN.Enemy.TimeSinceSeen > 30f)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool ExtractFromExternal()
        {
            return SAIN.Info.ForceExtract;
        }

        private bool Logged = false;

        public override bool IsCurrentActionEnding()
        {
            return false;
        }
    }
}