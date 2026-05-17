#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// 3-second pre-active lock. Server flips phase=countdown after both
    /// players ready in lobby; then flips phase=active after COUNTDOWN_MS.
    /// Decides MyTurn vs OpponentTurn by reading ArenaContext.TurnPlayerId
    /// (server sets at countdown→active transition).
    /// </summary>
    public class CountdownState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;

        public override void Enter()
        {
            Debug.Log("[Arena.Countdown] Enter — server lock, awaiting phase=active");
            // D.U7 juice will add a CountdownPanel overlay with 3-2-1 big text.
            _onPhase = OnPhaseChanged;
            Bill.Events.Subscribe(_onPhase);
        }

        public override void Exit()
        {
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            _onPhase = null;
        }

        void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.newPhase != "active") return;
            var amSelf = !string.IsNullOrEmpty(ArenaContext.TurnPlayerId)
                         && ArenaContext.TurnPlayerId == ArenaContext.MyDiscordId;
            Debug.Log($"[Arena.Countdown] phase=active, turn={ArenaContext.TurnPlayerId}, mine={amSelf}");
            if (amSelf) Bill.State.GoTo<MyTurnState>();
            else Bill.State.GoTo<OpponentTurnState>();
        }
    }
}
