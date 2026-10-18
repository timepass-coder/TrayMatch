
using System;
using System.Collections;
using System.Collections.Generic;
using TrayMatch.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrayMatch.View
{
    // Loads a level, shows board, tray and HUD, turns taps into GameState.Tap() and lets the views
    // animate the resulting events. Testing keys: R = restart, N = next level, P = previous level.
    public sealed class GameController : MonoBehaviour
    {
        // Pause before the end panel appears, so the last animation can finish (prototype timings).
        const float WinPanelDelay = 0.9f;
        const float LosePanelDelay = 0.75f;

        [SerializeField] TextAsset[] levels;
        [SerializeField] HudView hud;
        [SerializeField] Camera cam;

        BoardView _board;
        TrayView _tray;
        GameState _state;
        int _levelIndex;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            _board = new GameObject("Board").AddComponent<BoardView>();
            _tray = new GameObject("Tray").AddComponent<TrayView>();

            if (hud == null)
            {
                Debug.LogError("GameController: the Hud field is not assigned in the Inspector.");
                return;
            }

            hud.PrevPressed += () => StartLevel(_levelIndex - 1);
            hud.RestartPressed += () => StartLevel(_levelIndex);
            hud.NextPressed += () => StartLevel(_levelIndex + 1);
        }

        void Start() => StartLevel(0);

        void StartLevel(int index)
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("GameController: no level files assigned in the Inspector.");
                return;
            }

            StopAllCoroutines(); // an end panel still waiting to appear from the previous level
            _levelIndex = (index % levels.Length + levels.Length) % levels.Length;
            var level = LevelSerializer.FromJson(levels[_levelIndex].text);
            _state = new GameState(level);
            _board.Build(_state);
            _tray.Build(_state.TraySize);
            FitCamera();

            if (hud != null)
            {
                hud.HideEnd();
                hud.SetLevel(level.Id);
                hud.SetMoves(_state.MovesLeft);
            }

            Debug.Log($"Level {level.Id}: {_state.Blocks.Count} blocks, {_state.MovesLeft} moves, tray size {_state.TraySize}");
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame) StartLevel(_levelIndex);
                if (keyboard.nKey.wasPressedThisFrame) StartLevel(_levelIndex + 1);
                if (keyboard.pKey.wasPressedThisFrame) StartLevel(_levelIndex - 1);
            }

            var pointer = Pointer.current;
            if (_state == null || pointer == null || !pointer.press.wasPressedThisFrame) return;
            if (hud != null && hud.IsEndVisible) return; // taps belong to the end panel's buttons

            Vector2 screen = pointer.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            if (_board.TryWorldToCell(world, out int x, out int y))
                HandleEvents(_state.Tap(x, y));
        }

        void HandleEvents(IReadOnlyList<GameEvent> events)
        {
            if (events.Count == 0) return;

            foreach (var e in events)
            {
                switch (e)
                {
                    case LeftEvent left:
                        _board.LaunchToTray(left.Blocks, _tray);
                        break;

                    case BumpedEvent bump:
                        _board.Bump(bump.Blocks, bump.Blocker, bump.Steps);
                        break;

                    case WonEvent _:
                        StartCoroutine(ShowEndAfter(WinPanelDelay, ShowWin));
                        break;

                    case LostEvent lost:
                        var reason = lost.Reason;
                        StartCoroutine(ShowEndAfter(LosePanelDelay, () => ShowLose(reason)));
                        break;

                    case RevivedEvent _:
                        _tray.SetSize(_state.TraySize);
                        FitCamera();
                        if (hud != null) hud.HideEnd();
                        break;

                    // TripleClearedEvent: the tray pops triples itself when they land (sound in step 4d).
                }
            }

            _tray.SetDanger(_state.Status == GameStatus.Playing && _state.Tray.Count >= _state.TraySize - 1);
            if (hud != null) hud.SetMoves(_state.MovesLeft);
            Debug.Log($"Moves left {_state.MovesLeft} | tray [{string.Join(", ", _state.Tray)}] ({_state.Tray.Count}/{_state.TraySize})");
        }

        IEnumerator ShowEndAfter(float delay, Action show)
        {
            yield return new WaitForSeconds(delay);
            show();
        }

        void ShowWin()
        {
            if (hud == null) return;
            string spare = _state.MovesLeft == 1 ? "1 move to spare" : $"{_state.MovesLeft} moves to spare";
            hud.ShowEnd("Level Complete!", spare, "Next", () => StartLevel(_levelIndex + 1));
        }

        void ShowLose(LoseReason reason)
        {
            if (hud == null) return;
            if (reason == LoseReason.TrayFull && _state.CanRevive)
                hud.ShowEnd("Tray full!", "Out of space", "+1 Slot & Continue", Revive, "Retry", () => StartLevel(_levelIndex));
            else if (reason == LoseReason.TrayFull)
                hud.ShowEnd("Tray full!", "Out of space", "Retry", () => StartLevel(_levelIndex));
            else
                hud.ShowEnd("Out of moves!", "Try sending more blocks at once", "Retry", () => StartLevel(_levelIndex));
        }

        // "+1 Slot & Continue". In the real game a rewarded ad plays first.
        void Revive() => HandleEvents(_state.Revive());

        void FitCamera()
        {
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;

            // Room around the board: 1 cell at the sides, space above for the HUD's top bar and
            // below the tray for its bottom bar.
            float neededWidth = Mathf.Max(_state.Width, _state.TraySize * TrayView.SlotSize) + 1f;
            float neededHeight = _state.Height + 5f;
            cam.orthographicSize = Mathf.Max(neededHeight / 2f, neededWidth / 2f / cam.aspect);
            cam.transform.position = new Vector3(0, 0, -10);

            _board.transform.position = new Vector3(0, 0.5f, 0);
            _tray.transform.position = new Vector3(0, 0.5f - _state.Height / 2f - 1f, 0);
        }
    }
}
