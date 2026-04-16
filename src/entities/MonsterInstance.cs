using Godot;

public enum MonsterBehavior { Territorial, Aggressive }

public partial class MonsterInstance : Entity
{
    public MonsterBehavior Behavior { get; private set; }
    public bool HasAggressiveTrait { get; private set; }
    public Vector2I HomePosition { get; private set; }

    // Se vuelve agresivo permanentemente si:
    // Mente <= 1, tiene trait Aggressive, o tiene LOS a un héroe
    public bool IsPermAgressive { get; private set; } = false;

    public void Initialize(string name, int body, int mind,
        int attack, int defense, MonsterBehavior behavior,
        bool hasAggressiveTrait = false)
    {
        EntityName = name;
        MaxBodyPoints = body; BodyPoints = body;
        MaxMindPoints = mind; MindPoints = mind;
        AttackDice = attack; DefenseDice = defense;
        Behavior = behavior;
        HasAggressiveTrait = hasAggressiveTrait;

        // Conversión automática a agresivo
        if (mind <= 1 || hasAggressiveTrait)
            MakeAggressive();
    }

    public void SetHomePosition(Vector2I pos)
    {
        HomePosition = pos;
        GridPosition = pos;
    }

    public void MakeAggressive()
    {
        if (IsPermAgressive) return;
        IsPermAgressive = true;
        Behavior = MonsterBehavior.Aggressive;
        GD.Print($"{EntityName} se vuelve Agresivo permanentemente.");
    }

    public override void StartTurn()
    {
        GD.Print($"{EntityName} ({Behavior}) inicia su turno.");
    }
}