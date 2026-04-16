using Godot;
using System.Collections.Generic;

public partial class TurnManager : Node
{
    public static TurnManager Instance { get; private set; }

    private List<MercenaryInstance> _mercenaries = new();
    private List<MonsterInstance> _monsters = new();

    private int _currentMercenaryIndex = 0;
    private bool _isMercenaryPhase = true;

    [Signal] public delegate void TurnStartedEventHandler(string entityName);
    [Signal] public delegate void AllTurnsEndedEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }

    public void RegisterMercenary(MercenaryInstance m) => _mercenaries.Add(m);
    public void RegisterMonster(MonsterInstance m) => _monsters.Add(m);

    public void StartCombat()
    {
        _currentMercenaryIndex = 0;
        _isMercenaryPhase = true;
        GD.Print("=== COMBATE INICIADO ===");
        StartNextTurn();
    }

    public void StartNextTurn()
    {
        if (_isMercenaryPhase)
        {
            // Buscamos el siguiente mercenario vivo
            while (_currentMercenaryIndex < _mercenaries.Count &&
                   _mercenaries[_currentMercenaryIndex].IsDead)
            {
                _currentMercenaryIndex++;
            }

            if (_currentMercenaryIndex >= _mercenaries.Count)
            {
                // Todos los mercenarios han actuado, turno de monstruos
                _isMercenaryPhase = false;
                StartMonsterPhase();
                return;
            }

            var mercenary = _mercenaries[_currentMercenaryIndex];
            mercenary.StartTurn();
            GameState.Instance.IncrementChaos();
            EmitSignal(SignalName.TurnStarted, mercenary.EntityName);
        }
    }

    public void EndCurrentMercenaryTurn()
    {
        _currentMercenaryIndex++;
        StartNextTurn();
    }

    private void StartMonsterPhase()
    {
        GD.Print("--- Turno de monstruos ---");
        foreach (var monster in _monsters)
        {
            if (monster.IsAlive)
                monster.StartTurn();
        }

        // Volvemos a los mercenarios
        _currentMercenaryIndex = 0;
        _isMercenaryPhase = true;
        EmitSignal(SignalName.AllTurnsEnded);
        GD.Print("--- Fin de ronda ---\n");
    }
}	