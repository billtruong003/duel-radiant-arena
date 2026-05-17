#nullable enable
// TurnInputPanel — single BasePanel rendering both my-turn and spectator-turn UI.
// Switches via SetMode(TurnMode) which toggles a .spectator class on the root.
//
// Self mode: shows power gauge that follows AimUpdatedEvent. Hint reads
// "Kéo để nhắm, thả để bắn".
// Spectator mode: power gauge hidden via USS, hint reads "Đối thủ đang đánh…".
//
// Timer always shown — driven by VisualElement scheduler (250ms tick) reading
// ArenaContext.TurnDeadlineAt (epoch ms). UI displays remaining seconds; server
// enforces the actual timeout.

using System;
using BillGameCore;
using RadiantArena.Events;
using RadiantArena.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace RadiantArena.UI
{
    public enum TurnMode { Self, Spectator }

    public class TurnInputPanel : BasePanel
    {
        VisualElement? _root;
        Label? _title;
        Label? _timer;
        Label? _hint;
        Label? _powerValue;
        VisualElement? _powerFill;

        TurnMode _mode = TurnMode.Self;
        float _currentPower;

        Action<AimUpdatedEvent>? _onAimUpdated;
        Action<AimClearedEvent>? _onAimCleared;
        IVisualElementScheduledItem? _tick;

        protected override void Build(VisualElement root)
        {
            _root = root;

            var tree = Resources.Load<VisualTreeAsset>("TurnInputPanel");
            if (tree == null)
            {
                Debug.LogError("[Arena.TurnInput] TurnInputPanel.uxml not found in Resources/");
                return;
            }
            tree.CloneTree(root);

            var uss = Resources.Load<StyleSheet>("turn_input");
            if (uss != null) root.styleSheets.Add(uss);

            _title = root.Q<Label>("title");
            _timer = root.Q<Label>("timer");
            _hint = root.Q<Label>("hint");
            _powerValue = root.Q<Label>("power-value");
            _powerFill = root.Q<VisualElement>("power-fill");

            if (_powerFill != null) _powerFill.style.height = new StyleLength(Length.Percent(0));
        }

        public void SetMode(TurnMode mode)
        {
            _mode = mode;
            if (_root == null) return;
            _root.EnableInClassList("spectator", mode == TurnMode.Spectator);
            if (_title != null) _title.text = mode == TurnMode.Self ? "LƯỢT CỦA TÔI" : "LƯỢT ĐỐI THỦ";
            if (_hint != null) _hint.text = mode == TurnMode.Self ? "Kéo để nhắm, thả để bắn" : "Đối thủ đang đánh…";
        }

        public override void OnOpened()
        {
            if (_mode == TurnMode.Self)
            {
                _onAimUpdated = OnAimUpdated;
                _onAimCleared = OnAimCleared;
                Bill.Events.Subscribe(_onAimUpdated);
                Bill.Events.Subscribe(_onAimCleared);
            }
            _currentPower = 0f;
            UpdatePowerVisual();
            _tick = _root?.schedule.Execute(RefreshTimer).Every(250);
            RefreshTimer();
        }

        public override void OnClosed()
        {
            if (_onAimUpdated != null) Bill.Events.Unsubscribe(_onAimUpdated);
            if (_onAimCleared != null) Bill.Events.Unsubscribe(_onAimCleared);
            _onAimUpdated = null;
            _onAimCleared = null;
            _tick?.Pause();
            _tick = null;
        }

        void OnAimUpdated(AimUpdatedEvent e)
        {
            _currentPower = Mathf.Clamp01(e.power);
            UpdatePowerVisual();
        }

        void OnAimCleared(AimClearedEvent _)
        {
            _currentPower = 0f;
            UpdatePowerVisual();
        }

        void UpdatePowerVisual()
        {
            if (_powerFill != null)
            {
                _powerFill.style.height = new StyleLength(Length.Percent(_currentPower * 100f));

                Color c;
                if (_currentPower < 0.5f)
                    c = Color.Lerp(new Color(0.3f, 0.85f, 0.4f), new Color(1f, 0.85f, 0.2f), _currentPower * 2f);
                else
                    c = Color.Lerp(new Color(1f, 0.85f, 0.2f), new Color(1f, 0.35f, 0.25f), (_currentPower - 0.5f) * 2f);
                _powerFill.style.backgroundColor = c;
            }
            if (_powerValue != null) _powerValue.text = $"{(int)(_currentPower * 100f)}%";
        }

        void RefreshTimer()
        {
            if (_timer == null) return;
            var deadline = ArenaContext.TurnDeadlineAt;
            if (deadline <= 0)
            {
                _timer.text = "—";
                _timer.EnableInClassList("timer-urgent", false);
                return;
            }
            var nowMs = (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalMilliseconds;
            var remainMs = System.Math.Max(0L, deadline - nowMs);
            var seconds = (int)(remainMs / 1000);
            _timer.text = $"{seconds}s";
            _timer.EnableInClassList("timer-urgent", seconds <= 5 && seconds > 0);
        }
    }
}
