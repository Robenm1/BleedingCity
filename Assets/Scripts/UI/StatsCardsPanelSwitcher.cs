using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class StatsCardsPanelSwitcher : MonoBehaviour
{
    public enum PanelView
    {
        Stats,
        Cards
    }

    [Header("Views")]
    [Tooltip("Your old stats UI content/view. Put it exactly where it should appear when TAB opens.")]
    public RectTransform statsView;

    [Tooltip("The cards panel/view. Put it exactly where it should be hidden/offscreen at start.")]
    public RectTransform cardsView;

    [Header("Switch Button")]
    public RectTransform switchButton;
    public Button switchButtonComponent;
    public Image switchButtonIcon;

    [Tooltip("Icon shown when clicking will open the Cards view.")]
    public Sprite cardsIcon;

    [Tooltip("Icon shown when clicking will return to the Stats view.")]
    public Sprite statsIcon;

    [Header("Auto Capture Positions")]
    [Tooltip("If true, uses your current Unity positions as the correct start layout.")]
    public bool autoCapturePositions = true;

    [Tooltip("If true, StatsView moves left using the same distance CardsView is currently offscreen to the right.")]
    public bool useCardsOffsetForStatsLeft = true;

    [Tooltip("Extra distance added when moving StatsView left. Use this if a piece is still visible.")]
    public float extraStatsLeftDistance = 0f;

    [Tooltip("If true, button left position mirrors around the real StatsView center.")]
    public bool mirrorButtonAroundStatsCenter = true;

    [Tooltip("Extra distance added to the button left movement.")]
    public float extraButtonLeftDistance = 0f;

    [Header("Manual Positions - used only if Auto Capture Positions is OFF")]
    public Vector2 statsCenter = Vector2.zero;
    public Vector2 statsOffLeft = new Vector2(-900f, 0f);

    public Vector2 cardsCenter = Vector2.zero;
    public Vector2 cardsOffRight = new Vector2(900f, 0f);

    public Vector2 buttonRight = new Vector2(420f, 0f);
    public Vector2 buttonLeft = new Vector2(-420f, 0f);

    [Header("Animation")]
    public float moveDuration = 0.35f;

    [Range(0f, 1f)]
    public float iconSwapProgress = 0.5f;

    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Behavior")]
    public bool resetToStatsWhenOpened = true;

    [Header("Debug")]
    public bool showDebug = false;

    private PanelView _currentView = PanelView.Stats;
    private bool _isMoving;
    private bool _captured;
    private Coroutine _routine;

    private void Awake()
    {
        CapturePositionsIfNeeded();

        if (switchButtonComponent != null)
            switchButtonComponent.onClick.AddListener(TogglePanelView);

        SetVisibleWithStatsPanel(false);
    }

    private void OnEnable()
    {
        CapturePositionsIfNeeded();

        if (resetToStatsWhenOpened)
            ShowStatsInstant();
    }

    private void OnDestroy()
    {
        if (switchButtonComponent != null)
            switchButtonComponent.onClick.RemoveListener(TogglePanelView);
    }

    private void CapturePositionsIfNeeded()
    {
        if (_captured)
            return;

        if (!autoCapturePositions)
        {
            _captured = true;
            return;
        }

        if (statsView == null || cardsView == null || switchButton == null)
            return;

        // Current stats position = correct visible stats position.
        statsCenter = statsView.anchoredPosition;

        // Current cards position = correct hidden/offscreen cards position.
        cardsOffRight = cardsView.anchoredPosition;

        // Cards visible center = same place as stats visible center.
        cardsCenter = statsCenter;

        // How far cards are currently placed away from center.
        Vector2 cardsOffsetFromCenter = cardsOffRight - cardsCenter;

        if (useCardsOffsetForStatsLeft)
        {
            statsOffLeft = statsCenter - cardsOffsetFromCenter;

            if (extraStatsLeftDistance > 0f)
                statsOffLeft += Vector2.left * extraStatsLeftDistance;
        }

        // Current button position = correct right-side button position.
        buttonRight = switchButton.anchoredPosition;

        if (mirrorButtonAroundStatsCenter)
        {
            float rightDistanceFromCenter = buttonRight.x - statsCenter.x;
            buttonLeft = new Vector2(statsCenter.x - rightDistanceFromCenter, buttonRight.y);

            if (extraButtonLeftDistance > 0f)
                buttonLeft += Vector2.left * extraButtonLeftDistance;
        }

        _captured = true;

        if (showDebug)
        {
            Debug.Log(
                "[StatsCardsPanelSwitcher] Captured UI positions:\n" +
                $"Stats Center: {statsCenter}\n" +
                $"Stats Off Left: {statsOffLeft}\n" +
                $"Cards Center: {cardsCenter}\n" +
                $"Cards Off Right: {cardsOffRight}\n" +
                $"Button Right: {buttonRight}\n" +
                $"Button Left: {buttonLeft}"
            );
        }
    }

    public void SetVisibleWithStatsPanel(bool visible)
    {
        if (visible)
        {
            if (statsView != null)
                statsView.gameObject.SetActive(true);

            if (cardsView != null)
                cardsView.gameObject.SetActive(true);

            if (switchButton != null)
                switchButton.gameObject.SetActive(true);

            ShowStatsInstant();
        }
        else
        {
            StopMovement();

            if (statsView != null)
                statsView.gameObject.SetActive(false);

            if (cardsView != null)
                cardsView.gameObject.SetActive(false);

            if (switchButton != null)
                switchButton.gameObject.SetActive(false);
        }
    }

    public void TogglePanelView()
    {
        if (_isMoving)
            return;

        if (_currentView == PanelView.Stats)
            ShowCards();
        else
            ShowStats();
    }

    public void ShowCards()
    {
        MoveTo(PanelView.Cards);
    }

    public void ShowStats()
    {
        MoveTo(PanelView.Stats);
    }

    private void MoveTo(PanelView target)
    {
        CapturePositionsIfNeeded();

        if (_currentView == target)
            return;

        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(MoveRoutine(target));
    }

    private IEnumerator MoveRoutine(PanelView target)
    {
        _isMoving = true;

        Vector2 statsStart = statsView != null ? statsView.anchoredPosition : Vector2.zero;
        Vector2 cardsStart = cardsView != null ? cardsView.anchoredPosition : Vector2.zero;
        Vector2 buttonStart = switchButton != null ? switchButton.anchoredPosition : Vector2.zero;

        Vector2 statsEnd;
        Vector2 cardsEnd;
        Vector2 buttonEnd;
        Sprite finalIcon;

        if (target == PanelView.Cards)
        {
            statsEnd = statsOffLeft;
            cardsEnd = cardsCenter;
            buttonEnd = buttonLeft;
            finalIcon = statsIcon;
        }
        else
        {
            statsEnd = statsCenter;
            cardsEnd = cardsOffRight;
            buttonEnd = buttonRight;
            finalIcon = cardsIcon;
        }

        bool iconChanged = false;
        float timer = 0f;
        float duration = Mathf.Max(0.01f, moveDuration);

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float rawT = Mathf.Clamp01(timer / duration);
            float t = moveCurve != null ? moveCurve.Evaluate(rawT) : rawT;

            if (statsView != null)
                statsView.anchoredPosition = Vector2.LerpUnclamped(statsStart, statsEnd, t);

            if (cardsView != null)
                cardsView.anchoredPosition = Vector2.LerpUnclamped(cardsStart, cardsEnd, t);

            if (switchButton != null)
                switchButton.anchoredPosition = Vector2.LerpUnclamped(buttonStart, buttonEnd, t);

            if (!iconChanged && rawT >= iconSwapProgress)
            {
                SetIcon(finalIcon);
                iconChanged = true;
            }

            yield return null;
        }

        if (statsView != null)
            statsView.anchoredPosition = statsEnd;

        if (cardsView != null)
            cardsView.anchoredPosition = cardsEnd;

        if (switchButton != null)
            switchButton.anchoredPosition = buttonEnd;

        SetIcon(finalIcon);

        _currentView = target;
        _isMoving = false;
        _routine = null;
    }

    public void ShowStatsInstant()
    {
        CapturePositionsIfNeeded();
        StopMovement();

        if (statsView != null)
            statsView.anchoredPosition = statsCenter;

        if (cardsView != null)
            cardsView.anchoredPosition = cardsOffRight;

        if (switchButton != null)
            switchButton.anchoredPosition = buttonRight;

        SetIcon(cardsIcon);

        _currentView = PanelView.Stats;
        _isMoving = false;
    }

    public void ShowCardsInstant()
    {
        CapturePositionsIfNeeded();
        StopMovement();

        if (statsView != null)
            statsView.anchoredPosition = statsOffLeft;

        if (cardsView != null)
            cardsView.anchoredPosition = cardsCenter;

        if (switchButton != null)
            switchButton.anchoredPosition = buttonLeft;

        SetIcon(statsIcon);

        _currentView = PanelView.Cards;
        _isMoving = false;
    }

    private void StopMovement()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        _isMoving = false;
    }

    private void SetIcon(Sprite icon)
    {
        if (switchButtonIcon == null)
            return;

        switchButtonIcon.sprite = icon;
        switchButtonIcon.enabled = icon != null;
    }
}