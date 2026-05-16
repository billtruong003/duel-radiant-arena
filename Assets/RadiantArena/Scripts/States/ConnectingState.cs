#nullable enable
using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;

namespace RadiantArena.States
{
    /// <summary>
    /// Waits for NetClient.ConnectAsync to fire NetConnectedEvent or
    /// NetErrorEvent. D.U2a stops here — D.U3 will add LobbyState transition
    /// on PhaseChangedEvent { newPhase="lobby" }.
    /// </summary>
    public class ConnectingState : GameState
    {
        Action<NetConnectedEvent>? _onConnected;
        Action<NetErrorEvent>? _onError;

        public override void Enter()
        {
            Debug.Log("[Arena.Connecting] Waiting for NetConnectedEvent / NetErrorEvent ...");

            _onConnected = e =>
            {
                Debug.Log($"[Arena.Connecting] Connected sessionId={e.sessionId} roomId={e.roomId}");
                // D.U3 will GoTo<LobbyState>() here.
            };
            _onError = e =>
            {
                Debug.LogWarning($"[Arena.Connecting] NetErrorEvent code={e.code} message={e.message}");
                // D.U3+ will route to an error-screen state.
            };
            Bill.Events.Subscribe(_onConnected);
            Bill.Events.Subscribe(_onError);

            // Auto-connect if a URL-derived ConnectionInfo is sitting in CurrentInfo
            // already (production WebGL flow). Editor path goes through
            // ManualRoomConnect which calls NetClient.ConnectAsync directly.
            var nc = NetClient.Instance;
            if (nc != null && !nc.IsConnected && nc.CurrentInfo.IsValid())
            {
                _ = nc.ConnectAsync(nc.CurrentInfo);
            }
        }

        public override void Exit()
        {
            if (_onConnected != null) Bill.Events.Unsubscribe(_onConnected);
            if (_onError != null) Bill.Events.Unsubscribe(_onError);
            _onConnected = null;
            _onError = null;
        }
    }
}
