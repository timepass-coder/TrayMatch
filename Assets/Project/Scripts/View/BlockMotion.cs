
using System;
using TrayMatch.Core;
using UnityEngine;

namespace TrayMatch.View
{
    // Animations of one board block: pop-in at level start, bump, wiggle when hit, and the
    // slide-out + swoop into the tray. Timings and curves are the web prototype's.
    // Distances are in world units (1 cell = 1 unit).
    public sealed class BlockMotion : MonoBehaviour
    {
        const float SpawnTime = 0.3f;
        const float BumpReturnTime = 0.2f;
        const float HitTime = 0.45f;
        const float FlyTime = 0.3f;
        const float SwoopOvershoot = 1.8f;
        const int FlyingSortingBoost = 100;

        Vector3 _home;       // local position on the board
        Vector3 _dir;        // arrow direction in world space
        SpriteRenderer _arrow;

        bool _spawning;
        float _spawnDelay, _spawnTime;

        float _bumpTime = -1f, _bumpDistance, _bumpOutTime;

        bool _hitActive;
        float _hitTime;
        Vector3 _hitDir;

        bool _launched, _landedCalled;
        float _time, _distance, _maxDistance, _rigidTime, _edgeTime, _lag;
        Vector3 _startWorld;
        Func<Vector3> _target;
        Action _onLanded;

        // Time the bump takes to reach the blocker; the blocker starts wiggling then.
        public static float BumpOutTime(float distance) => 0.04f + 0.035f * distance;

        public void Init(Direction dir, float spawnDelay)
        {
            _home = transform.localPosition;
            _dir = new Vector3(dir.Dx(), -dir.Dy(), 0); // grid y grows down, world y grows up
            var arrow = transform.Find("Arrow");
            _arrow = arrow != null ? arrow.GetComponent<SpriteRenderer>() : null;
            _spawnDelay = spawnDelay;
            _spawning = true;
            transform.localScale = Vector3.zero;
        }

        // Slide forward `distance` cells and come back (the path is blocked).
        public void Bump(float distance)
        {
            if (_launched) return;
            _bumpDistance = distance;
            _bumpOutTime = BumpOutTime(distance);
            _bumpTime = 0f;
        }

        // Another block bumped into this one, coming from direction `from`.
        public void Hit(Direction from, float delay)
        {
            if (_launched) return;
            _hitDir = new Vector3(from.Dx(), -from.Dy(), 0);
            _hitTime = -delay;
            _hitActive = true;
        }

        // Leave the board: accelerate `distance` cells to the edge, then swoop to target().
        // All blocks of a train get the same maxDistance, so the train moves as one piece.
        public void Launch(
            float distance,
            float maxDistance,
            float lag,
            Func<Vector3> target,
            Action onLanded)
        {
            _launched = true;
            _spawning = false;
            _bumpTime = -1f;
            _hitActive = false;
            transform.localScale = Vector3.one;

            _startWorld = transform.parent.TransformPoint(_home);
            _distance = distance;
            _maxDistance = Mathf.Max(distance, maxDistance);
            _rigidTime = 0.08f + 0.03f * _maxDistance;
            _edgeTime = _rigidTime * Mathf.Sqrt(distance / _maxDistance);
            _lag = lag;
            _target = target;
            _onLanded = onLanded;
            _time = 0f;

            foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.sortingOrder += FlyingSortingBoost;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (_launched)
            {
                UpdateLaunch(dt);
                return;
            }

            if (_spawning)
            {
                _spawnTime += dt;
                float k = Mathf.Clamp01((_spawnTime - _spawnDelay) / SpawnTime);
                transform.localScale = Vector3.one * (k <= 0f ? 0f : Ease.OutBack(k));

                if (k >= 1f)
                {
                    _spawning = false;
                    transform.localScale = Vector3.one;
                }
            }

            Vector3 offset = Vector3.zero;

            if (_bumpTime >= 0f)
            {
                _bumpTime += dt;
                float d = _bumpTime < _bumpOutTime
                    ? _bumpDistance * Square(_bumpTime / _bumpOutTime)
                    : _bumpDistance * (1f - Ease.OutCubic(
                        Mathf.Min(1f, (_bumpTime - _bumpOutTime) / BumpReturnTime)));

                offset += _dir * d;

                if (_bumpTime > _bumpOutTime + BumpReturnTime)
                    _bumpTime = -1f;
            }

            if (_hitActive)
            {
                _hitTime += dt;

                if (_hitTime > 0f)
                    offset += _hitDir * (0.14f * Mathf.Sin(_hitTime * 30f) * Mathf.Exp(-_hitTime * 9f));

                if (_hitTime > HitTime)
                    _hitActive = false;
            }

            transform.localPosition = _home + offset;
        }

        void UpdateLaunch(float dt)
        {
            if (_landedCalled) return;

            _time += dt;

            if (_time < _edgeTime)
            {
                float d = _maxDistance * Square(_time / _rigidTime);
                transform.position = _startWorld + _dir * d;
                return;
            }

            float k = Ease.InOutCubic(
                Mathf.Clamp01((_time - _edgeTime - _lag) / FlyTime));

            Vector3 edge = _startWorld + _dir * _distance;
            Vector3 control = edge + _dir * SwoopOvershoot;

            transform.position = Bezier(edge, control, _target(), k);
            transform.localScale = Vector3.one *
                Mathf.Lerp(1f, TrayView.ItemSize / BlockView.Size, k);

            if (_arrow != null)
            {
                Color c = _arrow.color;
                c.a = 1f - k;
                _arrow.color = c;
            }

            if (_time >= _edgeTime + _lag + FlyTime)
            {
                _landedCalled = true;
                _onLanded();
                Destroy(gameObject);
            }
        }

        static float Square(float x) => x * x;

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float k) =>
            (1f - k) * (1f - k) * a
            + 2f * (1f - k) * k * b
            + k * k * c;
    }
}
