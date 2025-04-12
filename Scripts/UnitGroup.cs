using UnityEngine;
using TMPro;

public class UnitGroup : MonoBehaviour
{
    // --- Объявленные поля ---
    private Building targetBuilding;
    private Building sourceBuilding;
    private int unitCount;
    private Race unitRace; // Поле для хранения расы
    private UnitData unitData;
    private float moveSpeed;

    // --- !!! ИСПРАВЛЕНА ОПЕЧАТКА !!! ---
    [Header("Visuals")]
    [SerializeField] private TextMeshPro unitCountTextComponent; // Правильное имя
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool hasArrived = false;

    // --- !!! ДОБАВЛЕНО СВОЙСТВО ДЛЯ ДОСТУПА К РАСЕ !!! ---
    public Race UnitRace => unitRace; // Позволяет другим скриптам (например, Башне) узнать расу этой группы

    void Awake()
    {
        // Если не назначили в инспекторе, пытаемся найти
        if (spriteRenderer == null) {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        // Используем правильное имя переменной при поиске компонента
        if (unitCountTextComponent == null) {
             unitCountTextComponent = GetComponentInChildren<TextMeshPro>();
        }

        // Проверки на наличие компонентов
         if (spriteRenderer == null) Debug.LogError("SpriteRenderer не найден на " + gameObject.name, this);
         // Используем правильное имя переменной в проверке
         if (unitCountTextComponent == null) Debug.LogError("TextMeshPro не найден в/на " + gameObject.name, this);
    }

    // --- Initialize (без изменений, так как он уже корректен) ---
    public void Initialize(int count, Race race, UnitData data, Building source, Building target, float speed, Sprite sprite, Color color)
    {
        // Присваиваем значения полям класса
        this.unitCount = count;
        this.unitRace = race; // <<< Раса сохраняется здесь
        this.unitData = data;
        this.sourceBuilding = source;
        this.targetBuilding = target;
        this.moveSpeed = speed;

        // Используем правильное имя переменной unitCountTextComponent
        if (spriteRenderer == null || unitCountTextComponent == null)
        {
            Debug.LogError($"UnitGroup {gameObject.name} не может быть инициализирован: отсутствует SpriteRenderer или TextMeshPro.", this);
            Destroy(gameObject);
            return;
        }

        // --- 1. Настройка спрайта юнитов ---
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = color;

        // --- 2. Настройка сортировки спрайта юнитов ---
        if (sourceBuilding != null)
        {
            SpriteRenderer sourceRenderer = sourceBuilding.GetComponent<SpriteRenderer>();
            if (sourceRenderer != null) {
                spriteRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            } else {
                spriteRenderer.sortingOrder = 1;
                Debug.LogWarning($"Source building '{sourceBuilding.name}' has no SpriteRenderer for sorting reference.", sourceBuilding);
            }
        } else {
             spriteRenderer.sortingOrder = 1;
             Debug.LogWarning($"Source building was null during UnitGroup Initialize.", this);
        }

        // --- 3. Настройка текста ---
        // Используем правильное имя переменной unitCountTextComponent
        unitCountTextComponent.text = this.unitCount.ToString();
        unitCountTextComponent.color = Color.white;

        // --- 4. Настройка сортировки текста ---
        // Используем правильное имя переменной unitCountTextComponent
        unitCountTextComponent.sortingLayerID = spriteRenderer.sortingLayerID;
        unitCountTextComponent.sortingOrder = spriteRenderer.sortingOrder + 1;
    }

    // --- Update (без изменений) ---
    void Update()
    {
        if (targetBuilding == null || hasArrived || moveSpeed <= 0 || unitData == null) return;

        Vector3 targetPosition = targetBuilding.transform.position;
        targetPosition.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if ((targetPosition - transform.position).sqrMagnitude < 0.04f)
        {
            hasArrived = true;
            targetBuilding.ReceiveUnits(this.unitCount, this.unitRace, this.unitData);
            Destroy(gameObject);
        }
    }

    // --- TakeDamage (без изменений, так как он уже корректен) ---
    public void TakeDamage(float damage, Building attacker)
    {
        if (unitData == null || unitData.defenseStat <= 0 || unitCount <= 0) return;

        int unitsLost = Mathf.CeilToInt(damage / Mathf.Max(1, unitData.defenseStat));
        unitsLost = Mathf.Min(unitsLost, unitCount);
        unitCount -= unitsLost;

        if (unitCount <= 0) {
            Destroy(gameObject);
        } else {
            // Используем правильное имя переменной unitCountTextComponent
            if (unitCountTextComponent != null) {
                unitCountTextComponent.text = unitCount.ToString();
            }
        }
    }
}