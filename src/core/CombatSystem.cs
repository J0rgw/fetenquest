using Godot;
using System.Collections.Generic;

public partial class CombatSystem : Node
{
    public static CombatSystem Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    // Resultado de un ataque
    public struct AttackResult
    {
        public int Skulls;
        public int Shields;
        public int Damage;
        public DiceSystem.CombatFace[] AttackRolls;
        public DiceSystem.CombatFace[] DefenseRolls;
    }

    // Ejecuta un ataque de attacker sobre defender
    public AttackResult ExecuteAttack(Entity attacker, Entity defender)
    {
        bool defenderIsMonster = defender is MonsterInstance;

        var atkRolls = DiceSystem.RollCombatDice(attacker.AttackDice);
        var defRolls = DiceSystem.RollCombatDice(defender.DefenseDice);

        int skulls = 0;
        foreach (var d in atkRolls)
            if (d == DiceSystem.CombatFace.Skull) skulls++;

        int shields = 0;
        foreach (var d in defRolls)
        {
            if (d == DiceSystem.CombatFace.BlackShield) shields++;
            else if (d == DiceSystem.CombatFace.WhiteShield && !defenderIsMonster) shields++;
        }

        int damage = Mathf.Max(0, skulls - shields);

        GD.Print($"\n {attacker.EntityName} ataca a {defender.EntityName}");
        GD.Print($"  Ataque: {skulls} calaveras | Defensa: {shields} escudos");
        GD.Print($"  Daño final: {damage}");

        defender.TakeDamage(damage);

        return new AttackResult
        {
            Skulls = skulls,
            Shields = shields,
            Damage = damage,
            AttackRolls = atkRolls,
            DefenseRolls = defRolls
        };
    }

    // Preview de probabilidades antes de confirmar el ataque
    public void ShowAttackPreview(Entity attacker, Entity defender)
    {
        bool defenderIsMonster = defender is MonsterInstance;
        var probs = DiceSystem.CalculateCombatProbabilities(
            attacker.AttackDice, defender.DefenseDice, defenderIsMonster);

        GD.Print($"\n Preview: {attacker.EntityName} → {defender.EntityName}");
        GD.Print($"  {defender.EntityName}: {defender.BodyPoints}/{defender.MaxBodyPoints} cuerpo");

        float expectedDamage = 0f;
        foreach (var kv in probs)
            expectedDamage += kv.Key * kv.Value;

        GD.Print($"  Daño esperado: {expectedDamage:F2}");
        foreach (var kv in probs)
            GD.Print($"  Daño {kv.Key}: {kv.Value * 100:F1}%");
    }
}