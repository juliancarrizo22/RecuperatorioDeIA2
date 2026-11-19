using System.Collections.Generic;
using UnityEngine;

public class WolfManager : MonoBehaviour
{
    public static WolfManager Instance { get; private set; }

    private List<WolfAgent> wolves = new List<WolfAgent>();
    public IReadOnlyList<WolfAgent> Wolves => wolves;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterWolf(WolfAgent wolf)
    {
        if (wolf != null && !wolves.Contains(wolf))
        {
            wolves.Add(wolf);
        }
    }

    public void UnregisterWolf(WolfAgent wolf)
    {
        if (wolf != null && wolves.Contains(wolf))
        {
            wolves.Remove(wolf);
        }
    }

    public void BroadcastHowlAlert(WolfAgent emitter, Vector3 alertPosition)
    {
        Debug.Log($"[WolfManager] {emitter.name} difundió una alerta " + $"desde {alertPosition}.");
        foreach (var wolf in wolves)
        {
            if (wolf == null || wolf == emitter) continue;
            wolf.ReceiveHowlAlert(alertPosition);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}