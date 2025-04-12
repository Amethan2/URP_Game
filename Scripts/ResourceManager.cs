using UnityEngine;
using System.Collections.Generic;
using System; // Нужно для Action

public class ResourceManager : MonoBehaviour
{
    // --- Синглтон ---
    public static ResourceManager Instance { get; private set; }

    // --- Настройки ресурсов ---
    [Header("Starting Resources")]
    [Tooltip("Начальное количество маны для каждой не-нейтральной расы.")]
    [SerializeField] private float startingMana = 100f;
    // --- ДОБАВЛЯЕМ Максимальную Ману (для UI) ---
    [Tooltip("Максимальное значение маны, используемое для UI шкалы. Может быть динамическим в будущем.")]
    [SerializeField] private float maxManaForUI = 100f; // Пока фиксированное значение


    // --- Хранилище ресурсов ---
    private Dictionary<Race, float> manaStorage = new Dictionary<Race, float>();

    // --- !!! НОВОЕ СОБЫТИЕ !!! ---
    // Событие вызывается при изменении маны. Передает: Расу, Текущую Ману, Максимальную Ману (для UI)
    public static event Action<Race, float, float> OnManaChanged;
    // -----------------------------

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Опционально
            InitializeResources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeResources()
    {
        manaStorage.Clear();
        foreach (Race race in System.Enum.GetValues(typeof(Race)))
        {
            if (race == Race.Neutral) { manaStorage[race] = 0f; }
            else { manaStorage[race] = startingMana; }

            // --- Вызываем событие при инициализации ---
             OnManaChanged?.Invoke(race, manaStorage[race], maxManaForUI);
            // -----------------------------------------
        }
        Debug.Log("ResourceManager Initialized. Starting Mana: " + startingMana + ", Max Mana (UI): " + maxManaForUI);
    }

    // --- Методы для работы с Маной ---

    public void AddMana(Race race, float amount)
    {
        if (race == Race.Neutral || amount <= 0) return;
        if (manaStorage.ContainsKey(race))
        {
            // Ограничиваем ману максимумом для UI (можно сделать более сложную логику)
            manaStorage[race] = Mathf.Min(manaStorage[race] + amount, maxManaForUI);
            // Debug.Log($"{race} Mana increased by {amount}. Current: {manaStorage[race]}");

            // --- Вызываем событие ---
            OnManaChanged?.Invoke(race, manaStorage[race], maxManaForUI);
            // -----------------------
        }
        // ... (лог ошибки, если расы нет)
    }

    public bool SpendMana(Race race, float amount)
    {
        if (race == Race.Neutral || amount <= 0) return false;
        if (CanAffordMana(race, amount))
        {
            manaStorage[race] -= amount;
            // Debug.Log($"{race} Mana spent: {amount}. Remaining: {manaStorage[race]}");

            // --- Вызываем событие ---
            OnManaChanged?.Invoke(race, manaStorage[race], maxManaForUI);
            // -----------------------
            return true;
        }
        return false;
    }

    public bool CanAffordMana(Race race, float amount)
    {
        if (amount <= 0) return true;
        return manaStorage.TryGetValue(race, out float currentMana) && currentMana >= amount;
    }

    public float GetMana(Race race)
    {
        return manaStorage.TryGetValue(race, out float currentMana) ? currentMana : 0f;
    }

     // Метод для получения максимальной маны (для UI)
     public float GetMaxManaForUI()
     {
         return maxManaForUI;
     }
}