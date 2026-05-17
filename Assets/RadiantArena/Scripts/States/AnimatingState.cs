#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Trajectory playback phase. D.U5 will plug in TrajectoryPlayer that
    /// reads the server's shot_resolved payload, plays the projectile path,
    /// fires HP changed / hit events, then sends "animation_complete".
    ///
    /// D.U4a: stub. Just waits for the server to transition back to phase=active
    /// (or phase=ended). State machine routes to MyTurn/OpponentTurn or EndState.
    /// </summary>
    public class AnimatingState : GameState
    {
        Action<PhaseChangedEvent>? _onPhase;

        public override void Enter()
        {
            Debug.Log("[Arena.Animating] Enter — trajectory playback deferred to D.U5; idling");
            _onPhase = OnPhaseChanged;
            Bill.Events.Subscribe(_onPhase);
            // D.U5 will: TrajectoryPlayer.Play(...) and on complete send animation_complete.
        }

        public override void Exit()
        {
            if (_onPhase != null) Bill.Events.Unsubscribe(_onPhase);
            _onPhase = null;
        }

        void OnPhaseChanged(PhaseChangedEvent e)
        {
            if (e.newPhase == "active")
            {
                var amSelf = !string.IsNullOrEmpty(ArenaContext.TurnPlayerId)
                             && ArenaContext.TurnPlayerId == ArenaContext.MyDiscordId;
                Debug.Log($"[Arena.Animating] phase=active, turn={ArenaContext.TurnPlayerId}, mine={amSelf}");
                if (amSelf) Bill.State.GoTo<MyTurnState>();
                else Bill.State.GoTo<OpponentTurnState>();
            }
            else if (e.newPhase == "ended")
            {
                Debug.Log("[Arena.Animating] phase=ended (EndState deferred to D.U6)");
            }
        }
    }
}
