# Debug overlay spec (Phase 3 validation)

1. Same behavioral inputs → same state sequence  
2. Confidence accumulates and decays predictably  
3. Product switching resets correctly  
4. Priority rules behave consistently  

The overlay must show: **Current State**, **Triggered Rule**, **Confidence score**, **Active product ID**, **Session reset event** (when product changes).

---

## What to show in the overlay

| Overlay line      | Source | How to get it |
|-------------------|--------|----------------|
| **Current State** | Latest state from engine | `CognitiveState.State` from `OnStateUpdated`, or if no subscription: `engine.Logs[engine.Logs.Count - 1].FinalState` (after each Update). Use `.ToString()` for display (e.g. Neutral, Exploration, Comparison, Hesitation, ReadyToConfirm). |
| **Triggered Rule** | Rule that produced that state | `CognitiveState.ReasoningTag` from `OnStateUpdated`, or `engine.Logs[engine.Logs.Count - 1].TriggeredRule`. Examples: NoSignals, ProductFocus, SwipeVelocity, HighDwell, ConfirmIntentHigh, etc. |
| **Confidence score** | 0–1 value for current state | `CognitiveState.Confidence` from `OnStateUpdated`, or `engine.Logs[engine.Logs.Count - 1].Confidence`. Display as number (e.g. 0.42) or percentage. |
| **Active product ID** | Current product context | `engine.ActiveProductId`. Update whenever you read state (e.g. every frame after Update, or in OnStateUpdated). |
| **Session reset event** | When product changed | Subscribe to `engine.OnAuditEntry`. When `entry.Kind == AuditEntryKind.ContextSwitch`, show a line like "Session reset: [FromProductId] → [ToProductId]" (use `entry.FromProductId`, `entry.ToProductId`). Optionally also show on `SessionReset` kind: "Session reset: [ProductId]". Clear or append to a small log so the last switch is visible. |

---

## Recommended implementation (Unity)

1. **Subscribe once (e.g. in Start):**
   - `engine.OnStateUpdated += OnEngineStateUpdated;`
   - `engine.OnAuditEntry += OnEngineAuditEntry;`

2. **OnStateUpdated:**  
   Store the received `CognitiveState` (State, ReasoningTag, Confidence) in a field. Optionally update ActiveProductId from `engine.ActiveProductId` here too.

3. **OnAuditEntry:**  
   If `entry.Kind == AuditEntryKind.ContextSwitch`, set a string like `lastSessionResetText = $"Product: {entry.FromProductId} → {entry.ToProductId}";`. If you also want to show SessionReset: when `entry.Kind == AuditEntryKind.SessionReset`, set e.g. `lastSessionResetText = $"Session reset: {entry.ProductId}";`.

4. **Every frame (or after engine.Update):**  
   Refresh the overlay text from:
   - Current State = stored state (or `Logs[Count-1].FinalState`)
   - Triggered Rule = stored ReasoningTag (or `Logs[Count-1].TriggeredRule`)
   - Confidence = stored Confidence (or `Logs[Count-1].Confidence`)
   - Active product ID = `engine.ActiveProductId`
   - Session reset event = `lastSessionResetText` (clear it after N seconds if you want, or keep last one)

5. **UI:**  
   One panel (e.g. Text or multiple Labels) showing the five lines. No need for polish; readability for validation is enough.

---

## Types (engine API)

- **CognitiveState:** `State` (StateType), `Confidence` (float), `ReasoningTag` (string = rule name)
- **TickLog:** `TriggeredRule`, `FinalState`, `Confidence` (same meaning; use when you read from Logs)
- **AuditEntry:** `Kind` (ContextSwitch | SessionReset | Tick), `FromProductId`, `ToProductId`, `ProductId`
- **StateType:** enum Neutral, Exploration, Comparison, Hesitation, ReadyToConfirm

---

## What validate

- **Same inputs → same state sequence:** Repeat same actions (e.g. dwell, then confirm); overlay state/rule sequence should match.
- **Confidence:** Watch value over time after injecting signals; should rise then decay when signals stop.
- **Product switch reset:** Change product; overlay should show ContextSwitch line and Active product ID should update; state should reset (e.g. to Neutral) for the new product.
- **Priority rules:** Inject conflicting signals (e.g. dwell + confirm); overlay should show the higher-priority rule (e.g. ConfirmIntentHigh).
