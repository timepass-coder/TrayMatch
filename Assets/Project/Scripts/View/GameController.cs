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
        GameState _state;
        int _levelIndex;

        void Awake()
        {
            if (cam == null) cam = Camera.main;
            _board = new GameObject("Board").AddComponent<BoardView>();
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
                        _board.RemoveBlocks(left.Blocks);
                        break;

                    case BumpedEvent bump:
                        Debug.Log($"Bumped into block {bump.Blocker} (no move used)");
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

            Debug.Log($"Moves left {_state.MovesLeft} | tray [{string.Join(", ", _state.Tray)}] ({_state.Tray.Count}/{_state.TraySize})");
        }

        void FitCamera()
        {
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Background;

            // Room around the board: 1 cell at the sides, space above for the HUD (step 4c)
            // and below for the tray (step 4b).
            float neededWidth = _state.Width + 1f;
            float neededHeight = _state.Height + 4f;

            cam.orthographicSize = Mathf.Max(neededHeight / 2f, neededWidth / 2f / cam.aspect);
            cam.transform.position = new Vector3(0, 0, -10);

            _board.transform.position = new Vector3(0, 0.5f, 0);
        }
    }
}