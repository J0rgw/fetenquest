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
    [Signal] public delegate void MonsterPhaseStartedEventHandler();
    [Signal] public delegate void MercenaryPhaseStartedEventHandler();
    [Signal] public delegate void MercenaryMovementUpdatedEventHandler();

    public IReadOnlyList<MercenaryInstance> Mercenaries => _mercenaries;
    public IReadOnlyList<MonsterInstance> Monsters => _monsters;
    public bool IsMercenaryPhase => _isMercenaryPhase;

    public override void _Ready()
    {
        Instance = this;
    }

    public void RegisterMercenary(MercenaryInstance m) => _mercenaries.Add(m);
    public void RegisterMonster(MonsterInstance m) => _monsters.Add(m);

    public MercenaryInstance GetCurrentMercenary()
    {
        if (!_isMercenaryPhase) return null;
        if (_currentMercenaryIndex < 0 || _currentMercenaryIndex >= _mercenaries.Count) return null;
        return _mercenaries[_currentMercenaryIndex];
    }

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
            while (_currentMercenaryIndex < _mercenaries.Count &&
                   _mercenaries[_currentMercenaryIndex].IsDead)
            {
                _currentMercenaryIndex++;
            }

            if (_currentMercenaryIndex >= _mercenaries.Count)
            {
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

    public void NotifyMovementChanged()
    {
        EmitSignal(SignalName.MercenaryMovementUpdated);
    }

    private void StartMonsterPhase()
    {
        GD.Print("--- Turno de monstruos ---");
        EmitSignal(SignalName.TurnStarted, "Monstruos");
        EmitSignal(SignalName.MonsterPhaseStarted);
        // Si no hay subscriber (test runner), cerrar fase inmediatamente
        if (!HasConnections(SignalName.MonsterPhaseStarted))
            EndMonsterPhase();
    }

    private bool HasConnections(StringName signal)
    {
        return GetSignalConnectionList(signal).Count > 0;
    }

    public void EndMonsterPhase()
    {
        _currentMercenaryIndex = 0;
        _isMercenaryPhase = true;
        EmitSignal(SignalName.AllTurnsEnded);
        EmitSignal(SignalName.MercenaryPhaseStarted);
        GD.Print("--- Fin de ronda ---\n");
        StartNextTurn();
    }
}
