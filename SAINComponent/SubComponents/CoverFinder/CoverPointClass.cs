﻿using EFT;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes;
using System;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder
{
    public class CoverPoint
    {
        public CoverPoint(SAINComponentClass sain, Vector3 point, Collider collider, NavMeshPath pathToPoint)
        {
            SAIN = sain;

            Position = point;
            Collider = collider;

            Vector3 size = collider.bounds.size;
            CoverHeight = size.y;
            CoverValue = (size.x + size.y + size.z).Round1();

            TimeCreated = Time.time;
            ReCheckStatusTimer = Time.time;

            PathToPoint = pathToPoint;
            PathLength = PathToPoint.CalculatePathLength();
            LastPathCalcTimer = Time.time + 2f;
            CheckPathSafetyTimer = Time.time + 1f;

            Id = Guid.NewGuid().ToString();
        }

        public bool IsBad;

        public float CoverValue { get; private set; }

        private readonly SAINComponentClass SAIN;

        public NavMeshPath PathToPoint { get; private set; }

        private float LastPathCalcTimer;

        public int HitInCoverUnknownCount { get; set; }
        public int HitInCoverCount { get; set; }

        public bool IsSafePath { get; set; }

        public float CoverHeight { get; private set; }

        public bool CheckPathSafety()
        {
            if (CheckPathSafetyTimer < Time.time)
            {
                CheckPathSafetyTimer = Time.time + 3f;

                Vector3 target;
                if (SAIN.HasEnemy)
                {
                    target = SAIN.Enemy.EnemyHeadPosition;
                }
                else if (SAIN.CurrentTargetPosition != null)
                {
                    target = SAIN.CurrentTargetPosition.Value;
                }
                else
                {
                    IsSafePath = true;
                    return true;
                }

                CalcPath();
                IsSafePath = SAINBotSpaceAwareness.CheckPathSafety(PathToPoint, target);
            }
            return IsSafePath;
        }

        private float CheckPathSafetyTimer = 0;

        public string Id { get; set; }

        public bool BotIsUsingThis { get; set; }

        public bool BotIsHere => CoverStatus == CoverStatus.InCover;

        public bool Spotted
        {
            get
            {
                if (HitInCoverCount > 1 || HitInCoverUnknownCount > 0)
                {
                    return true;
                }
                if (BotIsUsingThis && PointIsVisible())
                {
                    return true;
                }
                return false;
            }
        }

        public bool PointIsVisible()
        {
            if (VisTimer < Time.time)
            {
                VisTimer = Time.time + 0.5f;
                PointVis = false;
                if (SAIN.Enemy != null)
                {
                    Vector3 coverPos = Position;
                    coverPos += Vector3.up * 0.5f;
                    Vector3 start = SAIN.Enemy.EnemyHeadPosition;
                    Vector3 direction = coverPos - start;
                    PointVis = !Physics.Raycast(start, direction, direction.magnitude, LayerMaskClass.HighPolyWithTerrainMask);
                }
            }
            return PointVis;
        }

        private bool PointVis;
        private float VisTimer;

        public float CalcPath()
        {
            if (LastPathCalcTimer < Time.time)
            {
                LastPathCalcTimer = Time.time + 2;
                PathToPoint.ClearCorners();
                if (NavMesh.CalculatePath(BotOwner.Position, Position, -1, PathToPoint))
                {
                    PathLength = PathToPoint.CalculatePathLength();
                }
                else
                {
                    PathLength = float.MaxValue;
                }
            }
            return PathLength;
        }

        public float PathLength { get; private set; }

        public float Distance => (BotOwner.Position - Position).magnitude;

        public CoverStatus CoverStatus
        {
            get
            {
                ReCheckStatusTimer += Time.deltaTime;
                if (ReCheckStatusTimer < 0.1f)
                {
                    return OldStatus;
                }
                ReCheckStatusTimer = 0f;

                float sqrMagnitude = (BotOwner.Position - Position).sqrMagnitude;

                CoverStatus status;
                if (OldStatus == CoverStatus.InCover && sqrMagnitude <= InCoverStayDist * InCoverStayDist)
                {
                    status = CoverStatus.InCover;
                }
                else if (sqrMagnitude <= InCoverDist * InCoverDist)
                {
                    status = CoverStatus.InCover;
                }
                else if (sqrMagnitude <= CloseCoverDist * CloseCoverDist)
                {
                    status = CoverStatus.CloseToCover;
                }
                else if (sqrMagnitude <= MidCoverDist * MidCoverDist)
                {
                    status = CoverStatus.MidRangeToCover;
                }
                else
                {
                    status = CoverStatus.FarFromCover;
                }
                CoverDistSqrMagnitude = sqrMagnitude;
                OldStatus = status;
                return status;
            }
        }

        private CoverStatus OldStatus;
        private float ReCheckStatusTimer;

        public BotOwner BotOwner => SAIN.BotOwner;
        public Collider Collider { get; private set; }
        public Vector3 Position { get; set; }
        public float TimeCreated { get; private set; }

        private const float InCoverDist = 0.75f;
        private const float InCoverStayDist = 1f;
        private const float CloseCoverDist = 8f;
        private const float MidCoverDist = 20f;

        public float CoverDistSqrMagnitude { get; private set; }
    }
}