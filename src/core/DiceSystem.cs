using Godot;
using System.Collections.Generic;

public static class DiceSystem
{
    private static readonly RandomNumberGenerator _rng = new();

    // Resultado de un dado de combate
    public enum CombatFace { Skull, WhiteShield, BlackShield }

    // Tira N dados de combate, devuelve la cara de cada uno
    public static CombatFace[] RollCombatDice(int n)
    {
        var results = new CombatFace[n];
        for (int i = 0; i < n; i++)
        {
            int roll = _rng.RandiRange(1, 6);
            results[i] = roll switch
            {
                1 or 2 or 3 => CombatFace.Skull,
                4 or 5      => CombatFace.WhiteShield,
                6           => CombatFace.BlackShield,
                _           => CombatFace.Skull
            };
        }
        return results;
    }

    // Tira 2d6 para movimiento
    public static int RollMovement()
    {
        return _rng.RandiRange(1, 6) + _rng.RandiRange(1, 6);
    }

    // Calcula probabilidades exactas de daño por combinatoria
    public static Dictionary<int, float> CalculateCombatProbabilities(
        int attackDice, int defenseDice, bool defenderIsMonster)
    {
        var distribution = new Dictionary<int, float>();
        int totalCombinations = (int)Mathf.Pow(6, attackDice + defenseDice);

        for (int atkMask = 0; atkMask < (int)Mathf.Pow(6, attackDice); atkMask++)
        {
            int skulls = CountFaces(atkMask, attackDice, CombatFace.Skull);

            for (int defMask = 0; defMask < (int)Mathf.Pow(6, defenseDice); defMask++)
            {
                int shields = defenderIsMonster
                    ? CountFaces(defMask, defenseDice, CombatFace.BlackShield)
                    : CountFaces(defMask, defenseDice, CombatFace.WhiteShield)
                      + CountFaces(defMask, defenseDice, CombatFace.BlackShield);

                int damage = Mathf.Max(0, skulls - shields);
                distribution.TryGetValue(damage, out float current);
                distribution[damage] = current + 1f / totalCombinations;
            }
        }
        return distribution;
    }

    private static int CountFaces(int mask, int numDice, CombatFace target)
    {
        int count = 0;
        for (int i = 0; i < numDice; i++)
        {
            int face = mask % 6 + 1;
            mask /= 6;
            CombatFace result = face switch
            {
                1 or 2 or 3 => CombatFace.Skull,
                4 or 5      => CombatFace.WhiteShield,
                6           => CombatFace.BlackShield,
                _           => CombatFace.Skull
            };
            if (result == target) count++;
        }
        return count;
    }
}