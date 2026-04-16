using Godot;

public partial class EscapeSystem : Node
{
    public static EscapeSystem Instance { get; private set; }

    public bool BossDefeated { get; private set; } = false;
    public bool EscapePhaseActive { get; private set; } = false;
    public bool RunCompleted { get; private set; } = false;

    // Profundidad maxima alcanzada (en salas desde la entrada)
    private int _maxDepthReached = 0;

    public override void _Ready()
    {
        Instance = this;
    }

    public void OnBossDefeated()
    {
        BossDefeated = true;
        EscapePhaseActive = true;
        GD.Print("=== JEFE DERROTADO ===");
        GD.Print("Fase de escape activada. El Contador de Caos sigue subiendo.");
        GD.Print("Llega a la sala de salida para conservar el botin.");
    }

    public void UpdateDepth(int currentDepth)
    {
        if (currentDepth > _maxDepthReached)
            _maxDepthReached = currentDepth;
    }

    // Calcula el porcentaje de oro que se pierde en derrota
    public float CalculateGoldLossPct()
    {
        if (EscapePhaseActive)
        {
            GD.Print("Derrota en fase de escape: pierdes 40% del oro.");
            return 0.40f;
        }

        if (_maxDepthReached >= 5)
        {
            GD.Print("Derrota en salas avanzadas: pierdes 65% del oro.");
            return 0.65f;
        }

        GD.Print("Derrota en salas iniciales: pierdes 90% del oro.");
        return 0.90f;
    }

    public void OnEscapeSuccessful()
    {
        RunCompleted = true;
        GD.Print("=== ESCAPE EXITOSO ===");
        GD.Print("Conservas todo el oro y el inventario de tus mercenarios vivos.");
        ApplyRunGold(lossPercent: 0f);
    }

    public void OnRunFailed()
    {
        float lossPercent = CalculateGoldLossPct();
        ApplyRunGold(lossPercent);
    }

    private void ApplyRunGold(float lossPercent)
    {
        int runGold = GameState.Instance.RunGold;
        int goldLost = Mathf.RoundToInt(runGold * lossPercent);
        int goldKept = runGold - goldLost;

        GD.Print($"Oro de la run: {runGold}");
        GD.Print($"Oro perdido: {goldLost} ({lossPercent * 100:F0}%)");
        GD.Print($"Oro conservado: {goldKept}");

        GameState.Instance.AddPermanentGold(goldKept);
    }
}