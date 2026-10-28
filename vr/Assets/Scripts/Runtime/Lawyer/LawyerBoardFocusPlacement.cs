using System.Collections.Generic;
using UnityEngine;

namespace NCKH.VR.Lawyer
{
    public readonly struct LawyerBoardFocusPlacementResult
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public bool UsesRaisedArc { get; }
        public Vector3 ArcControlPoint { get; }
        public float FinalOverlapVolume { get; }
        public float PathOverlapVolume { get; }
        public float OverlapVolume => FinalOverlapVolume + PathOverlapVolume;

        public LawyerBoardFocusPlacementResult(
            Vector3 position,
            Quaternion rotation,
            bool usesRaisedArc,
            Vector3 arcControlPoint,
            float finalOverlapVolume,
            float pathOverlapVolume)
        {
            Position = position;
            Rotation = rotation;
            UsesRaisedArc = usesRaisedArc;
            ArcControlPoint = arcControlPoint;
            FinalOverlapVolume = finalOverlapVolume;
            PathOverlapVolume = pathOverlapVolume;
        }
    }

    public static class LawyerBoardFocusPlacement
    {
        private const float ClearThreshold = 0.000001f;

        public static LawyerBoardFocusPlacementResult Calculate(
            Vector3 cameraPosition,
            Vector3 cameraForward,
            Vector3 originalPosition,
            Quaternion originalRotation,
            Vector3 boardHalfExtents,
            IReadOnlyList<Bounds> obstacles,
            float baseDistance,
            float baseVerticalOffset,
            float clearance,
            float maximumLift,
            float maximumPullTowardCamera,
            float searchStep,
            float raisedArcLift,
            int pathSamples = 12)
        {
            Vector3 forward = Vector3.ProjectOnPlane(cameraForward, Vector3.up);
            forward = forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
            int liftSteps = Mathf.Max(0, Mathf.RoundToInt(maximumLift / searchStep));
            int pullSteps = Mathf.Max(0, Mathf.RoundToInt(maximumPullTowardCamera / searchStep));
            LawyerBoardFocusPlacementResult bestFallback = default;
            float bestFallbackFinalOverlap = float.PositiveInfinity;
            float bestFallbackPathOverlap = float.PositiveInfinity;
            float bestFallbackMovement = float.PositiveInfinity;
            bool hasFallback = false;
            LawyerBoardFocusPlacementResult bestClear = default;
            float bestClearMovement = float.PositiveInfinity;
            float bestClearPull = float.PositiveInfinity;
            bool hasClear = false;

            for (int liftIndex = 0; liftIndex <= liftSteps; liftIndex++)
            for (int pullIndex = 0; pullIndex <= pullSteps; pullIndex++)
            {
                float lift = liftIndex * searchStep;
                float pull = pullIndex * searchStep;
                float distance = Mathf.Max(0.01f, baseDistance - pull);
                Vector3 position = cameraPosition + forward * distance
                    + Vector3.up * (baseVerticalOffset + lift);
                Quaternion rotation = Quaternion.LookRotation(-forward, Vector3.up);
                float finalOverlap = CalculateOverlapVolume(
                    position, rotation, boardHalfExtents, obstacles, clearance);
                float straightPathOverlap = CalculatePathOverlapVolume(
                    originalPosition, originalRotation, position, rotation,
                    boardHalfExtents, obstacles, clearance, pathSamples, false, Vector3.zero);

                Vector3 controlPoint = Vector3.Lerp(originalPosition, position, 0.5f);
                controlPoint.y = Mathf.Max(originalPosition.y, position.y) + raisedArcLift;
                float arcPathOverlap = CalculatePathOverlapVolume(
                    originalPosition, originalRotation, position, rotation,
                    boardHalfExtents, obstacles, clearance, pathSamples, true, controlPoint);
                bool useArc = straightPathOverlap > ClearThreshold;
                float selectedPathOverlap = useArc ? arcPathOverlap : straightPathOverlap;
                float selectedOverlap = finalOverlap + selectedPathOverlap;
                float movementCost = lift * lift + pull * pull;
                LawyerBoardFocusPlacementResult candidate = new LawyerBoardFocusPlacementResult(
                    position, rotation, useArc, controlPoint,
                    finalOverlap, selectedPathOverlap);

                if (selectedOverlap <= ClearThreshold)
                {
                    if (!hasClear || movementCost < bestClearMovement - ClearThreshold
                        || Mathf.Abs(movementCost - bestClearMovement) <= ClearThreshold
                        && pull < bestClearPull)
                    {
                        bestClear = candidate;
                        bestClearMovement = movementCost;
                        bestClearPull = pull;
                        hasClear = true;
                    }
                    continue;
                }

                LawyerBoardFocusPlacementResult fallback = new LawyerBoardFocusPlacementResult(
                    position, rotation, useArc, controlPoint,
                    finalOverlap, selectedPathOverlap);
                if (!hasFallback || finalOverlap < bestFallbackFinalOverlap - ClearThreshold
                    || Mathf.Abs(finalOverlap - bestFallbackFinalOverlap) <= ClearThreshold
                    && (selectedPathOverlap < bestFallbackPathOverlap - ClearThreshold
                    || Mathf.Abs(selectedPathOverlap - bestFallbackPathOverlap) <= ClearThreshold
                    && movementCost < bestFallbackMovement))
                {
                    bestFallback = fallback;
                    bestFallbackFinalOverlap = finalOverlap;
                    bestFallbackPathOverlap = selectedPathOverlap;
                    bestFallbackMovement = movementCost;
                    hasFallback = true;
                }
            }

            if (hasClear)
                return bestClear;
            return hasFallback
                ? bestFallback
                : new LawyerBoardFocusPlacementResult(
                    cameraPosition + forward * baseDistance
                        + Vector3.up * baseVerticalOffset,
                    Quaternion.LookRotation(-forward, Vector3.up),
                    false,
                    Vector3.zero,
                    0f,
                    0f);
        }

        public static float CalculateOverlapVolume(
            Vector3 position,
            Quaternion rotation,
            Vector3 boardHalfExtents,
            IReadOnlyList<Bounds> obstacles,
            float clearance)
        {
            if (obstacles == null)
                return 0f;
            float total = 0f;
            Vector3 padded = boardHalfExtents + Vector3.one * clearance;
            for (int index = 0; index < obstacles.Count; index++)
                total += CalculateBoundsOverlap(position, rotation, padded, obstacles[index]);
            return total;
        }

        public static Vector3 QuadraticPoint(
            Vector3 start, Vector3 control, Vector3 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        private static float CalculatePathOverlapVolume(
            Vector3 startPosition,
            Quaternion startRotation,
            Vector3 endPosition,
            Quaternion endRotation,
            Vector3 boardHalfExtents,
            IReadOnlyList<Bounds> obstacles,
            float clearance,
            int pathSamples,
            bool raisedArc,
            Vector3 controlPoint)
        {
            float total = 0f;
            int samples = Mathf.Max(2, pathSamples);
            for (int index = 1; index <= samples; index++)
            {
                float t = index / (float)samples;
                Vector3 position = raisedArc
                    ? QuadraticPoint(startPosition, controlPoint, endPosition, t)
                    : Vector3.Lerp(startPosition, endPosition, t);
                Quaternion rotation = Quaternion.Slerp(startRotation, endRotation, t);
                total += CalculateOverlapVolume(
                    position, rotation, boardHalfExtents, obstacles, clearance);
            }
            return total;
        }

        private static float CalculateBoundsOverlap(
            Vector3 position,
            Quaternion rotation,
            Vector3 halfExtents,
            Bounds obstacle)
        {
            Quaternion inverse = Quaternion.Inverse(rotation);
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Vector3 center = obstacle.center;
            Vector3 extents = obstacle.extents;
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
            {
                Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                Vector3 local = inverse * (corner - position);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }

            float overlapX = Mathf.Min(halfExtents.x, max.x) - Mathf.Max(-halfExtents.x, min.x);
            float overlapY = Mathf.Min(halfExtents.y, max.y) - Mathf.Max(-halfExtents.y, min.y);
            float overlapZ = Mathf.Min(halfExtents.z, max.z) - Mathf.Max(-halfExtents.z, min.z);
            return overlapX > 0f && overlapY > 0f && overlapZ > 0f
                ? overlapX * overlapY * overlapZ
                : 0f;
        }
    }
}
