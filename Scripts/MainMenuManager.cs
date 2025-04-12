using UnityEngine;
using UnityEngine.UI;          // Required for UI elements like Button, Image, Slider
using UnityEngine.SceneManagement; // Required for loading scenes
using TMPro;                  // Required for TextMeshPro components (Text, Font Assets)
using System.Collections.Generic; // Required for Lists
using System;                   // Required for Enum, Action
using System.Linq;              // Required for FirstOrDefault extension method

public class MainMenuManager : MonoBehaviour
{
    #region Serialize Fields (Ссылки на объекты в инспекторе)

    [Header("Building Selection UI")]
    [Header("Scene Management")]
    [Tooltip("Имя или индекс игровой сцены для загрузки")]
    [SerializeField] private string gameSceneName = "GameScene"; // !!! УКАЖИТЕ ИМЯ ВАШЕЙ ИГРОВОЙ СЦЕНЫ !!!

    [Header("Race Selection Data & UI")]
    [Tooltip("Список данных для каждой выбираемой расы (настройте в инспекторе)")]
    [SerializeField] private List<RaceUIData> raceDataList = new List<RaceUIData>();
    [Tooltip("Компонент Image для отображения герба")]
    [SerializeField] private Image crestImage;
    [Tooltip("Компонент TextMeshPro для отображения названия расы")]
    [SerializeField] private TextMeshProUGUI raceNameText;
    [Tooltip("Кнопка 'Стрелка Влево'")]
    [SerializeField] private Button leftArrowButton;
    [Tooltip("Кнопка 'Стрелка Вправо'")]
    [SerializeField] private Button rightArrowButton;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button researchButton;       // Опционально, если есть экран исследований
    [SerializeField] private Button achievementsButton;   // Опционально, если есть экран достижений
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Race Info Panel UI")]
    [SerializeField] private GameObject raceInfoPanel;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button closeInfoButton;
    [SerializeField] private TextMeshProUGUI raceInfoTitleText;
    [SerializeField] private TextMeshProUGUI raceInfoDescriptionText;
    [SerializeField] private Sprite filledStarSprite;
    [SerializeField] private Sprite emptyStarSprite;
    [SerializeField] private Image attackAttributeIcon;
    [SerializeField] private List<Image> attackStarImages = new List<Image>(3);
    [SerializeField] private Image defenseAttributeIcon;
    [SerializeField] private List<Image> defenseStarImages = new List<Image>(3);
    [SerializeField] private Image speedAttributeIcon;
    [SerializeField] private List<Image> speedStarImages = new List<Image>(3);

    [Header("Player Profile & Currency UI")]
    [SerializeField] private TextMeshProUGUI playerLevelText;
    [SerializeField] private Slider xpSlider;
    [SerializeField] private TextMeshProUGUI xpNeededText;
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private TextMeshProUGUI racePointsText;

    [Header("Building Upgrade Slots UI")]
    [Tooltip("Список скриптов BuildingSlotUI для слотов зданий (ровно 3 шт.)")]
    [SerializeField] private List<BuildingSlotUI> buildingSlots = new List<BuildingSlotUI>(3);

    [Header("Background & Fonts")]
    [SerializeField] private Image mainMenuBackground;
    [SerializeField] private List<TextMeshProUGUI> textElementsToChangeFont;
    [SerializeField] private TMP_FontAsset defaultFontAsset;
    [Tooltip("Специфический Font Asset для названия расы Норд")]
    [SerializeField] private TMP_FontAsset nordNameFontAsset;

    [Header("Building Selection UI")]
    [Tooltip("GameObject панели выбора здания")]
    [SerializeField] private GameObject buildingSelectionPanel;
    [Tooltip("Transform-контейнер, куда будут добавляться кнопки выбора")]
    [SerializeField] private Transform selectionButtonsContainer;
    [Tooltip("Префаб кнопки для выбора здания (должен иметь скрипт BuildingSelectionButton)")]
    [SerializeField] private GameObject buildingSelectionButtonPrefab;
    [Tooltip("Кнопка 'Отмена' на панели выбора здания")]
    [SerializeField] private Button closeSelectionPanelButton;
    [Tooltip("GameObject контейнера для сообщения об отсутствии зданий")]
    [SerializeField] private GameObject emptyStateMessageContainer;

    #endregion

    #region Private Fields (Внутреннее состояние)

    private int currentRaceIndex = 0;
    private const string LAST_RACE_INDEX_KEY = "LastSelectedRaceIndex";
    private int currentSlotIndexToAssign = -1; // Индекс слота (1 или 2), для которого выбираем здание

    // Свойство для получения текущей выбранной расы
    private Race currentSelectedRace => (raceDataList != null && raceDataList.Count > currentRaceIndex && currentRaceIndex >= 0) ? raceDataList[currentRaceIndex].raceEnum : Race.Neutral;

    #endregion

    #region Structs (Структуры данных)

    // Структура для хранения UI и связанных данных расы
    [System.Serializable]
    public struct RaceUIData
    {
        public Race raceEnum;
        public string displayName;
        public Sprite crestSprite;
        [TextArea(3, 10)]
        public string raceDescription;
        [Tooltip("Все ассеты BuildingData, доступные этой расе (для поиска и отображения в списке выбора)")]
        public List<BuildingData> availableBuildings; // Список ВСЕХ зданий расы

        [Header("Attribute Ratings & Icons")]
        [Range(0, 3)] public int attackRating;
        public Sprite attackIcon;
        [Range(0, 3)] public int defenseRating;
        public Sprite defenseIcon;
        [Range(0, 3)] public int speedRating;
        public Sprite speedIcon;

        [Header("Visual Style")]
        public Sprite backgroundSprite;
        public TMP_FontAsset raceFontAsset;
    }

    #endregion

    #region Unity Lifecycle Methods (Start, Awake...)

    void Start()
    {
        // 1. Проверяем все ссылки в инспекторе
        if (!ValidateReferences())
        {
            Debug.LogError("MainMenuManager: Обнаружены не назначенные ссылки! Пожалуйста, проверьте Инспектор. Менеджер отключен.", this);
            gameObject.SetActive(false); // Отключаем менеджер, чтобы избежать каскадных ошибок
            return;
        }

        // 2. Проверяем наличие менеджера прогресса
        if (PlayerProgressManager.Instance == null)
        {
            Debug.LogError("PlayerProgressManager не найден! Система прогрессии не будет работать.", this);
            SetInteractableState(false); // Отключаем кнопки, зависящие от прогресса
            // Не отключаем весь объект, позволяем хотя бы выйти из игры
            // или использовать меню без прогрессии
        }
        else
        {
            SetInteractableState(true); // Включаем кнопки, если менеджер есть
        }

        // 3. Скрываем панели по умолчанию
        if (raceInfoPanel != null) raceInfoPanel.SetActive(false);
        if (buildingSelectionPanel != null) buildingSelectionPanel.SetActive(false);

        // 4. Привязываем методы к кнопкам
        AddButtonListeners();

        // 5. Загружаем последнюю выбранную расу и обновляем весь интерфейс
        currentRaceIndex = GetInitialRaceIndex();
        UpdateRaceDisplay();
    }

    #endregion

    #region Initialization and Validation

    // Проверка наличия всех необходимых ссылок
    private bool ValidateReferences()
    {
        bool isValid = true;
        // Используем ??= для краткости проверки на null и логирования
        isValid &= AssertReference(gameSceneName, "Имя игровой сцены (gameSceneName)");
        isValid &= AssertReference(raceDataList, "Список данных рас (raceDataList)");
        if (raceDataList != null && raceDataList.Count == 0) { Debug.LogError("Список данных рас пуст!", this); isValid = false; }

        isValid &= AssertReference(crestImage, "Герб (crestImage)");
        // isValid &= AssertReference(raceNameText, "Текст имени расы (raceNameText)"); // Не критично
        isValid &= AssertReference(leftArrowButton, "Кнопка влево (leftArrowButton)");
        isValid &= AssertReference(rightArrowButton, "Кнопка вправо (rightArrowButton)");
        isValid &= AssertReference(playButton, "Кнопка Играть (playButton)");
        isValid &= AssertReference(exitButton, "Кнопка Выход (exitButton)");

        isValid &= AssertReference(raceInfoPanel, "Панель информации (raceInfoPanel)");
        isValid &= AssertReference(infoButton, "Кнопка информации (infoButton)");
        isValid &= AssertReference(closeInfoButton, "Кнопка закрытия инфо (closeInfoButton)");
        isValid &= AssertReference(filledStarSprite, "Спрайт заполненной звезды (filledStarSprite)");
        isValid &= AssertReference(emptyStarSprite, "Спрайт пустой звезды (emptyStarSprite)");

        isValid &= AssertReference(playerLevelText, "Текст уровня (playerLevelText)");
        isValid &= AssertReference(xpSlider, "Слайдер опыта (xpSlider)");
        isValid &= AssertReference(coinsText, "Текст монет (coinsText)");
        isValid &= AssertReference(racePointsText, "Текст очков расы (racePointsText)");

        if (buildingSlots == null || buildingSlots.Count != 3 || buildingSlots.Exists(slot => slot == null))
        { Debug.LogError("Список слотов зданий (buildingSlots) не настроен правильно (нужно 3 не пустых элемента)!", this); isValid = false; }

        isValid &= AssertReference(mainMenuBackground, "Фон меню (mainMenuBackground)");
        if (textElementsToChangeFont == null) { Debug.LogWarning("Список текстов для смены шрифта (textElementsToChangeFont) не назначен.", this); }
        if (nordNameFontAsset == null) { Debug.LogWarning("Специфический шрифт для Нордов (nordNameFontAsset) не назначен.", this); } // Не критично, будет использоваться другой

        isValid &= AssertReference(buildingSelectionPanel, "Панель выбора здания (buildingSelectionPanel)");
        isValid &= AssertReference(selectionButtonsContainer, "Контейнер кнопок выбора (selectionButtonsContainer)");
        isValid &= AssertReference(buildingSelectionButtonPrefab, "Префаб кнопки выбора здания (buildingSelectionButtonPrefab)");

        return isValid;
    }

    // Вспомогательный метод для проверки ссылки
    private bool AssertReference<T>(T obj, string fieldName) where T : class
    {
        if (obj == null)
        {
            Debug.LogError($"{fieldName} не назначен в инспекторе!", this);
            return false;
        }
        return true;
    }
     private bool AssertReference(string str, string fieldName) {
          if (string.IsNullOrEmpty(str)) {
               Debug.LogError($"{fieldName} не указано в инспекторе!", this);
                return false;
          }
          return true;
     }
    // Вспомогательный метод для проверки списков Image
    private bool ValidateImageList(List<Image> list, string listName) {
        if (list == null || list.Count != 3 || list.Exists(img => img == null)) {
             Debug.LogError($"Список {listName} настроен неверно (нужно 3 не пустых элемента)!", this);
             return false;
        }
        return true;
    }


    // Привязка методов к кнопкам
    private void AddButtonListeners()
    {
        leftArrowButton?.onClick.AddListener(PreviousRace);
        rightArrowButton?.onClick.AddListener(NextRace);
        playButton?.onClick.AddListener(StartGame);
        researchButton?.onClick.AddListener(OpenResearch);
        achievementsButton?.onClick.AddListener(OpenAchievements);
        settingsButton?.onClick.AddListener(OpenSettings);
        exitButton?.onClick.AddListener(QuitGame);
        infoButton?.onClick.AddListener(ShowRaceInfo);
        closeInfoButton?.onClick.AddListener(HideRaceInfo);
        closeSelectionPanelButton?.onClick.AddListener(HideBuildingSelection);
    }

    // Включение/выключение основных кнопок
    private void SetInteractableState(bool state)
    {
        // Проверяем наличие PlayerProgressManager только один раз
        bool progressAvailable = state && PlayerProgressManager.Instance != null;
        bool hasRaceData = raceDataList != null && raceDataList.Count > 0;
        bool canSwitchRace = hasRaceData && raceDataList.Count > 1;

        // Кнопки, зависящие от наличия данных рас и прогресса
        if (playButton) playButton.interactable = progressAvailable && hasRaceData;
        if (researchButton) researchButton.interactable = progressAvailable && hasRaceData; // Или своя логика
        if (achievementsButton) achievementsButton.interactable = progressAvailable && hasRaceData; // Или своя логика
        if (infoButton) infoButton.interactable = state && hasRaceData; // Инфо зависит только от наличия данных рас
        if (leftArrowButton) leftArrowButton.interactable = state && canSwitchRace;
        if (rightArrowButton) rightArrowButton.interactable = state && canSwitchRace;

        // Кнопки, которые могут быть доступны всегда или зависят только от 'state'
        if (settingsButton) settingsButton.interactable = state;
        // if (exitButton) exitButton.interactable = state; // Выход обычно всегда доступен
    }


    // Получаем индекс последней выбранной расы
    private int GetInitialRaceIndex()
    {
        int savedIndex = PlayerPrefs.GetInt(LAST_RACE_INDEX_KEY, 0);
        // Убедимся, что индекс валиден для текущего списка рас
        return Mathf.Clamp(savedIndex, 0, Mathf.Max(0, raceDataList.Count - 1));
    }

    #endregion

    #region Race Switching Logic

    public void NextRace()
    {
        if (raceDataList.Count <= 1) return;
        currentRaceIndex = (currentRaceIndex + 1) % raceDataList.Count;
        PlayerPrefs.SetInt(LAST_RACE_INDEX_KEY, currentRaceIndex);
        UpdateRaceDisplay();
    }

    public void PreviousRace()
    {
        if (raceDataList.Count <= 1) return;
        currentRaceIndex--;
        if (currentRaceIndex < 0) currentRaceIndex = raceDataList.Count - 1;
        PlayerPrefs.SetInt(LAST_RACE_INDEX_KEY, currentRaceIndex);
        UpdateRaceDisplay();
    }

    #endregion

    #region UI Update Logic

    // Главный метод обновления всего UI
    private void UpdateRaceDisplay()
    {
        if (!ValidateCurrentState()) return; // Проверяем готовность перед обновлением

        RaceUIData currentUIData = raceDataList[currentRaceIndex];
        Race currentRace = currentUIData.raceEnum;

        // --- Обновление Фона ---
        UpdateBackground(currentUIData);

        // --- Обновление Шрифта для ВСЕХ ОСТАЛЬНЫХ текстов ---
        // (Метод UpdateFonts теперь НЕ должен трогать raceNameText,
        // т.к. его нет в списке textElementsToChangeFont)
        UpdateFonts(currentUIData);

        // --- Обновляем Герб ---
        if (crestImage != null)
        {
            crestImage.sprite = currentUIData.crestSprite;
            crestImage.enabled = currentUIData.crestSprite != null;
        }

        // --- СПЕЦИАЛЬНОЕ ОБНОВЛЕНИЕ ШРИФТА ДЛЯ ИМЕНИ РАСЫ ---
        if (raceNameText != null)
        {
            TMP_FontAsset fontForRaceName = null;

            // 1. Проверяем, выбраны ли Норды и есть ли для них спец. шрифт
            if (currentRace == Race.Nord && nordNameFontAsset != null)
            {
                fontForRaceName = nordNameFontAsset;
            }
            // 2. Иначе, проверяем, есть ли общий шрифт для текущей расы
            else if (currentUIData.raceFontAsset != null)
            {
                fontForRaceName = currentUIData.raceFontAsset;
            }
            // 3. Иначе, используем дефолтный шрифт
            else
            {
                fontForRaceName = defaultFontAsset;
            }

            // Применяем найденный шрифт (если он есть)
            if (fontForRaceName != null)
            {
                raceNameText.font = fontForRaceName;
            }
            // Обновляем текст имени
            raceNameText.text = currentUIData.displayName;
        }

        // --- Скрываем панели ---
        HideRaceInfo();
        HideBuildingSelection();

        // --- Обновляем динамические данные ---
        UpdateProfileUI(currentRace, currentUIData.displayName);
        UpdateBuildingSlots(currentRace, currentUIData);
    }

    // Проверка готовности к обновлению
    private bool ValidateCurrentState()
    {
        if (PlayerProgressManager.Instance == null) return false; // Не можем обновить без данных
        if (raceDataList == null || raceDataList.Count == 0) return false; // Нет данных о расах
        if (currentRaceIndex < 0 || currentRaceIndex >= raceDataList.Count) return false; // Индекс вне диапазона
        return true;
    }

    // Обновление Фона
    private void UpdateBackground(RaceUIData data)
    {
        if (mainMenuBackground != null)
        {
            mainMenuBackground.sprite = data.backgroundSprite; // Показываем фон расы или None
            mainMenuBackground.enabled = data.backgroundSprite != null; // Включаем/выключаем Image
        }
    }

    // Обновление Шрифтов
    private void UpdateFonts(RaceUIData data)
    {
        TMP_FontAsset fontToUse = data.raceFontAsset ?? defaultFontAsset; // Выбираем шрифт расы или дефолтный
        if (fontToUse == null) return; // Если шрифта нет, ничего не делаем

        if (textElementsToChangeFont != null)
        {
            foreach (TextMeshProUGUI textElement in textElementsToChangeFont)
            {
                if (textElement != null && textElement.font != fontToUse) // Меняем только если шрифт отличается
                {
                    textElement.font = fontToUse;
                }
            }
        }
    }

    // Обновление UI Профиля
    private void UpdateProfileUI(Race race, string raceDisplayName)
    {
        if (PlayerProgressManager.Instance == null) return;

        // Обновляем монеты (всегда)
        if (coinsText != null) coinsText.text = $"{PlayerProgressManager.Instance.GetCoins()} G"; // Добавим "G"

        // Обновляем данные текущей расы
        if (race != Race.Neutral)
        {
            int level = PlayerProgressManager.Instance.GetRaceLevel(race);
            int currentXp = PlayerProgressManager.Instance.GetRaceXP(race);
            int xpForNext = PlayerProgressManager.Instance.GetXPForNextLevel(race);
            int racePoints = PlayerProgressManager.Instance.GetRacePoints(race);

            if (playerLevelText != null) playerLevelText.text = $"Ур. {level} ({raceDisplayName})";
            if (racePointsText != null) racePointsText.text = $"{racePoints} RP";
            if (xpSlider != null)
            {
                bool maxLevelReached = xpForNext <= 0; // Если XP не нужен, значит макс. уровень
                xpSlider.minValue = 0;
                xpSlider.maxValue = maxLevelReached ? 1 : xpForNext; // Макс = 1 на макс. уровне
                xpSlider.value = maxLevelReached ? 1 : currentXp; // Заполняем полностью на макс. уровне
                xpSlider.gameObject.SetActive(!maxLevelReached); // Скрываем на макс. уровне
            }
            if (xpNeededText != null)
            {
                 bool maxLevelReached = xpForNext <= 0;
                 xpNeededText.text = maxLevelReached ? "МАКС. УР." : $"{currentXp}/{xpForNext}";
                 // xpNeededText.gameObject.SetActive(!maxLevelReached); // Можно тоже скрыть
            }
        }
        else { /* Очистка UI для нейтральной, если это возможно */ }
    }

    // Обновление Слотов Зданий
    private void UpdateBuildingSlots(Race race, RaceUIData raceUIData)
    {
        if (PlayerProgressManager.Instance == null || buildingSlots.Count == 0 || race == Race.Neutral) return;

        int coins = PlayerProgressManager.Instance.GetCoins();
        int racePoints = PlayerProgressManager.Instance.GetRacePoints(race);
        int maxBuildingLevel = PlayerProgressManager.Instance.GetMaxBuildingLevel();

        for (int i = 0; i < buildingSlots.Count; i++)
        {
            BuildingSlotUI slot = buildingSlots[i];
            if (slot == null) continue; // Пропускаем неназначенный слот

            BuildingType? assignedType = PlayerProgressManager.Instance.GetAssignedBuildingType(race, i);
            BuildingData buildingData = (assignedType != null) ? FindBuildingDataForRace(race, assignedType.Value) : null;

            if (i == 0) // Слот 0 - всегда Дом
            {
                if (buildingData == null || assignedType != BuildingType.House) {
                     // Этого не должно случиться, если GetAssignedBuildingType(0) всегда возвращает House
                     slot.ShowErrorState("Дом?");
                     slot.SetupButton("Ошибка", false, null);
                     continue;
                }

                int houseLevel = PlayerProgressManager.Instance.GetBuildingLevel(race, BuildingType.House);
                slot.UpdateSlotInfo(buildingData.icon, buildingData.buildingName);
                slot.SetIconVisibility(true);

                if (houseLevel < maxBuildingLevel) {
                    int upgradeCost = PlayerProgressManager.Instance.GetNextUpgradeCostCoins(BuildingType.House, houseLevel);
                    slot.SetupButton($"Улучшить ({upgradeCost} G)", coins >= upgradeCost, () => BuildOrUpgradeBuilding(BuildingType.House));
                    slot.UpdateLevelText($"Ур. {houseLevel} / {maxBuildingLevel}");
                } else {
                    slot.SetupButton("МАКС.", false, null);
                    slot.UpdateLevelText($"Ур. {maxBuildingLevel} (МАКС.)");
                }
            }
            else // Слоты 1 и 2
            {
                if (assignedType == null) // Пустой слот
                {
                    slot.ShowEmptyState(); // Показывает "+"/текст
                    slot.SetupButton("Выбрать", true, () => ShowBuildingSelection(i));
                }
                else if (buildingData != null) // Слот назначен
                {
                    int currentLevel = PlayerProgressManager.Instance.GetBuildingLevel(race, assignedType.Value);
                    bool isResearched = PlayerProgressManager.Instance.IsBuildingResearched(race, assignedType.Value);

                    slot.UpdateSlotInfo(buildingData.icon, buildingData.buildingName);
                    slot.SetIconVisibility(currentLevel > 0);

                    if (!isResearched) {
                         // Теоретически не должно быть, т.к. назначаем только исследованные, но на всякий случай
                         slot.ShowErrorState("Не иссл.?");
                         slot.SetupButton("Ошибка", false, null);
                    } else if (currentLevel == 0) { // Исследовано, но не построено
                        int buildCost = PlayerProgressManager.Instance.GetNextUpgradeCostCoins(assignedType.Value, 0);
                        slot.SetupButton($"Построить ({buildCost} G)", coins >= buildCost, () => BuildOrUpgradeBuilding(assignedType.Value));
                        slot.UpdateLevelText("Исследовано");
                    } else if (currentLevel < maxBuildingLevel) { // Построено, можно улучшить
                        int upgradeCost = PlayerProgressManager.Instance.GetNextUpgradeCostCoins(assignedType.Value, currentLevel);
                        slot.SetupButton($"Улучшить ({upgradeCost} G)", coins >= upgradeCost, () => BuildOrUpgradeBuilding(assignedType.Value));
                        slot.UpdateLevelText($"Ур. {currentLevel} / {maxBuildingLevel}");
                    } else { // Макс. уровень
                        slot.SetupButton("МАКС.", false, null);
                        slot.UpdateLevelText($"Ур. {maxBuildingLevel} (МАКС.)");
                    }
                }
                else // Ошибка: тип назначен, но данные не найдены
                {
                    slot.ShowErrorState("Данные?");
                    slot.SetupButton("Ошибка", false, null);
                }
                if (PlayerProgressManager.Instance == null || buildingSelectionPanel == null ||
                    selectionButtonsContainer == null || buildingSelectionButtonPrefab == null ||
                    emptyStateMessageContainer == null) 
                    {
                               Debug.LogError("Не настроен UI для выбора здания!");
                        return;
                    }
            }
        }
    }

    #endregion

    #region Building Interaction Logic

    // Начать исследование здания
    public void StartResearch(BuildingType type)
    {
        if (PlayerProgressManager.Instance == null) return;
        Race race = currentSelectedRace;
        if (race == Race.Neutral || type == BuildingType.House) return;

        int cost = PlayerProgressManager.Instance.GetResearchCostRacePoints(type);
        if (PlayerProgressManager.Instance.SpendRacePoints(race, cost))
        {
            if (PlayerProgressManager.Instance.MarkBuildingResearched(race, type))
            {
                Debug.Log($"Research complete for {type} by {race}");
                UpdateRaceDisplay(); // Обновляем UI, чтобы показать кнопку "Построить"
            } else {
                PlayerProgressManager.Instance.AddRacePoints(race, cost); // Возврат RP в случае ошибки
                Debug.LogError($"Failed to mark research for {type}!");
            }
        } else {
            Debug.Log($"Not enough RP for {type}. Need {cost}."); // Сообщение игроку?
        }
    }

    // Построить или улучшить здание
    public void BuildOrUpgradeBuilding(BuildingType type)
    {
        if (PlayerProgressManager.Instance == null) return;
        Race race = currentSelectedRace;
        if (race == Race.Neutral) return;

        int currentLevel = PlayerProgressManager.Instance.GetBuildingLevel(race, type);
        int maxLevel = PlayerProgressManager.Instance.GetMaxBuildingLevel();

        if (currentLevel >= maxLevel) return;

        if (type != BuildingType.House && !PlayerProgressManager.Instance.IsBuildingResearched(race, type))
        {
            Debug.LogError($"Cannot build/upgrade unresearched building {type}");
            return;
        }

        int cost = PlayerProgressManager.Instance.GetNextUpgradeCostCoins(type, currentLevel);
        if (PlayerProgressManager.Instance.SpendCoins(cost))
        {
            int nextLevel = currentLevel + 1;
            if (PlayerProgressManager.Instance.SetBuildingLevel(race, type, nextLevel))
            {
                Debug.Log($"Building {type} built/upgraded to level {nextLevel} for {race}");
                UpdateRaceDisplay();
            } else {
                 PlayerProgressManager.Instance.AddCoins(cost); // Возврат монет
                 Debug.LogError($"Failed to set building level for {type}!");
            }
        } else {
            Debug.Log($"Not enough Coins for {type}. Need {cost}."); // Сообщение игроку?
        }
    }

    // Показать панель выбора здания для слота
    public void ShowBuildingSelection(int slotIndex)
    {
        if (PlayerProgressManager.Instance == null || buildingSelectionPanel == null ||
            selectionButtonsContainer == null || buildingSelectionButtonPrefab == null ||
            emptyStateMessageContainer == null)
        {
             Debug.LogError("Не настроен UI для выбора здания!");
             return;
        }

        currentSlotIndexToAssign = slotIndex;
        Race race = currentSelectedRace;

        // Очистка контейнера кнопок (на всякий случай)
        foreach (Transform child in selectionButtonsContainer) { Destroy(child.gameObject); }

        List<BuildingType> availableTypes = PlayerProgressManager.Instance.GetAvailableBuildingsToAssign(race);

        if (availableTypes.Count == 0)
        {
            // --- ПОКАЗАТЬ СООБЩЕНИЕ ---
            emptyStateMessageContainer.SetActive(true); // Показываем контейнер с текстом
            selectionButtonsContainer.gameObject.SetActive(false); // Скрываем контейнер с сеткой
            Debug.Log("Нет доступных зданий для постройки в этом слоте.");
            // Можно дополнительно обновить текст в emptyStateMessageContainer.GetComponentInChildren<TextMeshProUGUI>()
        }
        else
        {
            // --- ПОКАЗАТЬ КНОПКИ ---
            emptyStateMessageContainer.SetActive(false); // Скрываем контейнер с текстом
            selectionButtonsContainer.gameObject.SetActive(true); // Показываем контейнер с сеткой

            foreach (BuildingType type in availableTypes)
            {
                BuildingData data = FindBuildingDataForRace(race, type);
                if (data == null) continue;

                GameObject buttonGO = Instantiate(buildingSelectionButtonPrefab, selectionButtonsContainer);
                BuildingSelectionButton selectionButton = buttonGO.GetComponent<BuildingSelectionButton>();
                if (selectionButton != null)
                {
                    selectionButton.Initialize(data, SelectBuildingForCurrentSlot);
                } else { Destroy(buttonGO); }
            }
        }
        buildingSelectionPanel.SetActive(true); // Показываем главную панель выбора
    }

    // Вызывается при клике на кнопку в панели выбора
    private void SelectBuildingForCurrentSlot(BuildingType chosenType)
    {
         if (PlayerProgressManager.Instance == null || currentSlotIndexToAssign < 1 || currentSlotIndexToAssign > 2) return;
         Race race = currentSelectedRace;

         // Доп. проверка, что тип действительно доступен для назначения (на всякий случай)
         List<BuildingType> available = PlayerProgressManager.Instance.GetAvailableBuildingsToAssign(race);
         if (!available.Contains(chosenType)) {
              Debug.LogError($"Попытка назначить недоступное здание {chosenType}!");
              HideBuildingSelection();
              return;
         }


         if (PlayerProgressManager.Instance.SetAssignedBuildingType(race, currentSlotIndexToAssign, chosenType))
         {
             Debug.Log($"Building {chosenType} assigned to slot {currentSlotIndexToAssign} for {race}");
         } else {
              Debug.LogError($"Failed to assign building {chosenType} to slot {currentSlotIndexToAssign}");
         }

         HideBuildingSelection();
         UpdateRaceDisplay();
    }

    // Скрыть панель выбора
    public void HideBuildingSelection()
    {
        if (buildingSelectionPanel != null)
        {
             buildingSelectionPanel.SetActive(false);
             if(selectionButtonsContainer != null) { 
                 foreach (Transform child in selectionButtonsContainer) { 
                     Destroy(child.gameObject); 
                 } 
             }
             if(emptyStateMessageContainer != null) {
                  emptyStateMessageContainer.SetActive(false);
             }
             currentSlotIndexToAssign = -1;
        }
    }

    #endregion

    #region Main Menu Actions
    // Код для StartGame, QuitGame, OpenResearch и т.д.
     public void StartGame() {
        if (PlayerProgressManager.Instance == null || string.IsNullOrEmpty(gameSceneName) || currentSelectedRace == Race.Neutral)
        {
             Debug.LogError("Невозможно начать игру: нет данных прогресса, имени сцены или выбрана нейтральная раса!");
             return;
        }
        PlayerPrefs.SetString("SelectedPlayerRace", currentSelectedRace.ToString());
        PlayerPrefs.Save();
        Debug.Log($"Starting game with selected race: {currentSelectedRace}");
        SceneManager.LoadScene(gameSceneName);
    }
    public void OpenResearch() { Debug.Log("Research button - Not implemented."); }
    public void OpenAchievements() { Debug.Log("Achievements button - Not implemented."); }
    public void OpenSettings() { Debug.Log("Settings button - Not implemented."); }
    public void QuitGame() {
        Debug.Log("Quit button clicked");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    #endregion

    #region Info Panel Logic
     // Код для ShowRaceInfo, HideRaceInfo, UpdateStars
    public void ShowRaceInfo() {
        if (raceDataList.Count == 0 || raceInfoPanel == null || currentSelectedRace == Race.Neutral) return;
        if (currentRaceIndex >= raceDataList.Count) return;

        RaceUIData currentData = raceDataList[currentRaceIndex];

        if (raceInfoTitleText != null) raceInfoTitleText.text = currentData.displayName;
        if (raceInfoDescriptionText != null) raceInfoDescriptionText.text = currentData.raceDescription;

        if (attackAttributeIcon != null) { attackAttributeIcon.sprite = currentData.attackIcon; attackAttributeIcon.enabled = currentData.attackIcon != null;}
        if (defenseAttributeIcon != null) { defenseAttributeIcon.sprite = currentData.defenseIcon; defenseAttributeIcon.enabled = currentData.defenseIcon != null; }
        if (speedAttributeIcon != null) { speedAttributeIcon.sprite = currentData.speedIcon; speedAttributeIcon.enabled = currentData.speedIcon != null; }

        UpdateStars(attackStarImages, currentData.attackRating);
        UpdateStars(defenseStarImages, currentData.defenseRating);
        UpdateStars(speedStarImages, currentData.speedRating);

        // Обновляем шрифт текста в панели информации, если он есть в списке
        TMP_FontAsset fontToUse = currentData.raceFontAsset ?? defaultFontAsset;
        if (fontToUse != null && textElementsToChangeFont != null) {
             if(raceInfoTitleText && textElementsToChangeFont.Contains(raceInfoTitleText)) raceInfoTitleText.font = fontToUse;
             if(raceInfoDescriptionText && textElementsToChangeFont.Contains(raceInfoDescriptionText)) raceInfoDescriptionText.font = fontToUse;
        }

        raceInfoPanel.SetActive(true);
    }
    public void HideRaceInfo() {
        if (raceInfoPanel != null) raceInfoPanel.SetActive(false);
    }
    private void UpdateStars(List<Image> starImages, int rating) {
         if (starImages == null || starImages.Count != 3 || filledStarSprite == null || emptyStarSprite == null) return;
         int clampedRating = Mathf.Clamp(rating, 0, 3);
         for (int i = 0; i < 3; i++) {
              if (starImages[i] != null) {
                   starImages[i].sprite = (i < clampedRating) ? filledStarSprite : emptyStarSprite;
                   starImages[i].enabled = true;
              }
         }
    }
    #endregion

    #region Helper Methods

    // Находит BuildingData для указанного типа СРЕДИ зданий, доступных указанной расе
    private BuildingData FindBuildingDataForRace(Race race, BuildingType type)
    {
        // Находим данные текущей расы в raceDataList
        // Используем FirstOrDefault для поиска или получения default(RaceUIData)
        RaceUIData raceUIData = raceDataList.FirstOrDefault(data => data.raceEnum == race);

        // Проверяем, что структура не дефолтная (т.е. раса найдена) и список зданий не null
        if (raceUIData.availableBuildings != null)
        {
            // Ищем здание в списке доступных для этой расы
            return raceUIData.availableBuildings.FirstOrDefault(bData => bData != null && bData.type == type);
        }

        Debug.LogWarning($"Не удалось найти список зданий для расы {race} или само здание типа {type}");
        return null; // Возвращаем null, если не нашли
    }

    #endregion

} // -- Конец класса MainMenuManager --