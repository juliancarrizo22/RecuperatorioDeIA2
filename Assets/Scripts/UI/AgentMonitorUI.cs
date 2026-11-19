using TMPro;
using UnityEngine;

public class AgentMonitorUI : MonoBehaviour
{
    [SerializeField] private TMP_Text monitorText;

    [Header("Display Settings")]
    [SerializeField] private float refreshInterval = 0.2f;
    [SerializeField] private float messageDuration = 4f;

    private float refreshTimer;

    private void Update()
    {
        refreshTimer += Time.deltaTime;

        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;

        if (AgentStateMonitor.Instance == null ||
            monitorText == null)
            return;

        monitorText.text =
            AgentStateMonitor.Instance.GetCompactReport(
                messageDuration
            );
    }
}

