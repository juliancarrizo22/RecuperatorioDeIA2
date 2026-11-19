using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class AgentStateMonitor : MonoBehaviour
{
    public static AgentStateMonitor Instance { get; private set; }

    public class AgentData
    {
        public string Name;
        public string Type;
        public string State;
    }

    private readonly Dictionary<int, AgentData> agents = new();
    private readonly Queue<string> eventLog = new();
    private string temporaryMessage = "";
    private float temporaryMessageTime;

    [SerializeField] private int maxEvents = 12;

    public IReadOnlyDictionary<int, AgentData> Agents => agents;
    public IEnumerable<string> Events => eventLog;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterAgent(GameObject agent, string type, string state)
    {
        if (agent == null)
            return;

        int id = agent.GetInstanceID();

        agents[id] = new AgentData
        {
            Name = agent.name,
            Type = type,
            State = state
        };

        AddEvent($"{agent.name} registrado ({type})");

        if (type != "Hunter")
        {
            temporaryMessage = $"{agent.name}: {state}";
            temporaryMessageTime = Time.time;
        }
    }

    public void UpdateAgentState(GameObject agent, string newState)
    {
        if (agent == null)
            return;

        int id = agent.GetInstanceID();

        if (!agents.TryGetValue(id, out AgentData data))
            return;

        if (data.State == newState)
            return;

        string previousState = data.State;
        data.State = newState;

        string message =
            $"{data.Name}: {previousState} -> {newState}";

        AddEvent(message);

        // Hunter mantiene su estado permanentemente.
        // Wolves y Boids generan notificaciones temporales.
        if (data.Type != "Hunter")
        {
            temporaryMessage = message;
            temporaryMessageTime = Time.time;
        }
    }

    public void UnregisterAgent(GameObject agent)
    {
        if (agent == null)
            return;

        int id = agent.GetInstanceID();

        if (agents.Remove(id))
            AddEvent($"{agent.name} eliminado");
    }

    public void AddEvent(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");

        eventLog.Enqueue($"{timestamp} | {message}");

        while (eventLog.Count > maxEvents)
            eventLog.Dequeue();
    }

    public string GetMonitorReport()
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("========== AGENT MONITOR ==========");
        report.AppendLine();

        AddAgentSection(report, "BOIDS", "Boid");
        AddAgentSection(report, "WOLVES", "Wolf");
        AddAgentSection(report, "HUNTER", "Hunter");

        report.AppendLine();
        report.AppendLine("========== EVENT LOG ==========");

        foreach (string entry in eventLog)
            report.AppendLine(entry);

        return report.ToString();
    }

    private void AddAgentSection(
        StringBuilder report,
        string title,
        string type)
    {
        report.AppendLine($"--- {title} ---");

        int count = 0;

        foreach (AgentData agent in agents.Values)
        {
            if (agent.Type != type)
                continue;

            report.AppendLine($"{agent.Name} | {agent.State}");
            count++;
        }

        report.AppendLine($"Total: {count}");
        report.AppendLine();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public string GetCompactReport(float messageDuration)
    {
        StringBuilder report = new StringBuilder();

        report.AppendLine("===== AGENT MONITOR =====");
        report.AppendLine();

        report.AppendLine("--- HUNTER ---");

        bool hunterFound = false;

        foreach (AgentData agent in agents.Values)
        {
            if (agent.Type != "Hunter")
                continue;

            report.AppendLine($"{agent.Name}: {agent.State}");
            hunterFound = true;
        }

        if (!hunterFound)
            report.AppendLine("Esperando Hunter...");

        report.AppendLine();

        // Mostrar el último cambio durante unos segundos.
        if (!string.IsNullOrEmpty(temporaryMessage) &&
            Time.time - temporaryMessageTime < messageDuration)
        {
            report.AppendLine("--- AGENT ACTIVITY ---");
            report.AppendLine(temporaryMessage);
        }

        return report.ToString();
    }
}

