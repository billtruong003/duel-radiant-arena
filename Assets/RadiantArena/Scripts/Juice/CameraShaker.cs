#nullable enable
using BillGameCore;
using UnityEngine;

namespace RadiantArena.Juice
{
    /// <summary>
    /// Hand-rolled camera shake — BillTween envelope drives random offset
    /// on Camera.main.transform.position. Restores origin on tween complete.
    /// Static so subscribers can fire from anywhere without holding a reference.
    /// </summary>
    public static class CameraShaker
    {
        static Vector3 _origin;
        static bool _shaking;
        static Tween? _activeTween;

        /// <summary>Trigger a shake; intensity in world units, duration in seconds.</summary>
        public static void Shake(float intensity, float duration)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Juice.Shake] Camera.main null — skip");
                return;
            }

            // First shake captures origin; subsequent shakes within the window
            // re-use the same origin to avoid drift on rapid hits.
            if (!_shaking) _origin = cam.transform.position;
            _shaking = true;

            // Kill any in-flight shake so we don't double-jitter.
            if (_activeTween != null) BillTween.Kill(_activeTween);

            var t = cam.transform;
            _activeTween = BillTween.Float(1f, 0f, duration, env =>
            {
                if (cam == null || t == null) return;
                t.position = _origin + Random.insideUnitSphere * (intensity * env);
            });
            _activeTween?.OnComplete(() =>
            {
                _activeTween = null;
                _shaking = false;
                if (cam != null && t != null) t.position = _origin;
            });
        }
    }
}
