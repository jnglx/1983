using UnityEngine;

/// <summary>
/// Вешается на невидимый коллайдер рядом с люком башни.
/// Игрок подходит, нажимает E — садится в башню.
/// </summary>
public class TurretEntryPoint : MonoBehaviour, IInteractable
{
    [SerializeField] private TurretController turret;
    [SerializeField] private string _promptText = "Сесть в башню";

    public string PromptText => _promptText;
    public bool IsAvailable => turret != null;

    public void Interact()
    {
        turret.EnterTurret();
    }
}