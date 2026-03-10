# Decision-forming & confidence-stage texts (Unity)

## Short reply to Elian’s summary

**Elian:** *"So basically I can keep track of whether the user has explored and compared certain products, and if this is the case and the confidence level is greater than 0.8, the texts are displayed or the necessary actions are performed. Right?"*

**Reply:** Yes, that’s right. One small distinction:

- **First text (Decision forming – "narrowing down"):** Trigger when **confidence > 0.8** (you don’t need the “explored and compared” history for this one; it can show as soon as confidence is high).
- **Second text (High confidence – "matches your preferences"):** Trigger when **confidence > 0.8** **and** you’ve tracked that the user has explored and compared (e.g. your session flags), **and** they’re clearly focused on one product. Then show the text or run the action.

So: track explored/compared; use confidence > 0.8 for both; use the “explored and compared” condition only for the second text.

---

## What to do on the Unity side

You can wire this in wherever he already uses the engine (e.g. the same script that holds the engine/driver reference and calls `Update()`).

### Step 1. Get “current” state and confidence every frame

- **If using streaming:** After `_streamingEngine.Update(...)` in `Update()`, use:
  - `_streamingEngine.Current.State`
  - `_streamingEngine.Current.Confidence`
  - `_streamingEngine.Current.ReasoningTag`
- **If using discrete:** After `_engine.Update(...)` in `Update()`, read the **last tick** (don’t rely only on `OnStateUpdated`):
  - `if (_engine.Logs.Count > 0) { var last = _engine.Logs[_engine.Logs.Count - 1]; ... last.FinalState, last.Confidence, last.TriggeredRule }`

So in both modes you have, each frame: `currentState`, `currentConfidence`, and optionally the rule/tag.

### Step 2. Decision forming text ("narrowing down")

- After you have `currentConfidence` (and optionally `currentState`) for this frame:
  - If `currentConfidence > 0.8f` and (optional) `currentState` is `Exploration` or `ReadyToConfirm` → show *"It looks like you're narrowing down your choice"*.
- Add UX as needed: e.g. only show once per product, or hide again when confidence drops below a lower threshold (e.g. 0.6), or after a few seconds.

### Step 3. Session flags for “explored and compared enough”

- Add two booleans (e.g. on the same MonoBehaviour that owns the engine):
  - `bool _hasSeenExploration;`
  - `bool _hasSeenComparison;`
- In the same place you read current state each frame (or in `OnStateUpdated`), set:
  - `if (currentState == StateType.Exploration) _hasSeenExploration = true;`
  - `if (currentState == StateType.Comparison) _hasSeenComparison = true;`
- Reset these when starting a new session or when the user leaves the product flow (e.g. when you call `ResetSession()` or switch to a different screen).

### Step 4. High confidence text (“matches your preferences”)

- After you have `currentConfidence`, `currentState`, and the two flags:
  - If `currentConfidence > 0.8f` **and** `_hasSeenExploration && _hasSeenComparison` **and** the user is clearly focused on one product (e.g. `currentState == ReadyToConfirm`, or same product focused for a few seconds), then show *"Based on what you've explored, this product appears to match your preferences."*
- To avoid showing both texts at once: treat “High confidence” as a later stage (e.g. only after “Decision forming” has been shown at least once, or require a short sustained high-confidence period on one product).

### Step 5. Hooking into your UI

- From the script that holds the engine/driver reference, either:
  - Set a string or enum on a shared “AI copy” or UI model (e.g. `DecisionStage.None | DecisionForming | HighConfidence`) and let your UI/text component react to that, or
  - Invoke events/callbacks (e.g. `OnDecisionFormingShown`, `OnHighConfidenceShown`) that your UI subscribes to and uses to show/hide the two texts.