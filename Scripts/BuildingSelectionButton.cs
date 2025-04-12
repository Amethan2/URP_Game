using UnityEngine;
using UnityEngine.UI;      // Для доступа к Button и Image
using TMPro;             // Для доступа к TextMeshProUGUI
using System;             // Для использования Action<T>

// Этот скрипт должен быть добавлен на префаб кнопки,
// которая будет отображаться в панели выбора зданий.
public class BuildingSelectionButton : MonoBehaviour
{
    // --- Ссылки на UI элементы внутри кнопки ---
    [Header("UI References")]
    [Tooltip("Компонент Image для отображения иконки здания")]
    [SerializeField] private Image buildingIcon;

    [Tooltip("Компонент TextMeshPro для отображения названия здания")]
    [SerializeField] private TextMeshProUGUI buildingNameText;

    [Tooltip("Компонент Button самой этой кнопки")]
    [SerializeField] private Button button;

    // --- Внутренние переменные ---
    private BuildingType representedType; // Тип здания, который представляет эта кнопка
    private Action<BuildingType> onClickAction; // Действие, которое нужно выполнить при клике

    // --- Инициализация ---
    // Этот метод будет вызываться из MainMenuManager при создании кнопки
    public void Initialize(BuildingData data, Action<BuildingType> clickCallback)
    {
        // Проверка на корректность данных
        if (data == null)
        {
            Debug.LogError("Попытка инициализировать кнопку выбора здания с null данными!", this);
            gameObject.SetActive(false); // Скрываем кнопку, если данные некорректны
            return;
        }
        if (button == null) // Проверка наличия компонента Button
        {
             Debug.LogError("Компонент Button не назначен в BuildingSelectionButton!", this);
             gameObject.SetActive(false);
             return;
        }

        // Сохраняем тип здания и действие для клика
        representedType = data.type;
        onClickAction = clickCallback;

        // Обновляем визуальные элементы кнопки
        if (buildingIcon != null)
        {
            buildingIcon.sprite = data.icon; // Устанавливаем иконку
            buildingIcon.enabled = (data.icon != null); // Показываем/скрываем Image в зависимости от наличия иконки
        }
        if (buildingNameText != null)
        {
            buildingNameText.text = data.buildingName; // Устанавливаем название
        }

        // Настраиваем обработчик нажатия кнопки
        button.onClick.RemoveAllListeners();        // Очищаем предыдущие обработчики (важно при использовании пула объектов)
        button.onClick.AddListener(OnButtonClicked); // Добавляем наш метод

        // Убедимся, что объект активен, если данные корректны
        gameObject.SetActive(true);
    }

    // --- Обработчик Нажатия ---
    // Этот метод вызывается, когда пользователь нажимает на эту кнопку
    private void OnButtonClicked()
    {
        // Вызываем сохраненное действие (onClickAction) и передаем ему
        // тип здания (representedType), который представляет эта кнопка.
        // Это действие (метод SelectBuildingForCurrentSlot в MainMenuManager)
        // обработает выбор пользователя.
        onClickAction?.Invoke(representedType); // ?. - безопасный вызов, если вдруг onClickAction == null
    }
}