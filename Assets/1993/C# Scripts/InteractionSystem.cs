using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Вешается на камеру игрока (или на пустой объект внутри FPS-контроллера).
/// Raycast смотрит вперёд, ищет IInteractable — показывает подсказку и даёт нажать E.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactLayer;

    [Header("UI")]
    [SerializeField] private GameObject promptUI;       // Canvas с подсказкой "[E] Установить ДЗ"
    [SerializeField] private TextMeshProUGUI promptText;

    private IInteractable _current;

    void Update()
    {
        DetectInteractable();

        if (_current != null && Input.GetKeyDown(KeyCode.E))
        {
            _current.Interact();
        }
    }

    void DetectInteractable()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null && interactable.IsAvailable)
            {
                if (_current != interactable)
                {
                    _current = interactable;
                    ShowPrompt(interactable.PromptText);
                }
                return;
            }
        }

        // Ничего не нашли — сбрасываем
        if (_current != null)
        {
            _current = null;
            HidePrompt();
        }
    }

    void ShowPrompt(string text)
    {
        promptText.text = $"[E] {text}";
        promptUI.SetActive(true);
    }

    void HidePrompt()
    {
        promptUI.SetActive(false);
    }
}

/// <summary>
/// Интерфейс для всех интерактивных объектов в игре.
/// </summary>
public interface IInteractable
{
    string PromptText { get; }
    bool IsAvailable { get; }
    void Interact();
}