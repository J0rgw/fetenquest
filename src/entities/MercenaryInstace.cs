using Godot;
using System.Collections.Generic;

public enum MercenaryClass { Barbarian, Mage, Elf, Dwarf }

public partial class MercenaryInstance : Entity
{
    public MercenaryClass Class { get; private set; }
    public bool IsDead { get; private set; } = false;

    // Inventario: arma, armadura, accesorio + mochila
    public List<string> Inventory { get; private set; } = new();

    // Puntos de movimiento del turno actual
    public int MovementPool { get; private set; } = 0;

    // Si ya atacó este turno
    public bool HasAttackedThisTurn { get; private set; } = false;

    // Si ya buscó este turno
    public bool HasSearchedThisTurn { get; private set; } = false;

    public void Initialize(MercenaryClass mercClass)
    {
        Class = mercClass;
        EntityName = mercClass.ToString();

        // Stats base según clase
        switch (mercClass)
        {
            case MercenaryClass.Barbarian:
                MaxBodyPoints = 8; MaxMindPoints = 2;
                AttackDice = 3; DefenseDice = 2;
                break;
            case MercenaryClass.Mage:
                MaxBodyPoints = 3; MaxMindPoints = 5;
                AttackDice = 1; DefenseDice = 1;
                break;
            case MercenaryClass.Elf:
                MaxBodyPoints = 5; MaxMindPoints = 4;
                AttackDice = 2; DefenseDice = 2;
                break;
            case MercenaryClass.Dwarf:
                MaxBodyPoints = 6; MaxMindPoints = 3;
                AttackDice = 2; DefenseDice = 3;
                break;
        }

        BodyPoints = MaxBodyPoints;
        MindPoints = MaxMindPoints;
    }

    public override void StartTurn()
    {
        // Tirar 2d6 para movimiento
        MovementPool = DiceSystem.RollMovement();
        HasAttackedThisTurn = false;
        HasSearchedThisTurn = false;
        GD.Print($"{EntityName} inicia turno. Movimiento: {MovementPool}");
    }

    public bool SpendMovement(int cost)
    {
        if (MovementPool < cost) return false;
        MovementPool -= cost;
        return true;
    }

    public void RegisterAttack() => HasAttackedThisTurn = true;
    public void RegisterSearch() => HasSearchedThisTurn = true;

    protected override void OnDeath()
    {
        IsDead = true;
        Inventory.Clear(); // Permadeath: se pierde el equipo
        GD.Print($"{EntityName} ha muerto. Su equipo se pierde.");
    }
}