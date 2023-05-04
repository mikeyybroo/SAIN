﻿using BepInEx.Logging;
using EFT;
using Movement.Helpers;
using SAIN_Helpers;
using UnityEngine;
using UnityEngine.AI;
using static HairRenderer;
using static SAIN_Helpers.SAIN_Math;

namespace SAIN.Movement.Layers.DogFight
{
    internal class UpdateMove
    {
        private const float UpdateFrequency = 0.1f;
        public UpdateMove(BotOwner bot)
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(this.GetType().Name);
            BotOwner = bot;
            Dodge = new BotDodge(bot);
            CoverFinder = new CoverFinder(bot);
        }

        public void Update(bool debug = false, bool debugDrawAll = false)
        {
            if (ReactionTimer < Time.time)
            {
                ReactionTimer = Time.time + UpdateFrequency;
                UpdateDoorOpener();

                if (CanShootEnemy)
                {
                    EnemyLastSeenTime = Time.time;
                    LastEnemyPosition = BotOwner.Memory.GoalEnemy.CurrPosition;
                }
                else if (BotIsAtLastEnemyPosition)
                {
                    LastEnemyPosition = null;
                }

                if (CheckFallBackConditions(debug, debugDrawAll))
                {
                    SetSprint(HasStamina);
                    FallingBack = true;
                    return;
                }
                else
                {
                    FallingBack = false;
                    SetSprint(false);
                }

                if (CanShootEnemy)
                {
                    DecidePoseFromCoverLevel();
                }
                else
                {
                    DecideMovementSpeed();

                    if (LastEnemyPosition != null && !BotIsAtLastEnemyPosition)
                    {
                        BotOwner.MoveToEnemyData.TryMoveToEnemy(LastEnemyPosition.Value);
                    }
                    else
                    {
                        BotOwner.MoveToEnemyData.TryMoveToEnemy(BotOwner.Memory.GoalEnemy.CurrPosition);
                    }
                }
            }
        }

        private void DecideMovementSpeed()
        {
            if (IsEnemyVeryClose)
            {
                FullSpeed();
            }
            else if (IsEnemyClose)
            {
                NormalSpeed();
            }
            else if (ShouldISneak)
            {
                Sneak();
            }
            else
            {
                SlowWalk();
            }
        }

        public bool FallingBack { get; private set; }
        public Vector3? LastEnemyPosition { get; private set; }
        public float EnemyLastSeenTime { get; private set; }
        public bool IsEnemyClose
        {
            get
            {
                CheckPathLength();
                return PathLength < 10f;
            }
        }
        public bool IsEnemyVeryClose
        {
            get
            {
                CheckPathLength();
                return PathLength < 3f;
            }
        }
        public bool BotIsAtLastEnemyPosition => LastEnemyPosition != null && Vector3.Distance(LastEnemyPosition.Value, BotOwner.Transform.position) < 2f;
        public bool Reloading => BotOwner.WeaponManager.Reload.Reloading;
        public bool CanShootEnemyAndVisible => CanShootEnemy && CanSeeEnemy;
        public bool CanShootEnemy => BotOwner.Memory.GoalEnemy.IsVisible;
        public bool CanSeeEnemy => BotOwner.Memory.GoalEnemy.CanShoot;
        public bool ShouldISneak => !CanShootEnemyAndVisible && EnemyLastSeenTime + 10f < Time.time;
        private bool HasStamina => BotOwner.GetPlayer.Physical.Stamina.NormalValue > 0f;
        public bool IsSprintingFallback => FallingBack && BotOwner.Mover.IsMoving;

        private Vector3? FallbackPosition;

        private bool CheckFallBackConditions(bool debugMode = false, bool debugDrawAll = false)
        {
            if (!Reloading && !BotOwner.Medecine.FirstAid.Using)
            {
                return false;
            }

            if (FallbackPosition != null)
            {
                if (!CoverFinder.Analyzer.AnalyseCoverPosition(FallbackPosition.Value))
                {
                    FallbackPosition = null;
                }
                else
                {
                    BotOwner.GoToPoint(FallbackPosition.Value, false, -1, false, true, true);
                }
            }

            while (FallbackPosition == null)
            {
                if (CoverFinder.FindFallbackPosition(out Vector3? coverPosition))
                {
                    FallbackPosition = coverPosition;

                    DebugDrawFallback(debugMode);

                    StartFallback();
                }
            }

            return true;
        }

        private void StartFallback()
        {
            BotOwner.Steering.LookToMovingDirection();
            SetSprint(true);
            BotOwner.GoToPoint(FallbackPosition.Value, false, -1, false, true, true);
            UpdateDoorOpener();
        }

        private void DebugDrawFallback(bool debugMode)
        {
            if (debugMode)
            {
                DebugDrawer.Line(FallbackPosition.Value, BotOwner.MyHead.position, 0.1f, Color.green, 3f);
                DebugDrawer.Line(BotOwner.Memory.GoalEnemy.Owner.MyHead.position, BotOwner.MyHead.position, 0.1f, Color.green, 3f);
                DebugDrawer.Line(BotOwner.Memory.GoalEnemy.Owner.MyHead.position, FallbackPosition.Value, 0.1f, Color.green, 3f);
                Logger.LogDebug($"{BotOwner.name} is falling back while reloading");
            }
        }

        private void DecidePoseFromCoverLevel()
        {
            CoverFinder cover = new CoverFinder(BotOwner);
            cover.Analyzer.AnalyseCoverPosition(BotOwner.Transform.position);

            if (cover.Analyzer.FullCover)
            {
                //BotOwner.MovementPause(UpdateFrequency * 2f);
                BotOwner.SetPose(1f);
            }
            else if (cover.Analyzer.ChestCover)
            {
                //BotOwner.MovementPause(UpdateFrequency * 2f);
                BotOwner.SetPose(0.75f);
            }
            else if (cover.Analyzer.WaistCover)
            {
                //BotOwner.MovementPause(UpdateFrequency * 2f);
                BotOwner.SetPose(0.5f);
            }
            else if (cover.Analyzer.ProneCover)
            {
                //BotOwner.MovementPause(UpdateFrequency * 2f);
                BotOwner.SetPose(0f);
            }
            else
            {
                BotOwner.SetPose(0.9f);
                BotNeedsToBackup();
            }
        }

        private bool BotNeedsToBackup()
        {
            if (CheckEnemyDistance(out Vector3 trgPos))
            {
                Vector3 DodgeFallBack = Dodge.FindArcPoint(trgPos, BotOwner.Memory.GoalEnemy.CurrPosition, 2f, 10f, 1f, 2f);
                if (NavMesh.SamplePosition(DodgeFallBack, out NavMeshHit hit, 10f, -1))
                {
                    trgPos = hit.position;
                }

                BotOwner.GoToPoint(trgPos, false, -1, false, false, true);
                UpdateDoorOpener();

                return true;
            }
            return false;
        }

        public bool CheckEnemyDistance(out Vector3 trgPos)
        {
            Vector3 a = -NormalizeFastSelf(BotOwner.Memory.GoalEnemy.Direction);

            trgPos = Vector3.zero;

            float num = 0f;
            if (NavMesh.SamplePosition(BotOwner.Position + a * 2f / 2f, out NavMeshHit navMeshHit, 1f, -1))
            {
                trgPos = navMeshHit.position;

                Vector3 a2 = trgPos - BotOwner.Position;

                float magnitude = a2.magnitude;

                if (magnitude != 0f)
                {
                    Vector3 a3 = a2 / magnitude;

                    num = magnitude;

                    if (NavMesh.SamplePosition(BotOwner.Position + a3 * 2f, out navMeshHit, 1f, -1))
                    {
                        trgPos = navMeshHit.position;

                        num = (trgPos - BotOwner.Position).magnitude;
                    }
                }
            }
            if (num != 0f && num > BotOwner.Settings.FileSettings.Move.REACH_DIST)
            {
                navMeshPath_0.ClearCorners();
                if (NavMesh.CalculatePath(BotOwner.Position, trgPos, -1, navMeshPath_0) && navMeshPath_0.status == NavMeshPathStatus.PathComplete)
                {
                    trgPos = navMeshPath_0.corners[navMeshPath_0.corners.Length - 1];

                    return CheckStraightDistance(navMeshPath_0, num);
                }
            }
            return false;
        }

        private bool CheckStraightDistance(NavMeshPath path, float straighDist)
        {
            return path.CalculatePathLength() < straighDist * 1.2f;
        }

        private void ResetTarget()
        {
            FallbackPosition = null;
            FallingBack = false;
        }

        private void UpdateDoorOpener()
        {
            BotOwner.DoorOpener.Update();
        }

        private void SetSprint(bool value)
        {
            if (value)
            {
                BotOwner.GetPlayer.EnableSprint(true);
                BotOwner.Sprint(true);
                BotOwner.SetPose(1f);
                BotOwner.SetTargetMoveSpeed(1f);
            }
            else
            {
                BotOwner.GetPlayer.EnableSprint(false);
                BotOwner.Sprint(false);
            }
        }

        private void FullSpeed()
        {
            BotOwner.SetTargetMoveSpeed(1f);
        }

        private void CrouchWalk()
        {
            BotOwner.SetPose(0f);
            BotOwner.SetTargetMoveSpeed(1f);
        }

        private void Sneak()
        {
            BotOwner.SetPose(0f);
            BotOwner.SetTargetMoveSpeed(0f);
        }

        private void SlowWalk()
        {
            BotOwner.SetTargetMoveSpeed(0.33f);
        }

        private void NormalSpeed()
        {
            BotOwner.SetTargetMoveSpeed(0.75f);
        }

        private void CheckPathLength()
        {
            if (LastDistanceCheck < Time.time)
            {
                LastDistanceCheck = Time.time + 0.5f;
                NavMesh.CalculatePath(BotOwner.Transform.position, BotOwner.Memory.GoalEnemy.CurrPosition, -1, Path);
                PathLength = Path.CalculatePathLength();
            }
        }

        private NavMeshPath Path = new NavMeshPath();
        private float LastDistanceCheck = 0f;
        private float PathLength = 0f;

        private NavMeshPath navMeshPath_0 = new NavMeshPath();
        private readonly BotOwner BotOwner;
        private readonly ManualLogSource Logger;
        private readonly BotDodge Dodge;
        private readonly CoverFinder CoverFinder;
        private float DodgeTimer = 0f;
        private float ReactionTimer = 0f;
    }
}