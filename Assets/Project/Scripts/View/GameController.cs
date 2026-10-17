using System.Collections.Generic;
using TrayMatch.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TrayMatch.View
{
    // Step 4a: loads a level, shows the board and turns taps into GameState.Tap().
    // There's no tray view or animation yet: results are written to the Console.
    // Testing keys: R = restart, N = next level, P = previous level.
    public sealed class GameController : MonoBehaviour
    {
        [SerializeField] TextAsset[] levels;
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
        }

        void Start() => StartLevel(0);

        void StartLevel(int index)
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("GameController: no level files assigned in the Inspector.");
                return;
            }

            _levelIndex = (index % levels.Length + levels.Length) % levels.Length;
            var level = LevelSerializer.FromJson(levels[_levelIndex].text);
            _state = new GameState(level);
            _board.Build(_state);
            _tray.Build(_state.TraySize)
            FitCamera();

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

            var pointer = Pointer.current; // mouse in the editor, touch on a phone
            if (_state == null || pointer == null || !pointer.press.wasPressedThisFrame) return;

            Vector2 screen = pointer.position.ReadValue();
            Vector3 world = cam.ScreenToWorldPoint(
                new Vector3(screen.x, screen.y, -cam.transform.position.z));

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

                    case TripleClearedEvent triple:
                        Debug.Log($"Triple cleared: color {triple.Color}");
                        break;

                    case WonEvent _:
                        Debug.Log("WON! Press N for the next level.");
                        break;

                    case LostEvent lost:
                        Debug.Log($"LOST: {lost.Reason}. Press R to retry.");
                        break;
                }
            }
            _tray.SetDanger(_state.Status == GameStatus.Playing && _state.Tray.Count >= _state.TraySize -1);
            Debug.Log($"Moves left {_state.MovesLeft} | tray [{string.Join(", ", _state.Tray)}] ({_state.Tray.Count}/{_state.TraySize})");
        }

        void FitCamera()
        {
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;

            // Room around the board: 1 cell at the sides, space above for the HUD (step 4c)
            // and below for the tray (step 4b).
            float neededWidth = Mathf.Max(_state.Width,_state.TraySize*TrayView.SlotSize)+ 1f;
            float neededHeight = _state.Height + 4f;

            cam.orthographicSize = Mathf.Max(neededHeight / 2f, neededWidth / 2f / cam.aspect);
            cam.transform.position = new Vector3(0, 0, -10);

            _board.transform.position = new Vector3(0, 0.5f, 0);
            _tray.transform.posistion = new Vector3(0,0.5f - _state.Height/2f -1f, 0);
        }
    }
}