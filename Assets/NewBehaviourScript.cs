using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class ClickerGame : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public TextMeshProUGUI clickUpgradeText;
    public TextMeshProUGUI autoClickUpgradeText;
    public Button clickButton;
    public Button clickUpgradeButton;
    public Button autoClickUpgradeButton;
    public Button doubleCoinsButton;
    public Button adButton;
    public Image xpBar;

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

    void Start()
    {
        LoadProgress();
        UpdateUI();
        clickButton.onClick.AddListener(OnClick);
        clickUpgradeButton.onClick.AddListener(BuyClickUpgrade);
        autoClickUpgradeButton.onClick.AddListener(BuyAutoClickUpgrade);
        doubleCoinsButton.onClick.AddListener(ActivateDoubleCoins);
        adButton.onClick.AddListener(ShowAdForDoubleCoins);
        InvokeRepeating("AutoClick", 1f, 1f);
        Invoke("ForceSetSprite", 0.1f); // задержка после старта
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
        int coinsToAdd = clickPower * (doubleCoinsActive ? 2 : 1);
        score += coinsToAdd;
        xp += clickPower;

        if (xp >= xpToNextLevel)
        {
            LevelUp();
        }

        UpdateUI();
        SaveProgress();
    }

    void LevelUp()
    {
        level++;
        xp = 0;
        xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * 1.5f);
        if (level - 1 < clickButtonSprites.Length)
        {
            currentSpriteIndex = level - 1;
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
        score += autoClickPower * (doubleCoinsActive ? 2 : 1);
        UpdateUI();
        SaveProgress();
    }

    void UpdateUI()
    {
        scoreText.text = "" + score;
        levelText.text = "Уровень: " + level;
        xpBar.fillAmount = (float)xp / xpToNextLevel;
        clickUpgradeText.text = $"+1 клик\n{clickUpgradeCost} Бабанкоин";
        autoClickUpgradeText.text = $"+1 автоклик\n{autoClickUpgradeCost} Бабанкоин";
        // Установка спрайта только по currentSpriteIndex
    }

    void SaveProgress()
    {
        PlayerPrefs.SetInt("Score", score);
        PlayerPrefs.SetInt("Level", level);
        PlayerPrefs.SetInt("Xp", xp);
        PlayerPrefs.SetInt("XpToNextLevel", xpToNextLevel);
        PlayerPrefs.SetInt("ClickPower", clickPower);
        PlayerPrefs.SetInt("AutoClickPower", autoClickPower);
        PlayerPrefs.SetInt("ClickUpgradeCost", clickUpgradeCost);
        PlayerPrefs.SetInt("AutoClickUpgradeCost", autoClickUpgradeCost);
        PlayerPrefs.SetInt("CurrentSpriteIndex", currentSpriteIndex);
        PlayerPrefs.Save();
    }

    void LoadProgress()
    {
        if (PlayerPrefs.HasKey("Score"))
        {
            score = PlayerPrefs.GetInt("Score");
            level = PlayerPrefs.GetInt("Level");
            xp = PlayerPrefs.GetInt("Xp");
            xpToNextLevel = PlayerPrefs.GetInt("XpToNextLevel");
            clickPower = PlayerPrefs.GetInt("ClickPower");
            autoClickPower = PlayerPrefs.GetInt("AutoClickPower");
            clickUpgradeCost = PlayerPrefs.GetInt("ClickUpgradeCost");
            autoClickUpgradeCost = PlayerPrefs.GetInt("AutoClickUpgradeCost");
            currentSpriteIndex = PlayerPrefs.GetInt("CurrentSpriteIndex", level - 1);
        }
        UpdateUI();
        if (currentSpriteIndex < clickButtonSprites.Length && clickButtonSprites[currentSpriteIndex] != null)
        {
            clickButton.image.sprite = clickButtonSprites[currentSpriteIndex];
        }
    }

    void ForceSetSprite()
    {
        if (level - 1 < clickButtonSprites.Length && clickButtonSprites[level - 1] != null)
        {
            clickButton.image.sprite = clickButtonSprites[level - 1];
            Debug.Log("🌟 Спрайт установлен вручную после загрузки. Уровень: " + level);
        }
    }

    public void ActivateDoubleCoins()
    {
        if (!doubleCoinsActive)
        {
            doubleCoinsActive = true;
            doubleCoinsButton.interactable = false;
            Invoke("DisableDoubleCoins", doubleCoinsDuration);
        }
    }

    private void DisableDoubleCoins()
    {
        doubleCoinsActive = false;
        doubleCoinsButton.interactable = true;
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
}
