#nullable enable
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.States;
using UnityEngine;

namespace RadiantArena.Bootstrap {
    public class ArenaBootstrap : MonoBehaviour {
        void Start() {
            if (Bill.IsReady) InitArena();
            else Bill.Events.SubscribeOnce<GameReadyEvent>(_ => InitArena());
        }

        void InitArena() {
            Debug.Log($"[Arena] bootstrap ready (Bill.IsReady={Bill.IsReady})");
            Bill.Events.Fire(new ArenaBootstrapReadyEvent());

            ArenaStates.Register();
            Bill.State.GoTo<RadiantArena.States.BootState>();
        }
    }
}
