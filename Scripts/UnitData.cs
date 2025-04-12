using UnityEngine;

// Перечисление для типов юнитов (можно использовать для доп. логики)
public enum UnitType
{
    Peasant,
    Priest,
    Rider,
    HeavyWarrior
    // Можно добавить: Archer, Mage, Worker, etc.
}

[CreateAssetMenu(fileName = "UnitData_New", menuName = "RTS Game/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Identification")]
    public string unitName = "New Unit"; // Имя для отображения (Крестьянин, Священник...)
    public UnitType type;               // Тип юнита из enum

    [Header("Core Stats")]
    [Tooltip("Сила атаки этого типа юнита.")]
    public int attackStat = 1;
    [Tooltip("Защита этого типа юнита.")]
    public int defenseStat = 1;
    [Tooltip("Скорость передвижения этого типа юнита по карте.")]
    public float moveSpeed = 2.0f;

    [Header("Visuals & Prefab")]
    [Tooltip("Префаб, который будет создаваться при отправке группы этих юнитов. Должен содержать скрипт UnitGroup.")]
    public GameObject unitGroupPrefab; // ССЫЛКА НА ПРЕФАБ ГРУППЫ ЮНИТОВ!
    [Tooltip("Иконка для UI (если нужна).")]
    public Sprite icon;

    // --- !!! НОВОЕ ПОЛЕ !!! ---
    [Tooltip("Спрайт, который будет отображаться у ДВИЖУЩЕЙСЯ ГРУППЫ этих юнитов.")]
    public Sprite groupSprite; // <--- Вот оно!
    // ---------------------------

    // Можно добавить еще параметры:
    // public int costToProduce = 10; // Стоимость в ресурсах/мане
    // public float trainingTime = 5.0f; // Время на тренировку (если отличается от spawnTime здания)
    // public int requiredSupply = 1; // Сколько "места" занимает юнит
}