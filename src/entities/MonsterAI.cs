using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class MonsterAI : Node
{
    public static MonsterAI Instance { get; private set; }

    private const float MoveDelay = 0.15f;
    private const float AttackDelay = 0.3f;

    public override void _Ready()
    {
        Instance = this;
    }

    public async void OnMonsterPhaseStarted()
    {
        await ExecuteMonsterPhase();
    }

    private async Task ExecuteMonsterPhase()
    {
        var monsters = new List<MonsterInstance>(TurnManager.Instance.Monsters);
        var mercs = TurnManager.Instance.Mercenaries;

        foreach (var monster in monsters)
        {
            if (!monster.IsAlive) continue;
            monster.StartTurn();

            // Solo actuar si es visible (esta en celda revelada)
            if (!FogOfWarSystem.Instance.IsVisible(monster.GridPosition)) continue;

            bool hasLos = HasLosToAnyHero(monster, mercs);
            if (monster.MindPoints <= 1 || monster.HasAggressiveTrait || hasLos)
                monster.MakeAggressive();

            await ProcessMonsterTurn(monster, mercs);
        }

        TurnManager.Instance.EndMonsterPhase();
    }

    private async Task ProcessMonsterTurn(MonsterInstance monster, IReadOnlyList<MercenaryInstance> mercs)
    {
        var target = FindNearestHero(monster, mercs);
        if (target == null) return;

        bool inHomeRoom = monster.HomeRoom != null &&
                          DungeonRenderer.Instance.GetRoomAt(target.GridPosition) == monster.HomeRoom;

        if (monster.Behavior == MonsterBehavior.Territorial && !inHomeRoom && !monster.IsPermAgressive)
        {
            // Volver a HomePosition si no hay heroes en la sala
            if (monster.GridPosition != monster.HomePosition)
            {
                var retreatPath = GridManager.Instance.FindPath(monster.GridPosition, monster.HomePosition);
                await MoveAlongPath(monster, retreatPath, monster.AttackDice, null);
            }
            return;
        }

        var path = GridManager.Instance.FindPath(monster.GridPosition, target.GridPosition);
        int maxSteps = monster.AttackDice;
        await MoveAlongPath(monster, path, maxSteps, target.GridPosition);

        if (IsAdjacent(monster.GridPosition, target.GridPosition) && target.IsAlive)
        {
            await Attack(monster, target);
        }
    }

    private async Task MoveAlongPath(MonsterInstance monster, List<Vector2I> path, int maxSteps, Vector2I? stopBefore)
    {
        if (path == null || path.Count <= 1) return;
        var visual = EntityManager.Instance.GetVisual(monster);

        for (int i = 1; i < path.Count && i <= maxSteps; i++)
        {
            var next = path[i];
            if (stopBefore.HasValue && next == stopBefore.Value) break;
            if (!GridManager.Instance.IsWalkable(next)) break;
            if (GridManager.Instance.IsOccupied(next)) break;

            GridManager.Instance.PlaceEntity(monster, next);
            visual?.UpdatePosition(next);

            if (!FogOfWarSystem.Instance.IsVisible(next))
                visual?.SetVisibility(false);
            else
                visual?.SetVisibility(true);

            await ToSignal(GetTree().CreateTimer(MoveDelay), "timeout");
        }
    }

    private async Task Attack(MonsterInstance monster, MercenaryInstance target)
    {
        var result = CombatSystem.Instance.ExecuteAttack(monster, target);
        var targetVisual = EntityManager.Instance.GetVisual(target);
        targetVisual?.PlayDamageFlash();
        targetVisual?.UpdateHealthBar(target.BodyPoints, target.MaxBodyPoints);

        GameUI.Instance?.AddCombatLog(
            $"{monster.EntityName}: {result.Skulls} calaveras vs {result.Shields} escudos -> {result.Damage} dano a {target.EntityName}",
            Colors.White);

        if (!target.IsAlive)
        {
            GameUI.Instance?.AddCombatLog($"{target.EntityName} ha muerto.", new Color(1f, 0.3f, 0.3f));
            EntityManager.Instance.OnEntityDied(target);
        }

        await ToSignal(GetTree().CreateTimer(AttackDelay), "timeout");
    }

    private bool HasLosToAnyHero(MonsterInstance monster, IReadOnlyList<MercenaryInstance> mercs)
    {
        foreach (var h in mercs)
        {
            if (!h.IsAlive) continue;
            if (GridManager.Instance.HasLineOfSight(monster.GridPosition, h.GridPosition))
                return true;
        }
        return false;
    }

    private MercenaryInstance FindNearestHero(MonsterInstance monster, IReadOnlyList<MercenaryInstance> mercs)
    {
        MercenaryInstance best = null;
        float bestDist = float.MaxValue;
        foreach (var h in mercs)
        {
            if (!h.IsAlive) continue;
            float d = Mathf.Abs(monster.GridPosition.X - h.GridPosition.X) +
                      Mathf.Abs(monster.GridPosition.Y - h.GridPosition.Y);
            if (d < bestDist)
            {
                bestDist = d;
                best = h;
            }
        }
        return best;
    }

    private bool IsAdjacent(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1;
    }
}
