
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TrayMatch.View
{
    // Top bar (level, moves left), bottom bar (prev / restart / next) and the end-of-level panel.
    // The UI objects are built in the scene and dragged into these fields in the Inspector.
    public sealed class HudView : MonoBehaviour
    {
        [Header("Top bar")]
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text movesText;

        [Header("Bottom bar")]
        [SerializeField] Button prevButton;
        [SerializeField] Button restartButton;
        [SerializeField] Button nextButton;

        [Header("End panel")]
        [SerializeField] GameObject endPanel;
        [SerializeField] TMP_Text endTitle;
        [SerializeField] TMP_Text endSubtitle;
        [SerializeField] Button primaryButton;
        [SerializeField] TMP_Text primaryLabel;
        [SerializeField] Button secondaryButton;
        [SerializeField] TMP_Text secondaryLabel;

        const int LowMoves = 3;
        static readonly Color LowMovesColor = new Color(1f, 0.42f, 0.42f, 1f);

        public event Action PrevPressed;
        public event Action RestartPressed;
        public event Action NextPressed;

        Action _onPrimary, _onSecondary;

        void Awake()
        {
            if (!AllAssigned()) return;
            prevButton.onClick.AddListener(() => PrevPressed?.Invoke());
            restartButton.onClick.AddListener(() => RestartPressed?.Invoke());
            nextButton.onClick.AddListener(() => NextPressed?.Invoke());
            primaryButton.onClick.AddListener(() => _onPrimary?.Invoke());
            secondaryButton.onClick.AddListener(() => _onSecondary?.Invoke());
            HideEnd();
        }

        public bool IsEndVisible => endPanel != null && endPanel.activeSelf;

        public void SetLevel(int id)
        {
            if (levelText != null) levelText.text = $"Level {id}";
        }

        public void SetMoves(int movesLeft)
        {
            if (movesText == null) return;
            movesText.text = $"Moves {movesLeft}";
            movesText.color = movesLeft <= LowMoves ? LowMovesColor : Color.white;
        }

        // secondary may be null: then only the primary button is shown.
        public void ShowEnd(string title, string subtitle, string primary, Action onPrimary,
            string secondary = null, Action onSecondary = null)
        {
            if (!AllAssigned()) return;
            endTitle.text = title;
            endSubtitle.text = subtitle;
            primaryLabel.text = primary;
            _onPrimary = onPrimary;
            secondaryButton.gameObject.SetActive(secondary != null);
            if (secondary != null) secondaryLabel.text = secondary;
            _onSecondary = onSecondary;
            endPanel.SetActive(true);
        }

        public void HideEnd()
        {
            if (endPanel != null) endPanel.SetActive(false);
        }

        // Names any field that wasn't dragged in, instead of failing with a NullReferenceException.
        bool AllAssigned()
        {
            var missing = new System.Collections.Generic.List<string>();
            if (levelText == null) missing.Add(nameof(levelText));
            if (movesText == null) missing.Add(nameof(movesText));
            if (prevButton == null) missing.Add(nameof(prevButton));
            if (restartButton == null) missing.Add(nameof(restartButton));
            if (nextButton == null) missing.Add(nameof(nextButton));
            if (endPanel == null) missing.Add(nameof(endPanel));
            if (endTitle == null) missing.Add(nameof(endTitle));
            if (endSubtitle == null) missing.Add(nameof(endSubtitle));
            if (primaryButton == null) missing.Add(nameof(primaryButton));
            if (primaryLabel == null) missing.Add(nameof(primaryLabel));
            if (secondaryButton == null) missing.Add(nameof(secondaryButton));
            if (secondaryLabel == null) missing.Add(nameof(secondaryLabel));

            if (missing.Count == 0) return true;
            Debug.LogError($"HudView: not assigned in the Inspector: {string.Join(", ", missing)}", this);
            return false;
        }
    }
}
