#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// My turn — Sub 3 skeleton. Sub 7 activates full impl:
    ///   - Open TurnInputPanel (Self mode)
    ///   - Spawn ArenaAimController GO
    ///   - Subscribe ShotReleasedEvent → NetClient.Send("shoot", ShootMsg) → GoTo&lt;AnimatingState&gt;
    /// Skeleton only listens for phase=animating fallback (server-side timeout).
    /// </summary>
    public class MyTurnState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;

        public override void Enter()
        {
            Debug.Log("[Arena.MyTurn] Enter (skeleton — Sub 7 activates panel + controller)");
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
