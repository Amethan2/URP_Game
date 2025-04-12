using UnityEngine;
using UnityEngine.UI;       // Для Image, Button
using TMPro;                // Для TextMeshProUGUI
using UnityEngine.Events;   // Для UnityAction

public class BuildingSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image buildingIcon;
    [SerializeField] private TextMeshProUGUI buildingNameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI costText; // Текст на кнопке действия

    [Header("Visuals")]
    [Tooltip("Цвет иконки для не построенных/не назначенных зданий")]
    [SerializeField] private Color unavailableIconColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [Tooltip("Спрайт для отображения пустого слота (например, плюсик)")]
    [SerializeField] private Sprite emptySlotSprite; // Опционально

    // Метод для обновления базовой информации слота (иконка, имя)
    public void UpdateSlotInfo(Sprite icon, string name)
    {
        if (buildingIcon != null)
        {
            buildingIcon.sprite = icon;
            // Иконка может быть скрыта/показана через SetIconVisibility
        }
        if (buildingNameText != null)
        {
            buildingNameText.text = name;
            buildingNameText.enabled = true; // Убедимся, что текст видимый
        }
        if (levelText != null)
        {
             levelText.enabled = true; // Уровень тоже видимый
        }
    }

    // Метод для обновления текста уровня
    public void UpdateLevelText(string text)
    {
        if (levelText != null) levelText.text = text;
    }

    // Метод для настройки кнопки действия (текст, активность, функция)
    public void SetupButton(string buttonText, bool interactable, UnityAction action)
    {
        if (costText != null) costText.text = buttonText;
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(true); // Убедимся, что кнопка видима
            actionButton.interactable = interactable;
            actionButton.onClick.RemoveAllListeners(); // Очищаем старые действия
            if (action != null && interactable)
            {
                actionButton.onClick.AddListener(action); // Добавляем новое действие
            }
        }
    }

    // Управляет видимостью/цветом иконки в зависимости от того, построено ли здание
    public void SetIconVisibility(bool isBuiltOrAssigned)
    {
        if (buildingIcon != null)
        {
             // Если есть иконка здания (не пустой слот)
             if (buildingIcon.sprite != emptySlotSprite) {
                  buildingIcon.color = isBuiltOrAssigned ? Color.white : unavailableIconColor;
                  buildingIcon.enabled = true;
             }
            // Если это иконка пустого слота - цвет не меняем, она всегда видима (если активна)
        }
    }

    // --- НОВЫЙ МЕТОД: Показывает состояние ПУСТОГО слота ---
    public void ShowEmptyState()
    {
        if (buildingIcon != null)
        {
            // Показываем специальный спрайт "плюсик" или просто скрываем иконку
            if (emptySlotSprite != null) {
                 buildingIcon.sprite = emptySlotSprite;
                 buildingIcon.color = Color.white; // Стандартный цвет для плюсика
                 buildingIcon.enabled = true;
            } else {
                 buildingIcon.enabled = false; // Скрываем, если нет спрайта для пустого слота
            }
        }
        if (buildingNameText != null) buildingNameText.enabled = false; // Скрываем имя
        if (levelText != null)
        {
            levelText.text = "Пусто"; // Или оставить пустым
            levelText.enabled = true;
        }
        // Кнопку настроит MainMenuManager через SetupButton (текст "Выбрать")
    }

    // --- НОВЫЙ МЕТОД: Показывает состояние ОШИБКИ ---
    public void ShowErrorState(string message)
    {
        if (buildingIcon != null)
        {
             // Можно показать иконку ошибки или просто скрыть
             buildingIcon.enabled = false;
        }
        if (buildingNameText != null)
        {
            buildingNameText.text = "Ошибка";
            buildingNameText.enabled = true;
        }
        if (levelText != null)
        {
            levelText.text = message; // Показываем сообщение об ошибке
            levelText.enabled = true;
        }
        if (actionButton != null)
        {
            actionButton.gameObject.SetActive(false); // Скрываем кнопку при ошибке
        }
    }
}