using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class Roflan : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI clickUpgradeText;
    public TextMeshProUGUI autoClickUpgradeText;
    public Button clickButton;
    public RectTransform clickPopTarget; // назначьте в инспекторе маленький элемент (иконку кнопки) или оставьте null
    public Button clickUpgradeButton;
    public Button autoClickUpgradeButton;
    public Button doubleCoinsButton;
    public Button adButton;
    public Image xpBar;
    public TextMeshProUGUI doubleCoinsButtonText;
    public ClickAnimator clickAnimator; // ссылка на ClickAnimator

    private int score = 0;
    private int level = 1;
    private int xp = 0;
    private int xpToNextLevel = 100;
    private int clickPower = 1;
    private int autoClickPower = 0;
    private int clickUpgradeCost = 100;
    private int autoClickUpgradeCost = 150;
    private int currentSpriteIndex = 0;

    public Sprite[] clickButtonSprites;

    private bool doubleCoinsActive = false;
    private float doubleCoinsDuration = 30f;
    // текущий множитель, который будет применён при активации (2, 4, 8...)
    private int doubleMultiplier = 2;
    // множитель, который сейчас активен (1 если нет активного буста)
    private int activeMultiplier = 1;

    // кешы
    private RectTransform clickButtonRect;

    // ключи PlayerPrefs
    private const string KEY_SCORE = "Score";
    private const string KEY_LEVEL = "Level";
    private const string KEY_DOUBLE_MULTIPLIER = "DoubleMultiplier";
    private const string KEY_DOUBLE_ACTIVE = "DoubleCoinsActive";
    private const string KEY_ACTIVE_MULT = "ActiveMultiplier";

    void Start()
    {
        LoadProgress();
        UpdateUI();
        if (clickButton != null)
            clickButton.onClick.AddListener(OnClick);
        else
            Debug.LogWarning("Click button не назначена в инспекторе.");

        clickButtonRect = clickButton != null ? clickButton.GetComponent<RectTransform>() : null;

        if (clickUpgradeButton != null) clickUpgradeButton.onClick.AddListener(BuyClickUpgrade);
        if (autoClickUpgradeButton != null) autoClickUpgradeButton.onClick.AddListener(BuyAutoClickUpgrade);
        if (doubleCoinsButton != null) doubleCoinsButton.onClick.AddListener(ActivateDoubleCoins);
        if (adButton != null) adButton.onClick.AddListener(ShowAdForDoubleCoins);

        InvokeRepeating("AutoClick", 1f, 1f);
        Invoke("ForceSetSprite", 0.1f); // задержка после старта

        // передаём камеру в ClickAnimator, если нужно
        if (clickAnimator != null && clickAnimator.renderCamera == null)
            clickAnimator.renderCamera = Camera.main;
    }

    void OnEnable()
    {
        YandexGame.RewardVideoEvent += Rewarded;
    }

    void OnDisable()
    {
        YandexGame.RewardVideoEvent -= Rewarded;
    }

    void OnClick()
    {
        int coinsToAdd = clickPower * activeMultiplier;
        score += coinsToAdd;
        xp += clickPower;

        if (xp >= xpToNextLevel)
        {
            LevelUp();
        }

        // проиграть анимацию "+N" и pop кнопки (если настроено)
        if (clickAnimator != null && clickButtonRect != null)
        {
            string plusText = "+" + coinsToAdd.ToString();
            // передаём отдельную цель для "pop" (clickPopTarget). Если не назначено — pop не выполняется.
            clickAnimator.Play(plusText, clickButtonRect.position, clickPopTarget);
        }
        else if (clickAnimator == null)
        {
            Debug.LogWarning("ClickAnimator не назначен — анимация клика не проигрывается.");
        }

        UpdateUI();
        SaveProgress();
    }

    // пустой заглушечный метод (если раньше был)
    void ForceSetSprite() { /* при необходимости */ }

    void LevelUp()
    {
        level++;
        xp = 0;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.5f);
        if (level - 1 < clickButtonSprites.Length)
        {
            currentSpriteIndex = level - 1;
            if (clickButton != null && clickButtonSprites[currentSpriteIndex] != null)
                clickButton.image.sprite = clickButtonSprites[currentSpriteIndex];
        }
        SaveProgress();
    }

    void BuyClickUpgrade()
    {
        if (score >= clickUpgradeCost)
        {
            score -= clickUpgradeCost;
            clickPower++;
            clickUpgradeCost *= 2;
            UpdateUI();
            SaveProgress();
        }
    }

    void BuyAutoClickUpgrade()
    {
        if (score >= autoClickUpgradeCost)
        {
            score -= autoClickUpgradeCost;
            autoClickPower++;
            autoClickUpgradeCost *= 2;
            UpdateUI();
            SaveProgress();
        }
    }

    void AutoClick()
    {
        score += autoClickPower * activeMultiplier;
        UpdateUI();
        SaveProgress();
    }

    void UpdateUI()
    {
        // Форматируем большие числа в k, M, B и т.д.
        if (scoreText != null) scoreText.text = FormatNumber(score);
        if (levelText != null) levelText.text = "Уровень: " + level;
        if (xpBar != null) xpBar.fillAmount = (float)xp / xpToNextLevel;
        if (clickUpgradeText != null) clickUpgradeText.text = $"+1 клик\n{FormatNumber(clickUpgradeCost)} Автокредитов";
        if (autoClickUpgradeText != null) autoClickUpgradeText.text = $"+1 автоклик\n{FormatNumber(autoClickUpgradeCost)} Автокредитов";
        if (doubleCoinsButtonText != null)
        {
            // Показать текущий или следующий множитель на кнопке
            if (doubleCoinsActive)
                doubleCoinsButtonText.text = $"x{FormatNumber(activeMultiplier)} (активно)";
            else
                doubleCoinsButtonText.text = $"x{FormatNumber(doubleMultiplier)}";
        }
    }

    // helper: форматирование больших чисел
    private string FormatNumber(long value)
    {
        if (value < 1000) return value.ToString();
        string[] suffixes = { "k", "M", "B", "T", "P", "E" };
        double v = value;
        int idx = -1;
        while (v >= 1000d && idx < suffixes.Length - 1)
        {
            v /= 1000d;
            idx++;
        }
        if (idx < 0) return value.ToString();
        return v.ToString("0.#") + suffixes[idx];
    }

    public void ActivateDoubleCoins()
    {
        // При повторном просмотре рекламы даже во время активного бафа
        // мы сразу повышаем активный множитель до следующего (x2->x4->x8...) и
        // сбрасываем таймер длительности бафа.
        activeMultiplier = doubleMultiplier;
        doubleCoinsActive = true;
        if (doubleCoinsButton != null) doubleCoinsButton.interactable = false;
        // подготовим следующий множитель (удваиваем), с защитой от переполнения
        if (doubleMultiplier <= 1_000_000_000)
            doubleMultiplier *= 2;
        // если уже был запланирован вызов DisableDoubleCoins, сбросим его и назначим заново
        CancelInvoke("DisableDoubleCoins");
        Invoke("DisableDoubleCoins", doubleCoinsDuration);
        UpdateUI();
        SaveProgress();
    }

    private void DisableDoubleCoins()
    {
        doubleCoinsActive = false;
        activeMultiplier = 1;
        if (doubleCoinsButton != null) doubleCoinsButton.interactable = true;
        UpdateUI();
        SaveProgress();
    }

    public void ShowAdForDoubleCoins()
    {
        YandexGame.RewVideoShow(1);
    }

    private void Rewarded(int id)
    {
        if (id == 1)
        {
            ActivateDoubleCoins();
        }
    }

    [ContextMenu("Сбросить прогресс")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteAll();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    // Сохранение/загрузка прогресса (включая doubleMultiplier)
    void SaveProgress()
    {
        PlayerPrefs.SetInt(KEY_SCORE, score);
        PlayerPrefs.SetInt("Level", level);
        PlayerPrefs.SetInt("Xp", xp);
        PlayerPrefs.SetInt("XpToNextLevel", xpToNextLevel);
        PlayerPrefs.SetInt("ClickPower", clickPower);
        PlayerPrefs.SetInt("AutoClickPower", autoClickPower);
        PlayerPrefs.SetInt("ClickUpgradeCost", clickUpgradeCost);
        PlayerPrefs.SetInt("AutoClickUpgradeCost", autoClickUpgradeCost);
        PlayerPrefs.SetInt("CurrentSpriteIndex", currentSpriteIndex);
        PlayerPrefs.SetInt(KEY_DOUBLE_MULTIPLIER, doubleMultiplier);
        PlayerPrefs.SetInt(KEY_DOUBLE_ACTIVE, doubleCoinsActive ? 1 : 0);
        PlayerPrefs.SetInt(KEY_ACTIVE_MULT, activeMultiplier);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        if (PlayerPrefs.HasKey("Score"))
        {
            score = PlayerPrefs.GetInt(KEY_SCORE);
            level = PlayerPrefs.GetInt("Level");
            xp = PlayerPrefs.GetInt("Xp");
            xpToNextLevel = PlayerPrefs.GetInt("XpToNextLevel");
            clickPower = PlayerPrefs.GetInt("ClickPower");
            autoClickPower = PlayerPrefs.GetInt("AutoClickPower");
            clickUpgradeCost = PlayerPrefs.GetInt("ClickUpgradeCost");
            autoClickUpgradeCost = PlayerPrefs.GetInt("AutoClickUpgradeCost");
            currentSpriteIndex = PlayerPrefs.GetInt("CurrentSpriteIndex", level - 1);
            doubleMultiplier = PlayerPrefs.GetInt(KEY_DOUBLE_MULTIPLIER, 2);
            doubleCoinsActive = PlayerPrefs.GetInt(KEY_DOUBLE_ACTIVE, 0) == 1;
            activeMultiplier = PlayerPrefs.GetInt(KEY_ACTIVE_MULT, 1);
        }
        UpdateUI();
        if (currentSpriteIndex < clickButtonSprites.Length && clickButtonSprites[currentSpriteIndex] != null)
        {
            clickButton.image.sprite = clickButtonSprites[currentSpriteIndex];
        }
    }
}