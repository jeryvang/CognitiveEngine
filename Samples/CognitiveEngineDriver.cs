using UnityEngine;
using CognitiveEngine.Core.DecisionGuidance;

namespace CognitiveEngine.Samples
{
    /// <summary>
    /// Unity sample host for <see cref="DecisionGuidanceRuntime"/> (triggers, primary output timing,
    /// repeat guard, panel suppression). Map trial JSON <c>interaction_signals[].event_type</c> (compare, dwell, selection)
    /// to <see cref="NotifyCompareInvoked"/>, <see cref="NotifyDwellThresholdMetForActiveProduct"/> / <see cref="NotifyDwellThresholdMet"/>,
    /// and <see cref="NotifySelect"/>. Use <see cref="SwitchProduct"/> or <see cref="NotifyProductFocusChanged"/> for focus.
    /// </summary>
    public class CognitiveEngineDriver : MonoBehaviour
    {
        [Header("Decision guidance")]
        [SerializeField] private bool logTriggerEmissions;

        private DecisionGuidanceRuntime _runtime;
        private string _currentProductId = "default";

        void Awake()
        {
            _runtime = new DecisionGuidanceRuntime(null, BuildPlaceholderDecisionContent);
        }

        void Start()
        {
            _runtime.NotifyProductFocusChanged(_currentProductId);
        }

        void Update()
        {
            if (_runtime == null) return;
            int ms = Mathf.Max(0, Mathf.RoundToInt(Time.deltaTime * 1000f));
            _runtime.Tick(ms);
        }

        /// <summary>Sets the active product for dwell helpers and notifies focus (MRU / revisit).</summary>
        public void SwitchProduct(string newProductId)
        {
            if (string.IsNullOrEmpty(newProductId)) return;
            _currentProductId = newProductId;
            _runtime.NotifyProductFocusChanged(newProductId);
        }

        public DecisionGuidanceRuntime GetDecisionGuidanceRuntime() => _runtime;

        public string ActiveProductId => _currentProductId;

        public DecisionOutputBuildResult? GetCurrentPrimaryOutput() =>
            _runtime.Session.Presentation.CurrentPrimaryOutput;

        public float GetPrimaryPresentationAlpha() =>
            _runtime.Session.Presentation.GetPrimaryPresentationAlpha();

        public void NotifyCompareInvoked() => EmitTrigger(DecisionTriggerInput.CompareInvoked());

        /// <summary>Measurement only: call when compare UI opens.</summary>
        public void NotifyCompareEntered() => _runtime.NotifyCompareEntered();

        /// <summary>Measurement only: call when compare UI closes.</summary>
        public void NotifyCompareExited() => _runtime.NotifyCompareExited();

        public void NotifyDwellThresholdMetForActiveProduct()
        {
            if (string.IsNullOrEmpty(_currentProductId)) return;
            EmitTrigger(DecisionTriggerInput.DwellThresholdMet(_currentProductId));
        }

        public void NotifyDwellThresholdMet(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;
            EmitTrigger(DecisionTriggerInput.DwellThresholdMet(productId));
        }

        public void NotifyProductFocusChanged(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;
            _currentProductId = productId;
            _runtime.NotifyProductFocusChanged(productId);
        }

        public void NotifySelect(string productId)
        {
            if (string.IsNullOrEmpty(productId)) return;
            _runtime.NotifySelect(productId);
        }

        public void SetDetailPanelOpen(bool isOpen) => _runtime.SetPanelOpen(isOpen);

        public bool TryConsumeExpandDetailTap() => _runtime.TryConsumeExpandTap();

        public void ResetGuidanceSession() => _runtime.ResetSession();

        private void EmitTrigger(DecisionTriggerInput input)
        {
            var emitted = _runtime.ProcessTriggerInput(input);
            if (logTriggerEmissions && emitted != null)
                Debug.Log($"[DecisionGuidance] Emitted {emitted.Value.Kind} {ResolvedDecisionTrigger.Signature(emitted.Value)}");
        }

        private static DecisionOutputContent BuildPlaceholderDecisionContent(ResolvedDecisionTrigger trigger)
        {
            switch (trigger.Kind)
            {
                case DecisionTriggerKind.Compare:
                    return new DecisionOutputContent
                    {
                        KeyDifference =
                            $"Key difference between {trigger.ProductIdLow} and {trigger.ProductIdHigh} (replace with catalog copy).",
                        WhichToChooseIf =
                            "Which to choose if… (replace with catalog copy)."
                    };
                default:
                    return new DecisionOutputContent
                    {
                        WhatThisGivesYou = $"What this gives you for {trigger.ProductIdLow} (replace with catalog copy).",
                        WhatYouTradeOff = "What you trade off (replace with catalog copy)."
                    };
            }
        }

        void OnDestroy()
        {
            if (_runtime != null)
                _runtime.ResetSession();
        }
    }
}
