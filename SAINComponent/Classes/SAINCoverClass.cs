﻿using Comfort.Common;
using EFT;
using SAIN.Helpers;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using System.Collections.Generic;
using UnityEngine;

namespace SAIN.SAINComponent.Classes
{
    public class SAINCoverClass : SAINBase, ISAINClass
    {
        public SAINCoverClass(SAINComponentClass sain) : base(sain)
        {
            CoverFinder = sain.GetOrAddComponent<CoverFinderComponent>();
            Player.HealthController.ApplyDamageEvent += OnBeingHit;
        }

        public void Init()
        {
            CoverFinder.Init(SAIN);
        }

        public void Update()
        {
            if (!SAIN.BotActive || SAIN.GameIsEnding)
            {
                ActivateCoverFinder(false);
                return;
            }

            // If the config option is enabled. Let a bot find cover all the time when they have a target or enemy if the enemy is the player.
            if (GlobalSettings.Cover.EnhancedCoverFinding && SAIN.CurrentTargetPosition != null)
            {
                if (SAIN.HasEnemy && SAIN.Enemy?.EnemyPlayer != null && SAIN.Enemy.EnemyPlayer.IsYourPlayer == true)
                {
                    ActivateCoverFinder(true);
                    return;
                }
                // No way to check if a GoalTarget is created by a player afaik, so enable it anyways
                else if (SAIN.Enemy == null)
                {
                    ActivateCoverFinder(true);
                    return;
                }
            }

            var CurrentDecision = SAIN.Memory.Decisions.Main.Current;
            var currentCover = CoverInUse;
            if (CurrentDecision == SoloDecision.UnstuckMoveToCover || CurrentDecision == SoloDecision.Retreat || CurrentDecision == SoloDecision.RunToCover || CurrentDecision == SoloDecision.WalkToCover)
            {
                ActivateCoverFinder(true);
            }
            else if (CurrentDecision == SoloDecision.HoldInCover && (currentCover == null || currentCover.Spotted == true || Time.time - currentCover.TimeCreated > 5f))
            {
                ActivateCoverFinder(true);
            }
            else
            {
                ActivateCoverFinder(false);
            }
        }

        public void Dispose()
        {
            try
            {
                Player.HealthController.ApplyDamageEvent -= OnBeingHit;
                CoverFinder?.Dispose();
            }
            catch { }
        }

        private void OnBeingHit(EBodyPart part, float unused, DamageInfo damage)
        {
            LastHitTime = Time.time;
            CoverPoint activePoint = CoverInUse;
            if (activePoint != null && activePoint.CoverStatus == CoverStatus.InCover)
            {
                SAINEnemy enemy = SAIN.Enemy;
                if (enemy != null && damage.Player != null && enemy.EnemyPlayer.ProfileId == damage.Player.iPlayer.ProfileId)
                {
                    activePoint.HitInCoverCount++;
                    if (!enemy.IsVisible)
                    {
                        activePoint.HitInCoverCount++;
                    }
                }
                else
                {
                    activePoint.HitInCoverUnknownCount++;
                }
            }
        }

        private void ActivateCoverFinder(bool value)
        {
            if (value && GetPointToHideFrom(out var target))
            {
                CoverFinder?.LookForCover(target.Value, BotOwner.Position);
            }
            if (!value)
            {
                CoverFinder?.StopLooking();
            }
        }

        public CoverPoint ClosestPoint
        {
            get
            {
                foreach (var point in CoverPoints)
                {
                    point?.CalcPath();
                }

                CoverFinderComponent.OrderPointsByPathDist(CoverPoints);

                for (int i = 0; i < CoverPoints.Count; i++)
                {
                    CoverPoint point = CoverPoints[i];
                    if (point != null)
                    {
                        if (point != null && point.Spotted == false && point.IsBad == false)
                        {
                            return point;
                        }
                    }
                }
                return null;
            }
        }

        private bool GetPointToHideFrom(out Vector3? target)
        {
            target = SAIN.Grenade.GrenadeDangerPoint ?? SAIN.CurrentTargetPosition;
            return target != null;
        }

        public bool DuckInCover()
        {
            var point = CoverInUse;
            if (point != null)
            {
                var move = SAIN.Mover;
                var prone = move.Prone;
                bool shallProne = prone.ShallProneHide();

                if (shallProne && (SAIN.Decision.CurrentSelfDecision != SelfDecision.None || SAIN.Suppression.IsHeavySuppressed))
                {
                    prone.SetProne(true);
                    return true;
                }
                if (move.Pose.SetPoseToCover())
                {
                    return true;
                }
                if (shallProne && point.Collider.bounds.size.y < 1f)
                {
                    prone.SetProne(true);
                    return true;
                }
            }
            return false;
        }

        public bool CheckLimbsForCover()
        {
            var enemy = SAIN.Enemy;
            if (enemy?.IsVisible == true)
            {
                if (CheckLimbTimer < Time.time)
                {
                    CheckLimbTimer = Time.time + 0.1f;
                    bool cover = false;
                    var target = enemy.EnemyIPlayer.WeaponRoot.position;
                    if (CheckLimbForCover(BodyPartType.leftLeg, target, 4f) || CheckLimbForCover(BodyPartType.leftArm, target, 4f))
                    {
                        cover = true;
                    }
                    else if (CheckLimbForCover(BodyPartType.rightLeg, target, 4f) || CheckLimbForCover(BodyPartType.rightArm, target, 4f))
                    {
                        cover = true;
                    }
                    HasLimbCover = cover;
                }
            }
            else
            {
                HasLimbCover = false;
            }
            return HasLimbCover;
        }

        private bool HasLimbCover;
        private float CheckLimbTimer = 0f;

        private bool CheckLimbForCover(BodyPartType bodyPartType, Vector3 target, float dist = 2f)
        {
            var position = BotOwner.MainParts[bodyPartType].Position;
            Vector3 direction = target - position;
            return Physics.Raycast(position, direction, dist, LayerMaskClass.HighPolyWithTerrainMask);
        }

        public bool BotIsAtCoverInUse(out CoverPoint coverInUse)
        {
            coverInUse = CoverInUse;
            return coverInUse != null && coverInUse.BotIsHere;
        }

        public bool BotIsAtCoverPoint(CoverPoint coverPoint)
        {
            return coverPoint != null && coverPoint.BotIsHere;
        }

        public bool BotIsAtCoverInUse()
        {
            var coverPoint = CoverInUse;
            return coverPoint != null && coverPoint.BotIsHere;
        }

        public CoverPoint CoverInUse
        {
            get
            {
                if (FallBackPoint != null && (FallBackPoint.BotIsUsingThis || BotIsAtCoverPoint(FallBackPoint)))
                {
                    return FallBackPoint;
                }
                foreach (var point in CoverPoints)
                {
                    if (point != null && (point.BotIsUsingThis || BotIsAtCoverPoint(point)))
                    {
                        return point;
                    }
                }
                return null;
            }
        }

        public List<CoverPoint> CoverPoints => CoverFinder.CoverPoints;
        public CoverFinderComponent CoverFinder { get; private set; }
        public CoverPoint CurrentCoverPoint => ClosestPoint;
        public CoverPoint FallBackPoint => CoverFinder.FallBackPoint;

        public float LastHitTime { get; private set; }
    }
}