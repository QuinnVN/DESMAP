using System.Collections.Generic;
using NCKH.VR.Lawyer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace NCKH.VR.Lawyer.Tests
{
    public sealed class LawyerBoardFocusPlacementTests
    {
        private static readonly Quaternion FacingCamera = Quaternion.Euler(0f, 180f, 0f);

        [Test]
        public void ClearBasePoseUsesConfiguredHeadsetOffset()
        {
            LawyerBoardFocusPlacementResult result = Calculate(
                new Vector3(0f, 1.6f, 1.2f), FacingCamera,
                new Vector3(0.1f, 0.1f, 0.04f), new List<Bounds>());

            Assert.That(result.Position, Is.EqualTo(new Vector3(0f, 1.64f, 0.7f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(result.UsesRaisedArc, Is.False);
            Assert.That(result.FinalOverlapVolume, Is.Zero.Within(0.000001f));
            Assert.That(result.PathOverlapVolume, Is.Zero.Within(0.000001f));
            Assert.That(result.OverlapVolume, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void VerticalCorrectionIsPreferredOverCamerawardCorrection()
        {
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(0f, 1.64f, 0.7f), new Vector3(0.1f, 0.06f, 0.1f))
            };
            LawyerBoardFocusPlacementResult result = Calculate(
                new Vector3(0f, 1.84f, 1.2f), FacingCamera,
                new Vector3(0.1f, 0.02f, 0.04f), obstacles);

            Assert.That(result.Position.z, Is.EqualTo(0.7f).Within(0.001f));
            Assert.That(result.Position.y, Is.GreaterThan(1.64f));
            Assert.That(result.OverlapVolume, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void CamerawardCorrectionIsUsedWhenVerticalSearchCannotClear()
        {
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(0f, 1.64f, 0.7f), new Vector3(0.1f, 2f, 0.08f))
            };
            LawyerBoardFocusPlacementResult result = Calculate(
                new Vector3(0f, 1.64f, 0.6f), FacingCamera,
                new Vector3(0.1f, 0.1f, 0.04f), obstacles);

            Assert.That(result.Position.z, Is.LessThan(0.7f));
            Assert.That(result.OverlapVolume, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void FallbackMinimizesOverlapWhenEveryCandidateIsBlocked()
        {
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(0f, 1.75f, 0.7f), new Vector3(10f, 10f, 10f))
            };
            LawyerBoardFocusPlacementResult baseOnly = Calculate(
                new Vector3(0f, 1.6f, 1.2f), FacingCamera,
                new Vector3(0.1f, 0.1f, 0.04f), obstacles, 0f, 0f);
            LawyerBoardFocusPlacementResult searched = Calculate(
                new Vector3(0f, 1.6f, 1.2f), FacingCamera,
                new Vector3(0.1f, 0.1f, 0.04f), obstacles);

            Assert.That(searched.OverlapVolume, Is.GreaterThan(0f));
            Assert.That(searched.OverlapVolume, Is.LessThanOrEqualTo(baseOnly.OverlapVolume));
            Assert.That(searched.FinalOverlapVolume, Is.GreaterThan(0f));
        }

        [Test]
        public void RaisedArcIsSelectedWhenStraightPathIsBlocked()
        {
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(0f, 1.64f, 0.95f), new Vector3(0.1f, 0.06f, 0.04f))
            };
            LawyerBoardFocusPlacementResult result = Calculate(
                new Vector3(0f, 1.64f, 1.2f), FacingCamera,
                new Vector3(0.1f, 0.02f, 0.02f), obstacles);

            Assert.That(result.Position, Is.EqualTo(new Vector3(0f, 1.64f, 0.7f))
                .Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(result.UsesRaisedArc, Is.True);
            Assert.That(result.ArcControlPoint.y, Is.EqualTo(1.89f).Within(0.001f));
            Assert.That(result.FinalOverlapVolume, Is.Zero.Within(0.000001f));
            Assert.That(result.PathOverlapVolume, Is.Zero.Within(0.000001f));
            Assert.That(result.OverlapVolume, Is.Zero.Within(0.000001f));
        }

        [Test]
        public void BlockedAnimationPathIsReportedSeparatelyFromClearFinalPose()
        {
            var obstacles = new List<Bounds>
            {
                new Bounds(new Vector3(0f, 1.76f, 0.95f), new Vector3(0.1f, 0.5f, 0.04f))
            };
            LawyerBoardFocusPlacementResult result = Calculate(
                new Vector3(0f, 1.64f, 1.2f), FacingCamera,
                new Vector3(0.1f, 0.02f, 0.02f), obstacles, 0f, 0f);

            Assert.That(result.FinalOverlapVolume, Is.Zero.Within(0.000001f));
            Assert.That(result.PathOverlapVolume, Is.GreaterThan(0f));
            Assert.That(result.OverlapVolume,
                Is.EqualTo(result.PathOverlapVolume).Within(0.000001f));
        }

        private static LawyerBoardFocusPlacementResult Calculate(
            Vector3 originalPosition,
            Quaternion originalRotation,
            Vector3 halfExtents,
            IReadOnlyList<Bounds> obstacles,
            float maximumLift = 0.3f,
            float maximumPull = 0.15f)
        {
            return LawyerBoardFocusPlacement.Calculate(
                new Vector3(0f, 1.6f, 0f), Vector3.forward,
                originalPosition, originalRotation, halfExtents, obstacles,
                0.7f, 0.04f, 0.01f, maximumLift, maximumPull,
                0.05f, 0.25f, 12);
        }
    }
}
