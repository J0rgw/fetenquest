using Godot;
using System.Collections.Generic;
public partial class TestRunner : Node
{
	public override void _Ready()
	{
		GD.Print("=== FETENQUEST — TEST FASE 1 ===\n");

		// Crear bárbaro
		var barbarian = new MercenaryInstance();
		barbarian.Initialize(MercenaryClass.Barbarian);
		barbarian.GridPosition = new Vector2I(0, 0);
		TurnManager.Instance.RegisterMercenary(barbarian);

		// Crear zombie (Mente 0, trait Aggressive → se vuelve agresivo solo)
		var zombie = new MonsterInstance();
		zombie.Initialize("Zombie", body: 4, mind: 0,
			attack: 2, defense: 1,
			MonsterBehavior.Territorial, hasAggressiveTrait: true);
		zombie.SetHomePosition(new Vector2I(5, 5));
		TurnManager.Instance.RegisterMonster(zombie);

		// Iniciar run
		GameState.Instance.StartRun();

		// Simular 3 rondas
		GD.Print("\n--- Simulando 3 rondas ---");
		for (int i = 0; i < 3; i++)
		{
			GD.Print($"\n[Ronda {i + 1}]");
			TurnManager.Instance.StartCombat();
			TurnManager.Instance.EndCurrentMercenaryTurn();
		}

		// Test de dados
		GD.Print("\n--- Test de dados ---");
		var rolls = DiceSystem.RollCombatDice(3);
		foreach (var r in rolls)
			GD.Print($"  Dado: {r}");

		// Test de probabilidades (bárbaro 3 ataque vs monstruo 1 defensa)
		GD.Print("\n--- Probabilidades bárbaro vs monstruo ---");
		var probs = DiceSystem.CalculateCombatProbabilities(3, 1, defenderIsMonster: true);
		foreach (var kv in probs)
			GD.Print($"  Daño {kv.Key}: {kv.Value * 100:F1}%");

		// Test combate
		GD.Print("\n--- Test de combate ---");
		var atkDice = DiceSystem.RollCombatDice(barbarian.AttackDice);
		int skulls = 0;
		foreach (var d in atkDice)
			if (d == DiceSystem.CombatFace.Skull) skulls++;

		var defDice = DiceSystem.RollCombatDice(zombie.DefenseDice);
		int shields = 0;
		foreach (var d in defDice)
			if (d == DiceSystem.CombatFace.BlackShield) shields++;

		int damage = Mathf.Max(0, skulls - shields);
		GD.Print($"Bárbaro ataca: {skulls} calaveras");
		GD.Print($"Zombie defiende: {shields} escudos negros");
		GD.Print($"Daño final: {damage}");
		zombie.TakeDamage(damage);

		// Test de grid y pathfinding
		GD.Print("\n--- Test de Grid y Pathfinding ---");
		GridManager.Instance.Initialize(10, 10);

		// Crear un pasillo simple
		for (int x = 0; x < 10; x++)
			GridManager.Instance.SetTile(x, 5, TileType.Corridor);

		GridManager.Instance.PlaceEntity(barbarian, new Vector2I(0, 5));
		GridManager.Instance.PlaceEntity(zombie, new Vector2I(8, 5));

		var path = GridManager.Instance.FindPath(new Vector2I(0, 5), new Vector2I(8, 5));
		GD.Print($"Camino encontrado: {path.Count} pasos");
		foreach (var step in path)
			GD.Print($"  → ({step.X}, {step.Y})");

		// Test LOS
		bool los = GridManager.Instance.HasLineOfSight(new Vector2I(0, 5), new Vector2I(8, 5));
		GD.Print($"Línea de visión bárbaro → zombie: {los}");


		// Test FogOfWar
		GD.Print("\n--- Test de Niebla de Guerra ---");
		FogOfWarSystem.Instance.Initialize(10, 10);

		// Revelar el pasillo celda a celda
		GD.Print("Mercenario avanza por el pasillo...");
		for (int x = 0; x <= 4; x++)
			FogOfWarSystem.Instance.RevealCorridorCell(new Vector2I(x, 5));

		// Revelar habitación al abrir puerta
		var roomCells = new List<Vector2I>();
		for (int x = 6; x < 10; x++)
			for (int y = 3; y < 8; y++)
				roomCells.Add(new Vector2I(x, y));

		FogOfWarSystem.Instance.RevealRoom(roomCells);

		// Test visibilidad
		GD.Print($"Celda (2,5) visible: {FogOfWarSystem.Instance.IsVisible(new Vector2I(2, 5))}");
		GD.Print($"Celda (9,9) visible: {FogOfWarSystem.Instance.IsVisible(new Vector2I(9, 9))}");

		// Test CombatSystem con preview
		GD.Print("\n--- Test de CombatSystem ---");
		CombatSystem.Instance.ShowAttackPreview(barbarian, zombie);
		CombatSystem.Instance.ExecuteAttack(barbarian, zombie);

		// Test EscapeSystem
		GD.Print("\n--- Test de EscapeSystem ---");
		GameState.Instance.AddRunGold(100);
		EscapeSystem.Instance.UpdateDepth(3);
		EscapeSystem.Instance.OnRunFailed();

		GD.Print("\n--- Simulando escape exitoso ---");
		GameState.Instance.StartRun();
		GameState.Instance.AddRunGold(200);
		EscapeSystem.Instance.OnBossDefeated();
		EscapeSystem.Instance.OnEscapeSuccessful();

		// Test TreasureSystem
		GD.Print("\n--- Test de TreasureSystem ---");
		TreasureSystem.Instance.InitializeDeck();
		var roomPos = new Vector2I(3, 3);
		for (int i = 0; i < 5; i++)
			TreasureSystem.Instance.DrawCard(roomPos);


		// Test DungeonGenerator
		GD.Print("\n--- Test de DungeonGenerator ---");
		DungeonGenerator.Instance.GenerateDungeon(Biome.Sewers, targetRooms: 8);
	}


}