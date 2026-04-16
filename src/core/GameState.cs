using Godot;

public partial class GameState : Node
{
	public static GameState Instance { get; private set; }

	// Oro permanente (persiste entre runs)
	public int PermanentGold { get; private set; } = 0;

	// Oro acumulado en la run actual (temporal)
	public int RunGold { get; private set; } = 0;

	// Contador de Caos
	public int ChaosCounter { get; private set; } = 0;

	// Slots de mercenario desbloqueados (empieza en 1)
	public int MercenarySlots { get; private set; } = 1;

	public bool IsRunActive { get; private set; } = false;

	public override void _Ready()
	{
		Instance = this;
		GD.Print("GameState inicializado.");
	}

	public void StartRun()
	{
		RunGold = 0;
		ChaosCounter = 0;
		IsRunActive = true;
		GD.Print("Run iniciada.");
	}

	public void AddRunGold(int amount)
	{
		RunGold += amount;
		GD.Print($"Oro de run: {RunGold}");
	}

	public void AddPermanentGold(int amount)
	{
		PermanentGold += amount;
		GD.Print($"Oro permanente: {PermanentGold}");
	}
	public void IncrementChaos()
	{
		ChaosCounter++;
		GD.Print($"Contador de Caos: {ChaosCounter}");
		ChaosSystem.Instance?.CheckThresholds(ChaosCounter);
	}
}