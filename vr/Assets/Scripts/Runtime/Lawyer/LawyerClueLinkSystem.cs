using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace NCKH.VR.Lawyer
{
    [DisallowMultipleComponent]
    public sealed class LawyerClueLinkSystem : MonoBehaviour
    {
        [SerializeField] private Material stringMaterial;
        [SerializeField] private Color stringColor = new Color(0.72f, 0.08f, 0.06f, 1f);
        [SerializeField, Min(0.001f)] private float stringWidth = 0.008f;
        [SerializeField, Min(0.02f)] private float snapRadius = 0.12f;

        private readonly List<LawyerCluePin> pins = new List<LawyerCluePin>();
        private readonly List<CommittedString> committedStrings = new List<CommittedString>();
        private readonly List<CommittedString> terminalStrings = new List<CommittedString>();
        private LawyerGameSession session;
        private LawyerCluePin activeSource;
        private XRRayInteractor activeRay;
        private Vector3 activeEndPoint;
        private LineRenderer transientString;

        public event Action<LawyerLinkAttemptResult> LinkAttempted;

        /// <summary>Number of automatic evidence-to-reasoning links currently drawn.</summary>
        public int TerminalLinkCount => terminalStrings.Count;

        private sealed class CommittedString
        {
            public LawyerClueLink Link;
            public LawyerCluePin First;
            public LawyerCluePin Second;
            public LineRenderer Renderer;
        }

        private void LateUpdate()
        {
            if (transientString != null && activeSource != null)
            {
                UpdateActiveEndPoint();
                transientString.SetPosition(0, activeSource.AnchorPosition);
                transientString.SetPosition(1, activeEndPoint);
                if (activeRay == null || !activeRay.isActiveAndEnabled)
                    CancelActiveLink();
                else if (activeRay.logicalActivateState.wasCompletedThisFrame)
                    CompleteLink(activeSource, activeEndPoint);
            }

            for (int index = 0; index < committedStrings.Count; index++)
                UpdateString(committedStrings[index]);
            for (int index = 0; index < terminalStrings.Count; index++)
                UpdateString(terminalStrings[index]);
        }

        public void Configure(LawyerGameSession configuredSession)
        {
            session = configuredSession;
            RestoreCommittedLinks();
        }

        public void RegisterPin(LawyerCluePin pin)
        {
            if (pin != null && !pins.Contains(pin))
                pins.Add(pin);
        }

        public void UnregisterPin(LawyerCluePin pin)
        {
            pins.Remove(pin);
            if (pin == activeSource)
                CancelActiveLink();
        }

        public void BeginLink(LawyerCluePin source, XRRayInteractor ray)
        {
            if (source == null || source.Clue == null || ray == null || session == null
                || session.Phase != LawyerSessionPhase.EvidenceBoard)
                return;
            activeSource = source;
            activeRay = ray;
            UpdateActiveEndPoint();
            if (transientString == null)
                transientString = CreateStringRenderer("Dây liên kết đang kéo", false);
            transientString.gameObject.SetActive(true);
        }

        public void CompleteLink(LawyerCluePin source, Vector3 releasePosition)
        {
            if (source == null || source != activeSource)
                return;

            LawyerCluePin target = FindNearestTarget(source, releasePosition);
            if (target != null && session != null)
            {
                LawyerClueLink link = new LawyerClueLink(source.Clue.ClueId, target.Clue.ClueId);
                LawyerLinkAttemptResult result = session.TryAddEvidenceLink(
                    source.Clue.ClueId, target.Clue.ClueId);
                if (result == LawyerLinkAttemptResult.Added
                    || result == LawyerLinkAttemptResult.Solved)
                    AddCommittedString(link, source, target);
                LinkAttempted?.Invoke(result);
            }

            activeSource = null;
            activeRay = null;
            if (transientString != null)
                transientString.gameObject.SetActive(false);
        }

        public void CancelActiveLink()
        {
            activeSource = null;
            activeRay = null;
            if (transientString != null)
                transientString.gameObject.SetActive(false);
        }

        public void UndoLastLink()
        {
            if (session == null || !session.TryUndoLastLink(out LawyerClueLink removed))
                return;
            for (int index = committedStrings.Count - 1; index >= 0; index--)
            {
                if (!committedStrings[index].Link.Equals(removed))
                    continue;
                if (committedStrings[index].Renderer != null)
                    Destroy(committedStrings[index].Renderer.gameObject);
                committedStrings.RemoveAt(index);
                break;
            }
        }

        public void RestoreCommittedLinks()
        {
            ClearCommittedStrings();
            ClearTerminalLinks();
            if (session == null)
                return;
            for (int index = 0; index < session.ClueLinks.Count; index++)
            {
                LawyerClueLink link = session.ClueLinks[index];
                LawyerCluePin first = FindPin(link.FirstId);
                LawyerCluePin second = FindPin(link.SecondId);
                if (first != null && second != null)
                    AddCommittedString(link, first, second);
            }
        }

        /// <summary>
        /// Draws a terminal link that belongs to a revealed reasoning card. These links are
        /// deliberately kept out of the undoable evidence-link collection so that moving a
        /// card or undoing the last evidence link cannot break the revealed reasoning chain.
        /// </summary>
        public void AddTerminalLink(LawyerClueLink link, LawyerCluePin first, LawyerCluePin second)
        {
            if (first == null || second == null)
                return;
            for (int index = 0; index < terminalStrings.Count; index++)
            {
                if (terminalStrings[index].Link.Equals(link))
                    return;
            }
            terminalStrings.Add(new CommittedString
            {
                Link = link,
                First = first,
                Second = second,
                Renderer = CreateStringRenderer($"Dây suy luận {link.FirstId} - {link.SecondId}", true)
            });
        }

        public void ClearTerminalLinks()
        {
            for (int index = 0; index < terminalStrings.Count; index++)
                if (terminalStrings[index].Renderer != null)
                    Destroy(terminalStrings[index].Renderer.gameObject);
            terminalStrings.Clear();
        }

        private LawyerCluePin FindNearestTarget(LawyerCluePin source, Vector3 position)
        {
            LawyerCluePin best = null;
            float bestDistance = snapRadius * snapRadius;
            for (int index = 0; index < pins.Count; index++)
            {
                LawyerCluePin candidate = pins[index];
                if (candidate == null || candidate == source || candidate.Clue == null
                    || candidate.Clue == source.Clue)
                    continue;
                float distance = (candidate.AnchorPosition - position).sqrMagnitude;
                if (distance > bestDistance)
                    continue;
                best = candidate;
                bestDistance = distance;
            }
            return best;
        }

        private void UpdateActiveEndPoint()
        {
            if (activeRay == null)
                return;
            activeEndPoint = activeRay.TryGetCurrent3DRaycastHit(out RaycastHit hit)
                ? hit.point
                : activeRay.rayEndPoint;
        }

        private LawyerCluePin FindPin(string clueId)
        {
            for (int index = 0; index < pins.Count; index++)
                if (pins[index] != null && pins[index].Clue != null && pins[index].Clue.ClueId == clueId)
                    return pins[index];
            return null;
        }

        private void AddCommittedString(LawyerClueLink link, LawyerCluePin first, LawyerCluePin second)
        {
            committedStrings.Add(new CommittedString
            {
                Link = link,
                First = first,
                Second = second,
                    Renderer = CreateStringRenderer($"Dây {link.FirstId} - {link.SecondId}", false)
            });
        }

        private LineRenderer CreateStringRenderer(string objectName, bool terminal)
        {
            GameObject lineObject = new GameObject(objectName);
            lineObject.layer = gameObject.layer;
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = stringWidth;
            line.endWidth = stringWidth;
            Color color = terminal
                ? new Color(0.95f, 0.18f, 0.58f, 1f)
                : stringColor;
            line.startColor = color;
            line.endColor = color;
            line.sharedMaterial = stringMaterial;
            line.numCapVertices = 4;
            return line;
        }

        private void ClearCommittedStrings()
        {
            for (int index = 0; index < committedStrings.Count; index++)
                if (committedStrings[index].Renderer != null)
                    Destroy(committedStrings[index].Renderer.gameObject);
            committedStrings.Clear();
        }

        private static void UpdateString(CommittedString item)
        {
            if (item == null || item.First == null || item.Second == null || item.Renderer == null)
                return;
            item.Renderer.SetPosition(0, item.First.AnchorPosition);
            item.Renderer.SetPosition(1, item.Second.AnchorPosition);
        }
    }
}
