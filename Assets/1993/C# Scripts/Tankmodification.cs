using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Общий менеджер прогресса модификации башни.
/// Слушает события от TankModPoint и двигает историю вперёд.
/// </summary>
public class TankModificationManager : MonoBehaviour
{
    public static TankModificationManager Instance { get; private set; }

    [Header("Прогресс")]
    [SerializeField] private List<TankModPoint> modPoints; // назначь в инспекторе по порядку

    [Header("События")]
    public UnityEngine.Events.UnityEvent OnAllModsDone; // перекидывает на следующую сцену/событие

    private int _completedCount = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Все точки кроме первой — заблокированы
        for (int i = 1; i < modPoints.Count; i++)
            modPoints[i].SetLocked(true);

        modPoints[0].SetLocked(false);
    }

    /// <summary>
    /// Вызывается каждым TankModPoint при завершении.
    /// </summary>
    public void OnModCompleted(TankModPoint completed)
    {
        _completedCount++;

        // Разблокируем следующую точку
        int nextIndex = modPoints.IndexOf(completed) + 1;
        if (nextIndex < modPoints.Count)
            modPoints[nextIndex].SetLocked(false);

        if (_completedCount >= modPoints.Count)
        {
            StartCoroutine(DelayedFinish());
        }
    }

    IEnumerator DelayedFinish()
    {
        yield return new WaitForSeconds(1.5f);
        OnAllModsDone?.Invoke();
    }
}

