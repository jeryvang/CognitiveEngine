Copy-paste for Google Doc
=========================

How to use this file
--------------------

1. Scroll to **START COPY BELOW** and select from there through **END COPY**.
2. Paste into your Google Doc.
3. Apply **Heading 1** or **Heading 2** to each main title if you want a clear outline.

---

START COPY BELOW
================

PHASE 6 RECOMMENDATION
======================

Recommendation Summary
----------------------

Phase 6 is a lean decision guidance layer. It is decision-first. It is for validation. It is not for open-ended conversation.

Goal: validate whether guidance helps users decide faster.

Agreed timeline and budget for this phase
-----------------------------------------

Table: timeline and budget

| Item | Value |
|------|-------|
| **Timeline** | About 5 to 6 working days |
| **Budget** | $800 |

What we will deliver
--------------------

• **Define** behavior-to-response mapping.  
• **Define** structured output formats (single product vs comparison).  
• **Set** trigger logic for dwell, compare, and revisit.  
• **Specify** how outputs **surface by default** (direct, no tap required for the core answer) and how **optional** whisper or panel attach.  
• **Keep** a quality bar: outputs stay short, consistent, and decision-focused.  


1) Behavior to trigger mapping
------------------------------

Table: behavior to trigger mapping

| Behavior | What is triggered | How it shows in P6 (primary) |
|----------|-------------------|------------------------------|
| Dwell | Single product decision output for the product the user is viewing | Show the decision output directly after the delay in section 4. |
| Compare | Comparison decision output for the two products being compared | Show the decision output directly after the delay in section 4. Limit comparison to the latest 2 active products. |
| Revisit | Single product decision output for the product the user returned to | Show the decision output directly after the delay in section 4. |

Priority rules (strict and deterministic)

Table: priority order (no conflicts)

| Priority | Signal | When it may run |
|----------|--------|-----------------|
| 1 (highest) | Compare | Always wins when a compare is active. |
| 2 | Revisit | Only if no compare is active. |
| 3 (lowest) | Dwell | Only if no compare is active and no revisit is active. |

Table: when not to show new guidance

| Do not show new guidance | Rule |
|--------------------------|------|
| After user confirms a decision | Stop nudges for that decision step. |
| While primary decision output is visible | No second full output on top. See section 6. |
| During the repeat guard | See section 4. |


2) Structured output formats
----------------------------

Single product

Table: single product output

| Field | Meaning |
|------|---------|
| What this gives you | The main upside you get if you choose this option. |
| What you trade off | The main downside or cost you accept with this option. |

Comparison

Table: comparison output

| Field | Meaning |
|------|---------|
| Key difference | The main difference between the two products. |
| Which to choose if… | Short conditional steer (for example: if you care most about X, lean A; if Y, lean B). |

Earlier label mapping (same scope, new wording for P6)

Table: label mapping

| Earlier draft label | P6 label |
|--------------------|----------|
| Best for / Key highlight / Why it matters | Replaced by **What this gives you** and **What you trade off** for faster decision testing. |
| Who it's for / Recommendation | Replaced by **Which to choose if…** alongside **Key difference**. |


3) Prompt, panel, and whisper behavior
---------------------------------------

Decision-first presentation

Table: presentation rules

| Rule | Meaning |
|------|---------|
| Direct surface | When a trigger fires, the **structured output** (section 2) is the **main** thing the user sees after the short delay in section 4. |
| No tap to unlock core answer | The user should not **need** to tap a whisper to see the core decision text in P6. |

Optional panel

The panel is optional. It is not the default path in P6.
Use it only as a read more expansion (more detail, links, or future layout).

Optional whisper

Whisper is optional. If present, keep it short. It can reinforce the trigger. It must not block the direct output.

Table: optional UI actions

| UI element (optional paths) | User action | Result |
|------------------------------|-------------|--------|
| Expand control (if you add one) | Tap | Open optional panel or expanded area. Core answer already visible without this tap. |
| Whisper dismiss (if whisper exists) | Tap | Hide whisper only. |
| Optional panel close | Tap | Close panel only. |

Rule: no open-ended conversation. Only the structured fields in section 2.


4) Timing (P6, keep it simple)
-----------------------------

Table: timing values

| Parameter | Value | Notes |
|-----------|-------|-------|
| Output appearance delay | 1.0 s to 1.5 s | Wait after trigger conditions are met before showing the primary decision output. |
| Primary output visible time | 6 s to 8 s | Do not hard auto-hide. Use a soft collapse or fade after this window so users do not lose context while reading. |
| Repeat guard | 30 s to 60 s | Same trigger type should not fire again inside this window. |

Decision confirmation signal (what counts as a decision)

Table: decision confirmation

| Signal | Counts as a decision |
|--------|-----------------------|
| User taps select | Yes. Stop nudges for that decision step. |
| Sustained stay after output | Yes. If the user stays on the same product for a sustained duration after the output appears, treat it as a decision and stop nudges for that step. |


5) State reset definition
-------------------------

Table: reset events

| Event | What resets |
|-------|-------------|
| New product selected | Pending timers cancel. Dwell tracking resets for the new active product. Revisit logic re-evaluates for the new context. |
| Session restart | All guidance state clears. Repeat guard clears. |
| User confirms decision | Active guidance stops for that step. Triggers can apply again only after a new decision context (for example new session or new product flow). |


6) Basic edge cases
-------------------

Table: edge cases

| Case | Rule |
|------|------|
| Repeated taps on expand (if present) | First tap wins. Ignore duplicate taps for 300 ms while the action is processing. |
| Fast product switching | Cancel any pending timer. Reset dwell timing for the newly active product. Re-evaluate compare and revisit from the new context. |
| Overlapping triggers | Do not stack a second full decision output while the first is still visible. If a trigger fires during that window, ignore it until the surface clears and the repeat guard allows it. |


Inputs needed to lock
---------------------

Table: inputs needed

| Needed | Why |
|--------|-----|
| Product copy for all fields in section 2 | Outputs are short and pre-written |
| Approval of optional whisper text (if any) | So UX copy is final |
| Agreement on where the **direct** output appears | Inline card, strip, or other primary surface |
| Agreement on whether optional panel ships in P6 | Default can be off |


Summary
-------

Phase 6 tests whether guidance helps users **decide faster**. Triggers and priority stay the same (compare, then revisit, then dwell). Outputs use **What this gives you / What you trade off** and **Key difference / Which to choose if…**. The **primary** path is **direct** decision output. **Whisper is reduced. Panel is optional.** Timing stays **simple** for fast testing. Timeline is about 5 to 6 working days. Budget is $800.

END COPY
========

---

Repo note
---------

P6 timing is intentionally **minimal**. If you need the older strict cooldown table again, add it as a **post-validation** appendix after user tests. Pipe tables may paste oddly in Google Docs. Use **Insert, Table** if needed.
