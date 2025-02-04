﻿using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using UnityEngine.AI;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class RunToCoverAction : SAINAction
    {
        public RunToCoverAction(BotOwner bot) : base(bot, nameof(RunToCoverAction))
        {
        }

        public override void Update()
        {
            SAIN.Mover.SetTargetMoveSpeed(1f);
            SAIN.Mover.SetTargetPose(1f);

            if (RecalcTimer < Time.time)
            {
                MoveSuccess = false;
                bool shallProne = SAIN.Mover.Prone.ShallProneHide();
                if (FindTargetCover())
                {
                    RecalcTimer = Time.time + 2f;
                    if ((CoverDestination.Position - BotOwner.Position).sqrMagnitude > 4f)
                    {
                        MoveSuccess = BotOwner.BotRun.Run(CoverDestination.Position, false, 0.6f);
                    }
                    else
                    {
                        bool shallCrawl = SAIN.Decision.CurrentSelfDecision != SelfDecision.None && CoverDestination.CoverStatus == CoverStatus.FarFromCover && shallProne;
                        MoveSuccess = SAIN.Mover.GoToPoint(CoverDestination.Position, out bool calculating, -1, shallCrawl);
                    }
                }
                if (MoveSuccess)
                {
                    RecalcTimer = Time.time + 4f;
                }
                else
                {
                    RecalcTimer = Time.time + 0.1f;
                }
            }
            if (MoveSuccess)
            {
                SAIN.Steering.LookToMovingDirection();
            }
            else
            {
                EngageEnemy();
            }
        }

        private bool MoveSuccess;

        private float RecalcTimer;

        private bool FindTargetCover()
        {
            if (CoverDestination != null)
            {
                CoverDestination.BotIsUsingThis = false;
                CoverDestination = null;
            }

            CoverPoint coverPoint = SelectPoint();
            if (coverPoint != null && !coverPoint.Spotted)
            {
                if (SAIN.Mover.CanGoToPoint(coverPoint.Position, out Vector3 pointToGo, true, 1f))
                {
                    //coverPoint.Position = pointToGo;
                    coverPoint.BotIsUsingThis = true;
                    CoverDestination = coverPoint;
                    return true;
                }
            }
            return false;
        }

        private CoverPoint SelectPoint()
        {
            CoverPoint fallback = SAIN.Cover.FallBackPoint;
            SoloDecision currentDecision = SAIN.Memory.Decisions.Main.Current;
            CoverPoint coverInUse = SAIN.Cover.CoverInUse;

            if (currentDecision == SoloDecision.Retreat && fallback != null && fallback.CheckPathSafety())
            {
                return fallback;
            }
            else if (coverInUse != null && !coverInUse.Spotted)
            {
                return coverInUse;
            }
            else
            {
                return SAIN.Cover.ClosestPoint;
            }
        }

        private CoverPoint CoverDestination;

        private void EngageEnemy()
        {
            SAIN.Steering.SteerByPriority();
            Shoot.Update();
        }

        public override void Start()
        {
            if (SAIN.Decision.CurrentSelfDecision == SelfDecision.RunAwayGrenade)
            {
                SAIN.Talk.TalkAfterDelay(EPhraseTrigger.OnEnemyGrenade, ETagStatus.Combat, 0.5f);
            }
        }

        public override void Stop()
        {
            CoverDestination = null;
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            DebugOverlay.AddCoverInfo(SAIN, stringBuilder);
        }
    }
}