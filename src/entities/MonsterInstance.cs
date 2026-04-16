using Godot;

public enum MonsterBehavior { Territorial, Aggressive }

public partial class MonsterInstance : Entity
{
    public MonsterBehavior Behavior { get; private set; }
    public bool HasAggressiveTrait { get; private set; }
    public Vector2I HomePosition { get; private set; }
    public bool IsBoss { get; set; } = false;

    // Referencia a la sala de origen (para comportamiento territorial)
    public DungeonRoom HomeRoom { get; set; }

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

    public void BuffAttack(int amount)
    {
        AttackDice += amount;
        GD.Print($"{EntityName} ahora tiene {AttackDice} dados de ataque.");
    }

    public override void StartTurn()
    {
        GD.Print($"{EntityName} ({Behavior}) inicia su turno.");
    }
}
