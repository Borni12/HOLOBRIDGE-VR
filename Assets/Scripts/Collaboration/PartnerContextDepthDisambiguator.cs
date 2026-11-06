using System;
using System.Collections.Generic;
using UnityEngine;
using OmniOS.Core;

namespace HoloBridge.Collaboration
{
    /// <summary>
    /// PartnerContextDepthDisambiguator (PCDD) - Flagship 3DUI Technique for IEEE VR 2027.
    /// Solves the "2D Occluded Selection Breakdown" in Cross-Device VR + Desktop Collaboration:
    /// - Desktop collaborator's 2D ray inherently lacks depth information.
    /// - When clicking through a dense, multi-layered 3D model, conventional raycasting selects the outermost shell.
    /// - PCDD collects all pierced candidates, evaluates proximity to the VR collaborator's tracked hands/gaze,
    ///   and automatically infers the intended internal target without requiring verbal coordination.
    /// </summary>
    public class PartnerContextDepthDisambiguator : MonoBehaviour
    {
        [System.Serializable]
        public class PenetratedCandidate
        {
            public SharedDigitalTwinAssembly.AssemblyPart part;
            [System.NonSerialized] public RaycastHit hitInfo;
            public float rayDistance;
            public float distanceToVRHand;
            public float partnerContextScore; // 0.0 to 1.0 (Gaussian proximity score)
            public bool isPCDDSelected;
        }

        [Header("PCDD Configuration")]
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private float vrWorkspaceSigma = 0.38f; // 38cm effective focus radius around VR hand
        [SerializeField] private float confidenceThreshold = 0.35f;

        [Header("Partner Tracking")]
        [SerializeField] private Vector3 simulatedVRHandPos = new Vector3(0, 1.45f, -0.2f);
        [SerializeField] private bool useSimulatedVRHand = false;

        [Header("Live Diagnostics")]
        [SerializeField] private List<PenetratedCandidate> currentCandidates = new List<PenetratedCandidate>();
        [SerializeField] private PenetratedCandidate activeSelectedCandidate = null;
        [SerializeField] private int totalDisambiguations = 0;
        [SerializeField] private float lastDisambiguationConfidence = 0f;

        public bool IsEnabled { get => isEnabled; set => isEnabled = value; }
        public List<PenetratedCandidate> CurrentCandidates => currentCandidates;
        public PenetratedCandidate ActiveSelectedCandidate => activeSelectedCandidate;
        public int TotalDisambiguations => totalDisambiguations;
        public float LastConfidence => lastDisambiguationConfidence;

        private SharedDigitalTwinAssembly targetAssembly;

        private void Start()
        {
            targetAssembly = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
        }

        /// <summary>
        /// Performs Partner-Context Depth Disambiguation for a given optical ray.
        /// </summary>
        /// <param name="ray">2D desktop mouse or pointer ray</param>
        /// <param name="vrHandWorldPos">Real or simulated 3D position of VR partner's active hand</param>
        /// <returns>Disambiguated candidate part, or null if nothing hit</returns>
        public PenetratedCandidate DisambiguateRay(Ray ray, Vector3 vrHandWorldPos)
        {
            if (targetAssembly == null)
            {
                targetAssembly = UnityEngine.Object.FindAnyObjectByType<SharedDigitalTwinAssembly>();
                if (targetAssembly == null) return null;
            }

            Vector3 effectiveVRHand = useSimulatedVRHand ? simulatedVRHandPos : vrHandWorldPos;

            // 1. Collect all pierced colliders along ray
            RaycastHit[] hits = Physics.RaycastAll(ray, 50.0f);
            if (hits == null || hits.Length == 0)
            {
                currentCandidates.Clear();
                activeSelectedCandidate = null;
                return null;
            }

            // 2. Filter and map hits to assembly parts
            currentCandidates.Clear();
            foreach (var hit in hits)
            {
                var part = targetAssembly.GetPartByGameObject(hit.collider.gameObject);
                if (part != null)
                {
                    // Avoid duplicate parts along same ray
                    if (currentCandidates.Exists(c => c.part.componentType == part.componentType)) continue;

                    float distToHand = Vector3.Distance(hit.point, effectiveVRHand);
                    // Gaussian Proximity Scoring: S = exp(-d^2 / (2 * sigma^2))
                    float score = Mathf.Exp(-(distToHand * distToHand) / (2f * vrWorkspaceSigma * vrWorkspaceSigma));

                    currentCandidates.Add(new PenetratedCandidate
                    {
                        part = part,
                        hitInfo = hit,
                        rayDistance = hit.distance,
                        distanceToVRHand = distToHand,
                        partnerContextScore = score,
                        isPCDDSelected = false
                    });
                }
            }

            if (currentCandidates.Count == 0)
            {
                activeSelectedCandidate = null;
                return null;
            }

            // 3. Sort candidates front-to-back along ray distance
            currentCandidates.Sort((a, b) => a.rayDistance.CompareTo(b.rayDistance));

            // 4. Evaluate PCDD Selection
            PenetratedCandidate bestCandidate = null;

            if (isEnabled)
            {
                // Search for candidate with maximum partner context score exceeding confidence threshold
                float maxScore = -1f;
                foreach (var c in currentCandidates)
                {
                    if (c.partnerContextScore > maxScore)
                    {
                        maxScore = c.partnerContextScore;
                        bestCandidate = c;
                    }
                }

                // If highest score is below threshold, fallback to conventional nearest surface hit
                if (maxScore < confidenceThreshold)
                {
                    bestCandidate = currentCandidates[0]; // Nearest hit
                    lastDisambiguationConfidence = 0.50f;
                }
                else
                {
                    lastDisambiguationConfidence = maxScore;
                    totalDisambiguations++;
                }
            }
            else
            {
                // Conventional 2D raycasting: Always pick first surface hit
                bestCandidate = currentCandidates[0];
                lastDisambiguationConfidence = 0.50f;
            }

            // Mark active selection
            foreach (var c in currentCandidates)
            {
                c.isPCDDSelected = (c == bestCandidate);
            }

            activeSelectedCandidate = bestCandidate;
            if (activeSelectedCandidate != null)
            {
                targetAssembly.SetPartSelected(activeSelectedCandidate.part.componentType, true);
                SpatialAudioEngine.Instance?.PlayHoverTick();
            }

            return activeSelectedCandidate;
        }

        /// <summary>
        /// Allows manual override to any penetrated layer from the floating depth stack HUD.
        /// </summary>
        public void ManualSelectCandidate(int candidateIndex)
        {
            if (candidateIndex < 0 || candidateIndex >= currentCandidates.Count) return;

            for (int i = 0; i < currentCandidates.Count; i++)
            {
                currentCandidates[i].isPCDDSelected = (i == candidateIndex);
            }

            activeSelectedCandidate = currentCandidates[candidateIndex];
            if (activeSelectedCandidate != null && targetAssembly != null)
            {
                targetAssembly.SetPartSelected(activeSelectedCandidate.part.componentType, true);
                SpatialAudioEngine.Instance?.PlayButtonClick();
            }
        }

        public void SetSimulatedVRHandPosition(Vector3 pos)
        {
            simulatedVRHandPos = pos;
            useSimulatedVRHand = true;
        }
    }
}
