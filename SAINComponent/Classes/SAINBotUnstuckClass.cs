﻿using Comfort.Common;
using EFT;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Debug
{
    public class SAINBotUnstuckClass : SAINBase, ISAINClass
    {
        static SAINBotUnstuckClass()
        {
            _pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        }

        private static readonly FieldInfo _pathControllerField;

        public SAINBotUnstuckClass(SAINComponentClass sain) : base(sain)
        {
        }

        public void Init()
        {
            PathController = _pathControllerField.GetValue(BotOwner.Mover) as PathControllerClass;
        }

        public bool BotIsMoving { get; private set; }
        private bool _botIsMoving;

        public float TimeStartedMoving { get; private set; }
        public float TimeStoppedMoving { get; private set; }

        private float _timeStartMoving;

        private float _timeStopMoving;
        public float TimeSinceMovingStarted => TimeStartedMoving - Time.time;

        private void CheckIfMoving()
        {
            float time = Time.time;

            // Check if a bot is being told to move by the botowner pathfinder, and record the time this starts and stops
            bool botWasMoving = _botIsMoving;
            _botIsMoving = BotOwner.Mover.IsMoving;
            if (_botIsMoving && !botWasMoving)
            {
                _timeStartMoving = time;
            }
            else if (!_botIsMoving && botWasMoving)
            {
                _timeStopMoving = time;
            }

            // How long a bot must be "moving" for before we check
            const float movingForPeriod = 0.25f;

            // Has the bot been told to move for over x seconds? then record the state for use in other classes
            if (_botIsMoving && time - _timeStartMoving > movingForPeriod)
            {
                if (!BotIsMoving)
                {
                    TimeStartedMoving = time;
                }
                BotIsMoving = true;
            }
            else if (!_botIsMoving)
            {
                if (BotIsMoving)
                {
                    TimeStoppedMoving = time;
                }
                BotIsMoving = false;
            }
        }

        private void CheckIfPositionChanged()
        {
            if (CheckPositionTimer < Time.time)
            {
                CheckPositionTimer = Time.time + 1f;

                bool botChangedPositionLast = BotHasChangedPosition;

                const float DistThreshold = 0.25f;
                BotHasChangedPosition = (LastPos - BotOwner.Position).sqrMagnitude > DistThreshold * DistThreshold;

                if (botChangedPositionLast && !BotHasChangedPosition)
                {
                    TimeStartedChangingPosition = Time.time;
                }
                else if (BotHasChangedPosition)
                {
                    TimeStartedChangingPosition = 0f;
                }

                LastPos = BotOwner.Position;
            }
        }

        private float _nextVaultTime;

        public void Update()
        {
            if (SAIN.BotActive
                && !SAIN.GameIsEnding)
            {
                if (_nextVaultTime < Time.time)
                {
                    _nextVaultTime = Time.time + 0.5f;
                    Player.MovementContext.TryVaulting();
                }

                CheckIfMoving();
                CheckIfPositionChanged();

                if (CheckStuckTimer < Time.time)
                {
                    if (BotOwner.DoorOpener.Interacting)
                    {
                        CheckStuckTimer = Time.time + 1f;
                        BotIsStuck = false;
                    }
                    else
                    {
                        CheckStuckTimer = Time.time + 0.25f;

                        bool stuck = BotStuckGeneric()
                            || BotStuckOnObject()
                            || BotStuckOnPlayer();

                        if (!BotIsStuck && stuck)
                        {
                            TimeStuck = Time.time;
                        }
                        if (TimeSinceStuck < 1f)
                        {
                            BotIsStuck = false;
                        }
                        else
                        {
                            BotIsStuck = stuck;
                        }
                        BotIsStuck = stuck;
                    }

                    // If the bot is no longer stuck, but we are checking if we can teleport them, cancel the coroutine
                    if (!BotIsStuck
                        && TeleportCoroutineStarted
                        && TeleportCoroutine != null)
                    {
                        SAIN.StopCoroutine(TeleportCoroutine);
                        TeleportCoroutineStarted = false;
                        HasTriedJumpOrVault = false;
                        JumpTimer = Time.time + 1f;
                        IsTeleporting = false;
                    }

                    if (BotIsStuck)
                    {
                        if (DebugStuckTimer < Time.time && TimeSinceStuck > 3f)
                        {
                            DebugStuckTimer = Time.time + 3f;
                            Logger.LogWarning($"[{BotOwner.name}] has been stuck for [{TimeSinceStuck}] seconds " +
                                $"on [{StuckHit.transform?.name}] object " +
                                $"at [{StuckHit.transform?.position}] " +
                                $"with Current Decision as [{SAIN.Memory.Decisions.Main.Current}]");
                        }

                        if (HasTriedJumpOrVault
                            && TimeSinceStuck > 5f
                            && TimeSinceTriedJumpOrVault + 2f < Time.time
                            && !TeleportCoroutineStarted)
                        {
                            TeleportCoroutineStarted = true;
                            TeleportCoroutine = SAIN.StartCoroutine(CheckIfTeleport());
                        }

                        if (JumpTimer < Time.time)
                        {
                            JumpTimer = Time.time + 1f;

                            if (!Player.MovementContext.TryVaulting())
                            {
                                if (TimeSinceStuck > 5f)
                                {
                                    if (!HasTriedJumpOrVault)
                                    {
                                        HasTriedJumpOrVault = true;
                                        TimeSinceTriedJumpOrVault = Time.time;
                                    }
                                    SAIN.Mover.TryJump();
                                }
                            }
                        }
                    }
                }
            }
        }

        private float TimeSinceTriedJumpOrVault;
        private bool HasTriedJumpOrVault;
        private const float MinDistance = 100f;
        private const float MaxDistance = 300f;
        private const float PathLengthCoef = 1.25f;
        private const float MinDistancePathLength = MinDistance * PathLengthCoef;

        private bool TeleportCoroutineStarted;
        private Coroutine TeleportCoroutine;

        private IEnumerator CheckIfTeleport()
        {
            bool shallTeleport = true;
            var humanPlayers = GetHumanPlayers();

            Vector3? teleportDestination = GetPlaceToTeleport();

            if (teleportDestination != null)
            {
                foreach (var humanPlayer in humanPlayers)
                {
                    if (!BotIsStuck)
                    {
                        shallTeleport = false;
                        break;
                    }

                    if (humanPlayer == null || humanPlayer?.HealthController?.IsAlive == false)
                    {
                        continue;
                    }

                    // Make sure the player isn't visible to the bot
                    if (SAIN.Memory.VisiblePlayers.Contains(humanPlayer))
                    {
                        shallTeleport = false;
                        break;
                    }

                    // Makes sure the bot isn't too close to a human for them to hear
                    float sqrMag = (humanPlayer.Position - SAIN.Position).sqrMagnitude;
                    if (sqrMag < MinDistance * MinDistance)
                    {
                        shallTeleport = false;
                        break;
                    }

                    // Checks the max distance to do a path calculation
                    if (sqrMag < MaxDistance * MaxDistance)
                    {
                        Vector3 playerPosition = humanPlayer.Position;
                        NavMeshPath path = CalcPath(SAIN.Position, playerPosition, out float pathLength);
                        if (CheckPathLength(playerPosition, path, pathLength) == false)
                        {
                            shallTeleport = false;
                            break;
                        }
                    }

                    // Check next player on the next frame
                    yield return null;
                }
            }

            IsTeleporting = BotIsStuck && shallTeleport && teleportDestination != null;

            if (IsTeleporting)
            {
                Teleport(teleportDestination.Value);
                float distance = (teleportDestination.Value - SAIN.Position).magnitude;
                Logger.LogDebug($"Teleporting stuck bot: [{Player.name}] [{distance}] meters to the next corner they are trying to go to");
            }

            yield return null;
        }

        private bool IsTeleporting;

        private Vector3? GetPlaceToTeleport()
        {
            Vector3? destination = null;
            if (BotOwner.Mover.HavePath)
            {
                destination = PathController.CurrentCorner();
            }
            return destination;
        }

        private void Teleport(Vector3 position)
        {
            if (teleportTimer < Time.time)
            {
                teleportTimer = Time.time + 3f;
                Player.Teleport(position);
            }
        }

        private float teleportTimer;

        public PathControllerClass PathController { get; private set; }

        private static NavMeshPath CalcPath(Vector3 start, Vector3 end, out float pathLength)
        {
            if (PathToPlayer == null)
            {
                PathToPlayer = new NavMeshPath();
            }
            if (NavMesh.SamplePosition(end, out NavMeshHit hit, 1f, -1))
            {
                PathToPlayer.ClearCorners();
                if (NavMesh.CalculatePath(start, hit.position, -1, PathToPlayer))
                {
                    pathLength = PathToPlayer.CalculatePathLength();
                    return PathToPlayer;
                }
            }
            pathLength = 0f;
            return null;
        }

        private static bool CheckPathLength(Vector3 end, NavMeshPath path, float pathLength)
        {
            if (path == null)
            {
                return false;
            }
            if (path.status == NavMeshPathStatus.PathPartial)
            {
                Vector3 lastCorner = path.corners[path.corners.Length - 1];
                float sqrMag = (lastCorner - end).magnitude;
                float combinedLength = sqrMag + pathLength;
                if (combinedLength < MinDistancePathLength)
                {
                    return false;
                }
            }
            if (path.status == NavMeshPathStatus.PathComplete && pathLength < MinDistancePathLength)
            {
                return false;
            }
            return path.status != NavMeshPathStatus.PathInvalid;
        }

        private List<Player> GetHumanPlayers()
        {
            HumanPlayers.Clear();
            var allPlayers = Singleton<GameWorld>.Instance?.AllAlivePlayersList;
            if (allPlayers != null)
            {
                foreach (var player in allPlayers)
                {
                    if (player != null && player.AIData.IsAI == false && player.HealthController.IsAlive)
                    {
                        HumanPlayers.Add(player);
                    }
                }
            }
            return HumanPlayers;
        }

        private bool ShallTeleport()
        {
            return false;
        }

        private static NavMeshPath PathToPlayer;
        private List<Player> HumanPlayers = new List<Player>();

        public void Dispose()
        {
        }

        private RaycastHit StuckHit = new RaycastHit();
        private float DebugStuckTimer = 0f;
        private float CheckStuckTimer = 0f;
        public float TimeSinceStuck => Time.time - TimeStuck;
        public float TimeStuck { get; private set; }

        private float CheckPositionTimer = 0f;

        private Vector3 LastPos = Vector3.zero;

        public float TimeSpentNotMoving => Time.time - TimeStartedChangingPosition;

        public float TimeStartedChangingPosition { get; private set; }

        public bool BotIsStuck { get; private set; }

        private bool CanBeStuckDecisions(SoloDecision decision)
        {
            return decision == SoloDecision.Search || decision == SoloDecision.WalkToCover || decision == SoloDecision.DogFight || decision == SoloDecision.RunToCover || decision == SoloDecision.RunAway || decision == SoloDecision.UnstuckSearch || decision == SoloDecision.UnstuckDogFight || decision == SoloDecision.UnstuckMoveToCover;
        }

        public bool BotStuckOnPlayer()
        {
            var decision = SAIN.Memory.Decisions.Main.Current;
            if (!BotHasChangedPosition && CanBeStuckDecisions(decision))
            {
                if (BotOwner.Mover == null)
                {
                    return false;
                }
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.4f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                Vector3 lookDir = BotOwner.LookDirection;
                lookDir.y = 0;

                var moveHits = Physics.SphereCastAll(botPos, 0.15f, moveDir, 0.5f, LayerMaskClass.PlayerMask);
                if (moveHits.Length > 0)
                {
                    foreach (var move in moveHits)
                    {
                        if (move.transform.name != BotOwner.name)
                        {
                            StuckHit = move;
                            return true;
                        }
                    }
                }

                var lookHits = Physics.SphereCastAll(botPos, 0.15f, lookDir, 0.5f, LayerMaskClass.PlayerMask);
                if (lookHits.Length > 0)
                {
                    foreach (var look in lookHits)
                    {
                        if (look.transform.name != BotOwner.name)
                        {
                            StuckHit = look;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private bool BotStuckGeneric()
        {
            return BotIsMoving && !BotHasChangedPosition && !BotOwner.DoorOpener.Interacting && TimeSpentNotMoving > 2f;
        }

        public bool BotStuckOnObject()
        {
            if (CanBeStuckDecisions(SAIN.Memory.Decisions.Main.Current) && !BotHasChangedPosition && !BotOwner.DoorOpener.Interacting && SAIN.Decision.TimeSinceChangeDecision > 1f)
            {
                if (BotOwner.Mover == null)
                {
                    return false;
                }
                Vector3 botPos = BotOwner.Position;
                botPos.y += 0.4f;
                Vector3 moveDir = BotOwner.Mover.DirCurPoint;
                moveDir.y = 0;
                if (Physics.SphereCast(botPos, 0.15f, moveDir, out var hit, 0.25f, LayerMaskClass.HighPolyWithTerrainMask))
                {
                    StuckHit = hit;
                    return true;
                }
            }
            return false;
        }

        public bool BotHasChangedPosition { get; private set; }

        private float JumpTimer = 0f;
    }
}