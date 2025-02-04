﻿using BepInEx.Logging;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.SAINComponent.Classes.Decision;
using SAIN.SAINComponent.Classes.Talk;
using SAIN.SAINComponent.Classes.WeaponFunction;
using SAIN.SAINComponent.Classes.Mover;
using SAIN.SAINComponent.Classes;
using SAIN.SAINComponent.SubComponents;
using SAIN.SAINComponent;
using System.Text;
using UnityEngine;
using SAIN.SAINComponent.SubComponents.CoverFinder;
using SAIN.Layers.Combat.Solo;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace SAIN.Layers.Combat.Solo.Cover
{
    internal class WalkToCoverAction : SAINAction
    {
        public WalkToCoverAction(BotOwner bot) : base(bot, nameof(WalkToCoverAction))
        {
        }

        public override void Update()
        {
            if (CoverDestination != null)
            {
                if (!SAIN.Cover.CoverPoints.Contains(CoverDestination) || CoverDestination.Spotted)
                {
                    CoverDestination.BotIsUsingThis = false;
                    CoverDestination = null;
                }
            }

            SAIN.Mover.SetTargetMoveSpeed(1f);
            SAIN.Mover.SetTargetPose(1f);

            if (CoverDestination == null)
            {
                var coverPoint = SAIN.Cover.ClosestPoint;
                if (coverPoint != null && !coverPoint.Spotted && SAIN.Mover.GoToPoint(coverPoint.Position, out bool calculating))
                {
                    CoverDestination = coverPoint;
                    CoverDestination.BotIsUsingThis = true;
                    RecalcPathTimer = Time.time + 2f;
                    SAIN.Mover.SetTargetMoveSpeed(1f);
                    SAIN.Mover.SetTargetPose(1f);
                }
            }
            if (CoverDestination != null && RecalcPathTimer < Time.time)
            {
                if (SAIN.Mover.GoToPoint(CoverDestination.Position, out bool calculating))
                {
                    RecalcPathTimer = Time.time + 2f;

                    var personalitySettings = SAIN.Info.PersonalitySettings;
                    float moveSpeed = personalitySettings.Sneaky ? personalitySettings.SneakySpeed : 1f;
                    SAIN.Mover.SetTargetMoveSpeed(1f);
                    SAIN.Mover.SetTargetPose(1f);
                }
                else
                {
                    RecalcPathTimer = Time.time + 0.25f;
                }
            }

            EngageEnemy();
        }

        private float FindTargetCoverTimer = 0f;
        private float RecalcPathTimer = 0f;

        private bool FindTargetCover()
        {
            var coverPoint = SAIN.Cover.ClosestPoint;
            if (coverPoint != null && !coverPoint.Spotted)
            {
                coverPoint.BotIsUsingThis = true;
                CoverDestination = coverPoint;
                DestinationPosition = coverPoint.Position;
            }
            return false;
        }

        private bool MoveTo(Vector3 position)
        {
            if (SAIN.Mover.GoToPoint(position, out bool calculating))
            {
                CoverDestination.BotIsUsingThis = true;
                if (SAIN.HasEnemy || BotOwner.Memory.IsUnderFire)
                {
                    SAIN.Mover.SetTargetMoveSpeed(1f);
                }
                else
                {
                    SAIN.Mover.SetTargetMoveSpeed(0.65f);
                }
                SAIN.Mover.SetTargetPose(1f);
                return true;
            }
            return false;
        }

        private CoverPoint CoverDestination;
        private Vector3 DestinationPosition;
        private float SuppressTimer;

        private void EngageEnemy()
        {
            if (SAIN.Enemy?.IsVisible == false && SAIN.Enemy.Seen && SAIN.Enemy.TimeSinceSeen < 5f && SAIN.Enemy.LastCornerToEnemy != null && SAIN.Enemy.CanSeeLastCornerToEnemy)
            {
                Vector3 corner = SAIN.Enemy.LastCornerToEnemy.Value;
                corner += Vector3.up * 1f;
                SAIN.Steering.LookToPoint(corner);
                if (SuppressTimer < Time.time 
                    && BotOwner.WeaponManager.HaveBullets 
                    && SAIN.Shoot(true, true, SAINComponentClass.EShootReason.WalkToCoverSuppress))
                {
                    SuppressTimer = Time.time + 0.5f * Random.Range(0.66f, 1.25f);
                }
            }
            else
            {
                SAIN.Shoot(false);
                if (!BotOwner.ShootData.Shooting)
                {
                    SAIN.Steering.SteerByPriority(false);
                }
                Shoot.Update();
            }
        }

        public override void Start()
        {
            SAIN.Mover.Sprint(false);
        }

        public override void Stop()
        {
            if (CoverDestination != null)
            {
                CoverDestination.BotIsUsingThis = false;
                CoverDestination = null;
            }
        }

        public override void BuildDebugText(StringBuilder stringBuilder)
        {
            DebugOverlay.AddCoverInfo(SAIN, stringBuilder);
        }
    }
}