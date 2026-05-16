# D.U3 — LobbyPanel + weapon pick UI · REPORT

> Closed 2026-05-17 (executor: Claude Opus 4.7, sequential auto-run mode per Bill's choice).

---

## Result: D.U3a PASS · D.U3b deferred as planned

Client UI + state machine + payload wiring done. Mock smoke proves end-to-end: BootState (Editor URL empty) → manual GoTo<LobbyState> → LobbyPanel opens with 3 mock weapons → Ready/Pick events route to NetClient.Send → state transition out closes the panel cleanly.

Final mock smoke logs:

```
[Bill] Ready. 14 services in ~500ms.
[Arena] bootstrap ready (Bill.IsReady=True)
[Bill.State] None -> Boot
[Arena.Boot] URL parsed: wsUrl=ws://localhost:2567, session=, token=(0 chars), discordId=
[Arena.Boot] URL not valid — staying in Boot.
[Bill.State] Boot -> Boot
[Smoke] Mock ArenaContext.MyPlayer populated with 3 weapons
[Bill.State] Boot -> Lobby
[Arena.Lobby] Opened LobbyPanel, 3 weapons available
[Arena.Lobby] ready
[Arena.Net] Send(ready) ignored — not connected.
[Arena.Lobby] pick weapon: weapon_kiem_01
[Arena.Net] Send(select_weapon) ignored — not connected.
[Smoke] After GoTo<Connecting>, IsOpen<LobbyPanel>=False
```

Errors at end of Play: 3× PanelSettings + 1× URP missing-types — D.U1 baseline only, no D.U3 regression.

---

## Sub-by-sub status

| Sub | Status | Commit | Notes |
|---|---|---|---|
| 1. Verify baseline (read-only) | ✅ | — | Surfaced ArraySchema<T> doesn't implement IEnumerable<T> — Sub 2 used int indexer. |
| 2. ArenaContext: WeaponSnapshot + hydration | ✅ | `a7e89e7` | PlayerSnapshot gains AvailableWeapons[] + LockedWeapon. |
| 3. LobbyPanel.uxml | ✅ | `f907fb3` | header + 2 player slots + weapon ListView + Ready/Unready buttons. |
| 4. lobby.uss | ✅ | `2b7879d` | Flat dark theme, no animations (D.U7 will juice). |
| 5. LobbyPanel.cs | ✅ | `bcc2e5f` | BasePanel impl, Resources.Load UXML+USS, ListView binding, 3 events, 250ms scheduler poll for opponent. |
| 6. LobbyState.cs | ✅ | `1f3ce08` | Open panel, wire events to NetClient.Send, listen for phase=countdown (stub for D.U4). |
| 7. ConnectingState→LobbyState transition + register | ✅ | `c6c7e0f` | ConnectingState subscribes PhaseChangedEvent. ArenaStates.Register appends LobbyState. |
| 8. Mock smoke (no commit) | ✅ | — | execute_code reflection populated ArenaContext, transitioned state, raised events, verified IsOpen + cleanup. |

---

## Deviations from PLAN

1. **ArraySchema<T> doesn't implement IEnumerable<T>.** PLAN §6 and SUBTASKS Sub 2 originally said `foreach (WeaponSchema w in p.available_weapons)`. Sub 1 verification caught the issue — `ArraySchema<T> : IArraySchema` only. Switched to `int` indexer + `Count` (which IS exposed). Pattern documented inline in `ArenaContext.cs`.

2. **No asmdef created (carryover from D.U2 deviation §2.3).** All new RadiantArena scripts stay in `Assembly-CSharp` / `Assembly-CSharp-Editor`. LobbyPanel is in `Assets/RadiantArena/UI/LobbyPanel.cs` (no Editor folder, so it goes to Assembly-CSharp like the rest). Same plan when BillGameCore gets its asmdef story sorted, all of RadiantArena can migrate together.

3. **Resources/ folder placement.** PLAN §3 said `Assets/RadiantArena/UI/Resources/`. Confirmed by Sub 1: that path works for `Resources.Load<VisualTreeAsset>("LobbyPanel")`. Unity scans every `Resources` subfolder in the project at build time.

4. **Mock smoke step injected MyPlayer via reflection.** The setter is private (per ArenaContext design — only NetClient.OnStateChange should mutate it). Sub 8's reflection trick is for testing only and never runs in production builds. Documented in SUBTASKS Sub 8.

5. **`ConnectingState.cs` inline-comment for `ManualRoomConnect` updated to `ArenaConnectWindow`.** Carryover hygiene from D.U2's commit `5cf5834` rename.

6. **PanelSettings ThemeStyleSheet + pickingMode shim (post-visual-check).** Visual check (post-Sub-8) caught that BillGameCore's `UIService.Initialize` creates a `PanelSettings` with no `ThemeStyleSheet` — Unity 6 then skips Label text rendering and eats Button click events. Bill reported `"tương tác k đc và chữ thì ko hiện chỉ có section thôi"`. Fixed by:
   - Adding `Assets/RadiantArena/UI/Resources/ArenaRuntimeTheme.tss` (imports `unity-theme://default` + minor Label/Button/ListView rules).
   - Extending `ArenaBootstrap.InitArena()` with `ApplyArenaRuntimeTheme()`: loads the TSS via `Resources.Load`, reflects into `Bill.UI._doc.panelSettings.themeStyleSheet`, and flips `_uiRoot.pickingMode = PickingMode.Position` so child Buttons receive clicks. Logs `[Arena] Applied ArenaRuntimeTheme.tss + pickingMode to Bill.UI`.
   - Shim runs once at boot; remove when BillGameCore ships its own default theme.

7. **Lobby UI is placeholder for D.U3a — Bill's full vision deferred.** After visual check Bill confirmed shipped panel is acceptable but called out his polish vision: each weapon as an icon card (not a text row), hover tooltip showing full stats + skill descriptions, drag-and-drop onto a player-slot weapon socket. Captured as memory + tracked as a future "lobby polish" lát (likely between D.U3 and D.U4, or rolled into D.U7 juice). Do NOT retrofit into D.U3a — that lát closed on the placeholder.

---

## Bill checkpoints — what happened

| Checkpoint | Outcome |
|---|---|
| Sub 1 verify | One ArraySchema gotcha surfaced — Sub 2 adapted before write. |
| Sub 3 UXML sanity | Bill skipped (auto-run mode); structure matches Sub 3 spec verbatim. |
| Sub 5 code review | Skipped — auto-run; LobbyPanel.cs available in `bcc2e5f`. |
| Sub 8 D.U3a close vs extend | Closed at D.U3a. D.U3b backlog tracked below. |

---

## What's left for D.U3b

Triggered when arena-server Lát D.3 + D.4 ship:

1. Real 2-instance smoke: ParrelSync 2 Editors, both ArenaConnectWindow → connect to localhost room.
2. Watch `state.phase` flip `lobby → countdown` after both `ready=true`.
3. Verify `LobbyState.OnPhaseChanged("countdown")` log fires (currently stub — D.U4 will replace with `Bill.State.GoTo<CountdownState>()`).
4. Opponent ready mirror — `ArenaContext.OpponentPlayer.Ready` polled every 250ms — UI reflects opponent click within 250ms.

The client side is fully ready; D.U3b is purely an integration verification.

---

## Known baseline (NOT D.U3 issues)

- 3× `No Theme Style Sheet set to PanelSettings` — BillGameCore framework (`UIService.Initialize`, `DebugOverlay`, `CheatConsole`). D.U1 carryover; D.U3 patches the `Bill.UI` one at runtime (§2.6) but the warning still fires from framework boot order. DebugOverlay + CheatConsole stay unpatched (not user-facing).
- 1× `Missing types referenced from UniversalRenderPipelineGlobalSettings` — URP downgrade leftover. D.U1 carryover.
- 3× `CS0618 PlayerSettings.GetScriptingDefineSymbolsForGroup obsolete` (warning, not error) — `Assets/BillGameCore/Editor/BillSetupWizard.cs`. Pre-existing BillGameCore framework code; not introduced by D.U3.

---

## Files added/edited

| Path | Lines | Status |
|---|---|---|
| `Assets/RadiantArena/Scripts/Net/ArenaContext.cs` | +47 | edit (WeaponSnapshot + AvailableWeapons hydration) |
| `Assets/RadiantArena/UI/Resources/LobbyPanel.uxml` | 32 | new |
| `Assets/RadiantArena/UI/Resources/lobby.uss` | 170 | new |
| `Assets/RadiantArena/UI/Resources/ArenaRuntimeTheme.tss` | 14 | new (post-visual-check, §2.6) |
| `Assets/RadiantArena/UI/LobbyPanel.cs` | 164 | new |
| `Assets/RadiantArena/Scripts/States/LobbyState.cs` | 93 | new |
| `Assets/RadiantArena/Scripts/States/ConnectingState.cs` | edit (+phase routing) | |
| `Assets/RadiantArena/Scripts/States/ArenaStates.cs` | edit (+1 line) | |
| `Assets/RadiantArena/Scripts/Bootstrap/ArenaBootstrap.cs` | edit (+ApplyArenaRuntimeTheme, §2.6) | |

Stage 1 docs (PLAN + SUBTASKS + OPUS_PROMPTS): ~930 lines under `arena-unity/tasks/todo/D.U3-lobby-panel/`.

---

## Commits (this Lát)

```
c6c7e0f feat(arena-unity/Lát-D.U3): wire ConnectingState→LobbyState on phase=lobby + register LobbyState
1f3ce08 feat(arena-unity/Lát-D.U3): add LobbyState wiring panel events to NetClient.Send
bcc2e5f feat(arena-unity/Lát-D.U3): add LobbyPanel BasePanel with ListView + event surface
2b7879d feat(arena-unity/Lát-D.U3): add lobby.uss under UI/Resources
f907fb3 feat(arena-unity/Lát-D.U3): add LobbyPanel.uxml under UI/Resources
a7e89e7 feat(arena-unity/Lát-D.U3): extend ArenaContext with WeaponSnapshot + available_weapons hydration
8aebcf0 docs(arena-unity/Lát-D.U3): Stage 1 architect — PLAN + SUBTASKS + OPUS_PROMPTS
```

---

## Next lát: D.U4 — TurnInputPanel + drag-aim

Prereqs unblocked by D.U3a:
- ✅ State machine has a slot to add `CountdownState` + `MyTurnState` / `OpponentTurnState` / `AnimatingState`.
- ✅ `PhaseChangedEvent` plumbing covers all server-side phase transitions.
- ✅ `ArenaContext.MyPlayer.LockedWeapon` populated when server clones from available_weapons at countdown→active.
- ✅ `ShootMsg { angle, power }` type ready for `Send("shoot", ...)`.

Prereqs STILL blocked:
- ⏸ Server D.4 turn loop (`shoot`/`animation_complete` handlers) — same blocker as D.U3b.
- ⏸ Server D.5 physics sim — needed for `shot_resolved` payload smoke.

D.U4 Stage 1 can be drafted independently; smoke gated on server.
