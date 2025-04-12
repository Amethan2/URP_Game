using UnityEngine;
using TMPro;                  // Required for TextMeshPro
using System.Reflection;      // Required for accessing private fields (alternative to making them public)
using System.Collections.Generic; // If you need Lists later

// Ensure Race and BuildingType enums are accessible
// public enum Race { Neutral, Nord, Orden, Rus, Orda }
// public enum BuildingType { House, Altar, Tower, Stable }

public class Building : MonoBehaviour
{
    #region Serialized Fields & Public References

    [Header("Data Reference")]
    [Tooltip("Ссылка на ScriptableObject с данными этого типа здания.")]
    public BuildingData buildingData; // !!! НАЗНАЧЬТЕ В ИНСПЕКТОРЕ !!!

    [Header("Runtime State (Может меняться в игре)")]
    [Tooltip("Текущая раса, контролирующая здание (будет перезаписана при инициализации для стартовых зданий игрока).")]
    public Race race = Race.Neutral;
    [Tooltip("Текущее количество юнитов в здании.")]
    public int currentUnits = 0;

    [Header("Initialization Settings (Set in Scene)")]
    [Tooltip("Установите, если это здание должно принадлежать игроку на старте карты.")]
    [SerializeField] private bool startsAsPlayerBuilding = false;
    // [Tooltip("Индекс слота меню (1 или 2), которому соответствует это здание (опционально).")]
    // [SerializeField] private int correspondingMenuSlot = -1; // Опционально для более сложной логики

    [Header("Visuals & Links")]
    [Tooltip("Компонент для отображения количества юнитов")]
    public TextMeshPro unitCountText;
    [Tooltip("Цвет подсветки при выделении")]
    public Color selectionColor = Color.yellow;
    [Tooltip("Слои для атаки (актуально для Башен)")]
    [SerializeField] private LayerMask enemyLayerMask = default; // Инициализируем значением по умолчанию

    #endregion

    #region Private Runtime Variables

    // Характеристики, зависящие от уровня
    private int runtimeMaxCapacity;
    private float runtimePopulationGrowth;
    private float runtimeSpawnTime;
    private float runtimeManaPerSecond;
    private float runtimeAttackDamage;
    private float runtimeAttackRange;
    private float runtimeAttackCooldown;
    private bool runtimeCanAttack;

    // Состояние и компоненты
    private UnitData currentUnitType;
    private SpriteRenderer buildingSpriteRenderer;
    private Color originalColor;
    private bool isSelected = false;
    private bool isInitialized = false;

    // Таймеры и прогрессы
    private float populationGrowthProgress = 0.0f;
    private float unitSpawnProgress = 0.0f;
    private float attackCooldownTimer = 0.0f;

    #endregion

    #region Unity Lifecycle Methods

    void Awake()
    {
        // Получаем компоненты
        buildingSpriteRenderer = GetComponent<SpriteRenderer>();
        if (unitCountText == null) unitCountText = GetComponentInChildren<TextMeshPro>();

       
        if (buildingSpriteRenderer == null) Debug.LogError($"SpriteRenderer не найден на '{gameObject.name}'!", this);
    }

    void Start()
    {
        // Инициализация вызывается извне (GameManager'ом) после определения расы игрока.
        // Поэтому здесь ничего не делаем, чтобы избежать инициализации до того,
        // как будет установлена правильная раса для зданий игрока.
        // Если нужно, чтобы нейтральные здания инициализировались сами,
        // можно добавить вызов InitializeBuilding() здесь с проверкой if (!startsAsPlayerBuilding).
    }

    void Update()
    {
        if (!isInitialized || race == Race.Neutral) return; // Ждем инициализации, пропускаем нейтральные

        float dt = Time.deltaTime;

        // Логика здания на основе типа и runtime-статов
        switch (buildingData.type)
        {
            case BuildingType.House: HandlePopulationGrowth(dt); break;
            case BuildingType.Tower:
                HandleUnitSpawning(dt);
                HandleTowerAttack(dt); break;
            case BuildingType.Altar:
                HandleManaGeneration(dt);
                HandleUnitSpawning(dt); break;
            case BuildingType.Stable: HandleUnitSpawning(dt); break;
        }

        // Санитизация и визуал
        if (currentUnits < 0) { currentUnits = 0; UpdateUnitText(); }
        UpdateSelectionVisuals();
    }

    #endregion

    #region Initialization Logic

    // --- Главный метод инициализации здания (вызывается из GameManager или при захвате) ---
    public void InitializeBuilding()
    {
        // Можно добавить проверку if (isInitialized), если есть риск двойного вызова,
        // но лучше, чтобы вызывающая сторона (GameManager) это контролировала.
        // if (isInitialized) return;

        if (buildingData == null) {
             Debug.LogError($"Попытка инициализировать {gameObject.name} без BuildingData!", this);
             gameObject.SetActive(false);
             return;
        }

        // --- Определяем уровень здания ---
        int buildingLevel = 1; // Уровень по умолчанию

        // Если раса здания уже установлена (не Neutral) и есть менеджер прогресса
        if (this.race != Race.Neutral && PlayerProgressManager.Instance != null)
        {
            buildingLevel = PlayerProgressManager.Instance.GetBuildingLevel(this.race, buildingData.type);

            // --- Проверка назначения на слот и уровня (для зданий, кроме Дома) ---
            if (buildingData.type != BuildingType.House)
            {
                 // Проверяем, назначено ли здание на слот 1 или 2 для этой расы
                 BuildingType? slot1Assignment = PlayerProgressManager.Instance.GetAssignedBuildingType(this.race, 1);
                 BuildingType? slot2Assignment = PlayerProgressManager.Instance.GetAssignedBuildingType(this.race, 2);
                 bool isAssigned = (slot1Assignment.HasValue && slot1Assignment.Value == buildingData.type) ||
                                   (slot2Assignment.HasValue && slot2Assignment.Value == buildingData.type);

                 // Если здание не назначено ИЛИ его сохраненный уровень <= 0, деактивируем его
                 if (!isAssigned || buildingLevel <= 0)
                 {
                     Debug.Log($"Деактивируем здание {gameObject.name} (Тип: {buildingData.type}, Раса: {this.race}). Причина: Не назначено на слот или Уровень <= 0 (Уровень={buildingLevel}, Назначено={isAssigned}).");
                     gameObject.SetActive(false);
                     return; // Прекращаем инициализацию
                 }
            }
             // Уровень дома и назначенных зданий должен быть >= 1
             buildingLevel = Mathf.Max(1, buildingLevel);
        }
        else if (this.race == Race.Neutral)
        {
             // Нейтральные здания всегда Ур. 1
             buildingLevel = 1;
        }
        // Если раса не нейтральная, но нет PlayerProgressManager, используем уровень 1 по умолчанию

        // --- Применяем статы для уровня ---
        InitializeStatsForLevel(buildingLevel);

        // --- Определяем тип юнитов ---
        DetermineInitialUnitType();

        // --- Обновляем визуал ---
        UpdateBuildingVisuals(); // Устанавливаем цвет
        if (buildingSpriteRenderer != null && buildingData.buildingSprite != null)
        {
            buildingSpriteRenderer.sprite = buildingData.buildingSprite;
        }
        UpdateUnitText(); // Отображаем начальных юнитов

        isInitialized = true; // Помечаем как инициализированное
        Debug.Log($"{gameObject.name} (Тип: {buildingData.type}) инициализирован. Раса: {this.race}, Уровень: {buildingLevel}. Вместимость: {runtimeMaxCapacity}");
    }

    // Установка характеристик на основе уровня
    private void InitializeStatsForLevel(int level)
    {
        if (buildingData == null) return;
        BuildingLevelStats? statsOpt = buildingData.GetStatsForLevel(level);

        if (statsOpt.HasValue)
        {
            BuildingLevelStats stats = statsOpt.Value;
            runtimeMaxCapacity = stats.maxCapacity;
            runtimePopulationGrowth = stats.populationGrowthPerSecond;
            runtimeSpawnTime = stats.spawnTime;
            runtimeManaPerSecond = stats.manaPerSecond;
            runtimeAttackDamage = stats.attackDamage;
            runtimeAttackRange = stats.attackRange;
            runtimeAttackCooldown = stats.attackCooldown;
            runtimeCanAttack = stats.canAttack;

            // Начальные юниты при инициализации/захвате (можно сделать 0 или другое значение)
            currentUnits = (this.race == Race.Neutral) ? 0 : Mathf.Max(0, Mathf.FloorToInt(runtimeMaxCapacity * 0.1f)); // Начнем с 10% или 0
        }
        else
        {
            Debug.LogError($"Не найдены статы для уровня {level} в {buildingData.name}! Здание будет нефункционально.", this);
            // Устанавливаем безопасные, нерабочие значения
            runtimeMaxCapacity = 0; currentUnits = 0; runtimePopulationGrowth = 0; runtimeSpawnTime = float.PositiveInfinity;
            runtimeManaPerSecond = 0; runtimeAttackDamage = 0; runtimeAttackRange = 0; runtimeAttackCooldown = float.PositiveInfinity;
            runtimeCanAttack = false;
        }
        ResetTimers();
    }

    // Определяем тип юнита
    private void DetermineInitialUnitType()
    {
        currentUnitType = null; // Сбрасываем перед определением
        if (buildingData == null) return;

        switch (buildingData.type)
        {
            case BuildingType.House: currentUnitType = buildingData.populationUnitType; break;
            case BuildingType.Altar:
            case BuildingType.Tower:
            case BuildingType.Stable: currentUnitType = buildingData.unitToSpawn; break;
        }

        // Если не определился стандартно, и раса не нейтральная, пробуем через DataManager
        if (currentUnitType == null && this.race != Race.Neutral && DataManager.Instance != null)
        {
            currentUnitType = DataManager.Instance.GetUnitDataForBuilding(this.race, buildingData.type);
        }

        if (currentUnitType == null && this.race != Race.Neutral &&
            (buildingData.type == BuildingType.House || buildingData.type == BuildingType.Altar || buildingData.type == BuildingType.Tower || buildingData.type == BuildingType.Stable))
        {
            Debug.LogWarning($"Не удалось определить тип юнита для {gameObject.name} (Тип: {buildingData.type}, Раса: {this.race})", this);
        }
    }

    // Сброс таймеров
    private void ResetTimers()
    {
        populationGrowthProgress = 0f;
        unitSpawnProgress = 0f;
        attackCooldownTimer = 0f;
    }

    #endregion

    #region Building Logic (Using Runtime Variables)

    // Рост населения
    void HandlePopulationGrowth(float dt)
    {
        if (buildingData.type != BuildingType.House || runtimePopulationGrowth <= 0) return;
        if (currentUnitType == null && buildingData.populationUnitType != null) currentUnitType = buildingData.populationUnitType; // Проверка
        if (currentUnitType == null) return; // Не растем без типа

        if (currentUnits < runtimeMaxCapacity)
        {
            populationGrowthProgress += runtimePopulationGrowth * dt;
            if (populationGrowthProgress >= 1.0f)
            {
                int unitsToAdd = Mathf.FloorToInt(populationGrowthProgress);
                currentUnits = Mathf.Min(currentUnits + unitsToAdd, runtimeMaxCapacity);
                populationGrowthProgress -= unitsToAdd;
                UpdateUnitText();
            }
        } else { populationGrowthProgress = 0f; }
    }

    // Генерация маны
    void HandleManaGeneration(float dt)
    {
        if (buildingData.type != BuildingType.Altar || runtimeManaPerSecond <= 0 || ResourceManager.Instance == null) return;
        ResourceManager.Instance.AddMana(this.race, runtimeManaPerSecond * dt);
    }

    // Спавн юнитов
    void HandleUnitSpawning(float dt)
    {
        if (currentUnitType == null || runtimeSpawnTime <= 0) return;
        if (buildingData.type != BuildingType.Altar && buildingData.type != BuildingType.Tower && buildingData.type != BuildingType.Stable) return;

        if (currentUnits < runtimeMaxCapacity)
        {
            unitSpawnProgress += dt;
            if (unitSpawnProgress >= runtimeSpawnTime)
            {
                // TODO: Resource check?
                currentUnits++;
                unitSpawnProgress = 0f;
                UpdateUnitText();
            }
        } else { unitSpawnProgress = 0f; }
    }

    // Атака Башни
    void HandleTowerAttack(float dt)
    {
        if (buildingData.type != BuildingType.Tower || !runtimeCanAttack || runtimeAttackRange <= 0) return;

        attackCooldownTimer -= dt;
        if (attackCooldownTimer <= 0)
        {
            UnitGroup targetUnitGroup = FindClosestEnemyUnitGroup();
            if (targetUnitGroup != null)
            {
                // Debug.Log($"{gameObject.name} attacking {targetUnitGroup.name}"); // Раскомментировать для лога атаки
                targetUnitGroup.TakeDamage(runtimeAttackDamage, this);
                attackCooldownTimer = runtimeAttackCooldown;
                // TODO: Visual effect
            }
        }
    }

    // Поиск цели для Башни
    UnitGroup FindClosestEnemyUnitGroup()
    {
        Collider2D[] targetsInRange = Physics2D.OverlapCircleAll(transform.position, runtimeAttackRange, enemyLayerMask);
        UnitGroup closestTarget = null;
        float minDistanceSqr = float.MaxValue;
        foreach (Collider2D targetCollider in targetsInRange)
        {
            UnitGroup unitGroup = targetCollider.GetComponent<UnitGroup>();
            if (unitGroup != null && unitGroup.UnitRace != Race.Neutral && unitGroup.UnitRace != this.race)
            {
                float distanceSqr = (targetCollider.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr < minDistanceSqr)
                {
                    minDistanceSqr = distanceSqr;
                    closestTarget = unitGroup;
                }
            }
        }
        return closestTarget;
    }

    #endregion

    #region Visuals & Selection

    // Обновление цвета здания
    void UpdateBuildingVisuals()
    {
        originalColor = RaceStats.GetColor(this.race);
        if (buildingSpriteRenderer != null)
        {
            buildingSpriteRenderer.color = isSelected ? selectionColor : originalColor;
        }
    }

    // Обновление цвета при изменении выделения
    void UpdateSelectionVisuals()
    {
        if (buildingSpriteRenderer == null) return;
        Color targetColor = isSelected ? selectionColor : originalColor;
        // Обновляем цвет только если он изменился, для оптимизации
        if (buildingSpriteRenderer.color != targetColor)
        {
            buildingSpriteRenderer.color = targetColor;
        }
    }

    // Обновление текста юнитов
    void UpdateUnitText()
    {
        if (unitCountText != null)
        {
            unitCountText.text = currentUnits.ToString();
        }
    }

    // Выделение здания
    public void SelectBuilding()
    {
        isSelected = true;
        UpdateSelectionVisuals();
    }

    // Снятие выделения
    public void DeselectBuilding()
    {
        isSelected = false;
        UpdateSelectionVisuals();
    }

    #endregion

    #region Unit Transfer & Combat

    // Отправка юнитов
    public void LaunchUnits(Building targetBuilding)
    {
        if (race == Race.Neutral || currentUnits <= 0 || currentUnitType == null || targetBuilding == this || currentUnitType.unitGroupPrefab == null) return;

        int unitsToSend = Mathf.Max(1, currentUnits / 2);
        currentUnits -= unitsToSend;
        UpdateUnitText();

        GameObject unitGroupGO = Instantiate(currentUnitType.unitGroupPrefab, transform.position, Quaternion.identity);
        UnitGroup unitGroupScript = unitGroupGO.GetComponent<UnitGroup>();

        if (unitGroupScript != null)
        {
            unitGroupScript.Initialize(
                unitsToSend, this.race, this.currentUnitType, this, targetBuilding,
                currentUnitType.moveSpeed, currentUnitType.groupSprite, RaceStats.GetColor(this.race)
            );
        }
        else
        {
            Debug.LogError($"UnitGroup script not found on prefab '{currentUnitType.unitGroupPrefab.name}'!", unitGroupGO);
            Destroy(unitGroupGO);
            currentUnits += unitsToSend; // Возвращаем юнитов
            UpdateUnitText();
        }
    }

    // Прием юнитов (подкрепление или бой)
    public void ReceiveUnits(int incomingUnits, Race senderRace, UnitData attackerUnitData)
    {
        if (buildingData == null || !isInitialized || attackerUnitData == null) return;

        if (senderRace == this.race) // Подкрепление
        {
            int oldUnits = currentUnits;
            currentUnits = Mathf.Min(currentUnits + incomingUnits, runtimeMaxCapacity); // Используем runtimeMaxCapacity
            if(currentUnits > oldUnits) UpdateUnitText(); // Обновляем текст, только если число изменилось
        }
        else // Атака
        {
            int attackerAttack = Mathf.Max(1, attackerUnitData.attackStat);
            int defenderDefense;
            UnitData defenderUnitType = this.currentUnitType;

            if (this.race == Race.Neutral || currentUnits <= 0 || defenderUnitType == null) {
                 defenderDefense = Mathf.Max(1, RaceStats.GetDefense(this.race)); // Базовая защита
            } else {
                 defenderDefense = Mathf.Max(1, defenderUnitType.defenseStat); // Защита юнитов
            }

            int defendersLost = Mathf.CeilToInt((float)(incomingUnits * attackerAttack) / defenderDefense);
            int attackersLost = Mathf.CeilToInt((float)(currentUnits * defenderDefense) / attackerAttack);
            defendersLost = Mathf.Min(defendersLost, currentUnits);
            attackersLost = Mathf.Min(attackersLost, incomingUnits);

            int remainingAttackers = incomingUnits - attackersLost;
            int remainingDefenders = currentUnits - defendersLost;

            if (remainingDefenders <= 0) // Защитники пали
            {
                if (remainingAttackers > 0) // Захват
                {
                    Race oldRace = this.race;
                    int oldLevel = 1; // Уровень по умолчанию, если нет менеджера
                    if(PlayerProgressManager.Instance != null) {
                         oldLevel = PlayerProgressManager.Instance.GetBuildingLevel(oldRace, buildingData.type);
                    }

                    this.race = senderRace; // Новая раса
                    this.currentUnits = remainingAttackers; // Заселяем атакующих
                    int capturedLevel = Mathf.Max(1, oldLevel); // Уровень сохраняется (мин 1)

                    // Обновляем прогресс для новой расы
                    if (PlayerProgressManager.Instance != null) {
                         PlayerProgressManager.Instance.SetBuildingLevel(this.race, buildingData.type, capturedLevel);
                         // Исследование считается полученным при захвате
                         if(buildingData.type != BuildingType.House) PlayerProgressManager.Instance.MarkBuildingResearched(this.race, buildingData.type);
                    }

                    // Переинициализируем здание для новой расы и уровня
                    isInitialized = false; // Сбрасываем флаг для повторной инициализации
                    InitializeBuilding(); // Вызываем полную инициализацию
                    Debug.Log($"Building '{name}' captured by {senderRace} from {oldRace}. Level: {capturedLevel}");
                } else { // Нейтрализация
                    this.race = Race.Neutral;
                    this.currentUnits = 0;
                    this.currentUnitType = null;
                    isInitialized = false; // Сбрасываем флаг
                    InitializeBuilding(); // Инициализируем как нейтральное
                    Debug.Log($"Building '{name}' neutralized.");
                }
            } else { // Атака отбита
                currentUnits = remainingDefenders;
                UpdateUnitText();
            }
        }
        if (currentUnits < 0) { currentUnits = 0; UpdateUnitText(); } // Финальная проверка
    }

    #endregion

    #region Public Accessors (Для GameManager и других)

    // Доступ к флагу startsAsPlayerBuilding (используя Reflection в GameManager или сделав это public)
    // Если делать public: public bool StartsAsPlayerBuilding => startsAsPlayerBuilding;

    #endregion

} // -- Конец класса Building --