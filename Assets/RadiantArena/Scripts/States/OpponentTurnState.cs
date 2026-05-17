#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Opponent's turn — Sub 3 skeleton. Sub 7 activates:
    ///   - Open TurnInputPanel (Spectator mode).
    /// Skeleton listens for phase=animating to transition.
    /// </summary>
    public class OpponentTurnState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;

        public override void Enter()
        {
            Debug.Log("[Arena.OpponentTurn] Enter (skeleton — Sub 7 activates spectator panel)");
            _onPhase = e =>
            {
                if (e.newPhase == "animating") Bill.State.GoTo<AnimatingState>();
            };
            Bill.Events.Subscribe(_onPhase);
        }

        public override void Exit()
        {
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            _onPhase = null;
        }
    }
}
