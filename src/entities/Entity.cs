using Godot;

public abstract partial class Entity : Node2D
{
    // --- Stats base ---
    public string EntityName { get; protected set; } = "Unknown";
    public int BodyPoints { get; protected set; }
    public int MaxBodyPoints { get; protected set; }
    public int MindPoints { get; protected set; }
    public int MaxMindPoints { get; protected set; }
    public int AttackDice { get; protected set; }
    public int DefenseDice { get; protected set; }

    // --- Posición en el grid ---
    public Vector2I GridPosition { get; set; }

    public bool IsAlive => BodyPoints > 0;

    // --- Recibir daño ---
    public virtual int TakeDamage(int amount)
    {
        int actual = Mathf.Min(amount, BodyPoints);
        BodyPoints -= actual;
        GD.Print($"{EntityName} recibe {actual} de daño. Cuerpo: {BodyPoints}/{MaxBodyPoints}");

        if (!IsAlive)
            OnDeath();

        return actual;
    }

    // --- Curar ---
    public virtual void Heal(int amount)
    {
        BodyPoints = Mathf.Min(BodyPoints + amount, MaxBodyPoints);
        GD.Print($"{EntityName} se cura {amount}. Cuerpo: {BodyPoints}/{MaxBodyPoints}");
    }

    protected virtual void OnDeath()
    {
        GD.Print($"{EntityName} ha muerto.");
    }

    // --- Turno (cada subclase lo implementa) ---
    public abstract void StartTurn();
}