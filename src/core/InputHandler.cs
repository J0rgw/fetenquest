using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public partial class InputHandler : Node
{
    public static InputHandler Instance { get; private set; }

    [Export] public Node2D OverlayContainer;

    public MercenaryInstance SelectedMercenary { get; private set; }
    public bool InputBlocked { get; set; } = false;

    private HashSet<Vector2I> _reachableCells = new();
    private Dictionary<Vector2I, int> _reachableCosts = new();
    private List<Node> _overlayNodes = new();
    private List<Node> _borderNodes = new();

    public override void _Ready()
    {
        Instance = this;
    }

    public void OnMercenaryTurnStarted(MercenaryInstance merc)
    {
        ClearOverlays();
        SelectedMercenary = null;
        InputBlocked = false;
        if (merc != null && merc.IsAlive)
        {
            SelectedMercenary = merc;
            RecalculateReachableCells();
            DrawOverlays();
        }
    }

    public void OnMonsterPhaseStarted()
    {
        SelectedMercenary = null;
        InputBlocked = true;
        ClearOverlays();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (InputBlocked) return;
        if (!TurnManager.Instance.IsMercenaryPhase) return;
        if (@event is not InputEventMouseButton mb || !mb.Pressed) return;
        if (mb.ButtonIndex == MouseButton.Middle ||
            mb.ButtonIndex == MouseButton.WheelUp ||
            mb.ButtonIndex == MouseButton.WheelDown) return;

        var cam = GetViewport().GetCamera2D();
        if (cam == null) return;

        var mouseWorld = cam.GetGlobalMousePosition();
        var gridPos = DungeonRenderer.Instance.WorldToGrid(mouseWorld);

        if (mb.ButtonIndex == MouseButton.Right)
        {
            var current = TurnManager.Instance.GetCurrentMercenary();
            SelectedMercenary = null;
            ClearOverlays();
            // Reseleccionar el mercenario activo automaticamente
            if (current != null && current.IsAlive)
            {
                SelectedMercenary = current;
                RecalculateReachableCells();
                DrawOverlays();
            }
            return;
        }

        if (mb.ButtonIndex != MouseButton.Left) return;

        var entity = GridManager.Instance.GetEntityAt(gridPos);

        if (SelectedMercenary == null)
        {
            if (entity is MercenaryInstance m && m.IsAlive && IsCurrentMercenary(m))
            {
                SelectedMercenary = m;
                RecalculateReachableCells();
                DrawOverlays();
            }
            return;
        }

        // Mercenario ya seleccionado
        if (entity is MonsterInstance monster && monster.IsAlive &&
            IsAdjacent(SelectedMercenary.GridPosition, monster.GridPosition))
        {
            if (SelectedMercenary.HasAttackedThisTurn)
            {
                GameUI.Instance?.AddCombatLog($"{SelectedMercenary.EntityName} ya ha atacado este turno.", new Color(0.9f, 0.7f, 0.3f));
                return;
            }
            GameUI.Instance?.ShowCombatPreview(SelectedMercenary, monster, OnAttackConfirmed);
            return;
        }

        if (_reachableCells.Contains(gridPos))
        {
            _ = MoveMercenaryToAsync(gridPos);
        }
    }

    private async Task MoveMercenaryToAsync(Vector2I target)
    {
        var m = SelectedMercenary;
        if (m == null) return;

        InputBlocked = true;
        ClearOverlays();

        var monsters = GetLivingMonstersList();
        var path = GridManager.Instance.FindPath(m.GridPosition, target, monsters);
        if (path.Count < 2)
        {
            InputBlocked = false;
            RecalculateReachableCells();
            DrawOverlays();
            return;
        }

        var visual = EntityManager.Instance.GetVisual(m);

        for (int i = 1; i < path.Count; i++)
        {
            var next = path[i];
            if (!GridManager.Instance.IsWalkable(next)) break;
            if (GridManager.Instance.IsOccupied(next)) break;

            int cost = 1;
            foreach (var mon in monsters)
            {
                if (!mon.IsAlive) continue;
                if (IsAdjacent(mon.GridPosition, next) || mon.GridPosition == next)
                    cost += mon.AttackDice;
            }

            if (m.MovementPool < cost) break;
            m.SpendMovement(cost);

            GridManager.Instance.PlaceEntity(m, next);
            visual?.UpdatePosition(next);

            var tile = GridManager.Instance.GetTile(next);
            if (tile == TileType.Corridor || tile == TileType.Door)
            {
                FogOfWarSystem.Instance.RevealCorridorCell(next);
            }

            TurnManager.Instance.NotifyMovementChanged();

            if (tile == TileType.Door)
            {
                // Revelar sala conectada
                RevealRoomFromDoor(next);
                EntityManager.Instance.RefreshVisibility();
                await ToSignal(GetTree().CreateTimer(0.15), "timeout");
                break;
            }

            EntityManager.Instance.RefreshVisibility();
            await ToSignal(GetTree().CreateTimer(0.12), "timeout");
        }

        InputBlocked = false;
        if (m.IsAlive && IsCurrentMercenary(m))
        {
            RecalculateReachableCells();
            DrawOverlays();
        }
    }

    private void RevealRoomFromDoor(Vector2I doorPos)
    {
        foreach (var n in GridManager.Instance.GetNeighbors(doorPos))
        {
            var room = DungeonRenderer.Instance.GetRoomAt(n);
            if (room != null && !room.IsRevealed)
            {
                var cells = DungeonRenderer.Instance.GetRoomCells(room);
                FogOfWarSystem.Instance.RevealRoom(cells);
                room.IsRevealed = true;
            }
        }
    }

    private void OnAttackConfirmed(MercenaryInstance att, MonsterInstance def)
    {
        if (att.HasAttackedThisTurn) return;
        att.RegisterAttack();

        var result = CombatSystem.Instance.ExecuteAttack(att, def);
        var defVisual = EntityManager.Instance.GetVisual(def);
        defVisual?.PlayDamageFlash();
        defVisual?.UpdateHealthBar(def.BodyPoints, def.MaxBodyPoints);

        GameUI.Instance?.AddCombatLog(
            $"{att.EntityName}: {result.Skulls} calaveras vs {result.Shields} escudos -> {result.Damage} dano a {def.EntityName}",
            Colors.White);

        if (!def.IsAlive)
        {
            GameUI.Instance?.AddCombatLog($"{def.EntityName} ha muerto.", new Color(1f, 0.3f, 0.3f));
            EntityManager.Instance.OnEntityDied(def);

            // Si es el jefe, activar EscapeSystem
            if (def.IsBoss)
            {
                EscapeSystem.Instance.OnBossDefeated();
                GameUI.Instance?.AddCombatLog("=== JEFE DERROTADO ===", new Color(1f, 0.8f, 0.2f));
            }
        }

        RecalculateReachableCells();
        DrawOverlays();
    }

    private bool IsCurrentMercenary(MercenaryInstance m)
    {
        return TurnManager.Instance.GetCurrentMercenary() == m;
    }

    private bool IsAdjacent(Vector2I a, Vector2I b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y) == 1;
    }

    private List<MonsterInstance> GetLivingMonstersList()
    {
        var list = new List<MonsterInstance>();
        foreach (var m in TurnManager.Instance.Monsters)
            if (m.IsAlive) list.Add(m);
        return list;
    }

    public void RecalculateReachableCells()
    {
        _reachableCells.Clear();
        _reachableCosts.Clear();

        if (SelectedMercenary == null) return;

        var monsters = GetLivingMonstersList();
        int pool = SelectedMercenary.MovementPool;
        var start = SelectedMercenary.GridPosition;

        var dist = new Dictionary<Vector2I, int> { [start] = 0 };
        var queue = new PriorityQueue<Vector2I, int>();
        queue.Enqueue(start, 0);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int currentCost = dist[current];

            foreach (var n in GridManager.Instance.GetNeighbors(current))
            {
                if (!GridManager.Instance.IsWalkable(n)) continue;
                if (GridManager.Instance.IsOccupied(n) && n != start) continue;

                int step = 1;
                foreach (var mon in monsters)
                {
                    if (IsAdjacent(mon.GridPosition, n) || mon.GridPosition == n)
                        step += mon.AttackDice;
                }

                int total = currentCost + step;
                if (total > pool) continue;

                if (!dist.ContainsKey(n) || total < dist[n])
                {
                    dist[n] = total;
                    queue.Enqueue(n, total);
                }
            }
        }

        foreach (var kv in dist)
        {
            if (kv.Key == start) continue;
            _reachableCells.Add(kv.Key);
            _reachableCosts[kv.Key] = kv.Value;
        }
    }

    public void DrawOverlays()
    {
        ClearOverlays();

        if (SelectedMercenary == null) return;

        DrawBorderHighlight(SelectedMercenary.GridPosition, Color.FromHtml("#FFFF00"));

        var monsters = GetLivingMonstersList();
        var overlayColor = new Color(0.2f, 0.3f, 0.4f, 0.4f);

        foreach (var cell in _reachableCells)
        {
            var rect = new ColorRect
            {
                Color = overlayColor,
                Size = new Vector2(32, 32),
                Position = new Vector2(cell.X * 32, cell.Y * 32),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            OverlayContainer.AddChild(rect);
            _overlayNodes.Add(rect);

            bool adjMonster = false;
            foreach (var mon in monsters)
            {
                if (IsAdjacent(mon.GridPosition, cell) || mon.GridPosition == cell)
                {
                    adjMonster = true;
                    break;
                }
            }

            if (adjMonster && _reachableCosts.TryGetValue(cell, out int cost))
            {
                var label = new Label
                {
                    Text = cost.ToString(),
                    Position = new Vector2(cell.X * 32 + 10, cell.Y * 32 + 6),
                    Size = new Vector2(20, 20),
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                label.AddThemeFontSizeOverride("font_size", 10);
                label.AddThemeColorOverride("font_color", new Color(1f, 0.9f, 0.3f));
                OverlayContainer.AddChild(label);
                _overlayNodes.Add(label);
            }
        }

        foreach (var mon in monsters)
        {
            if (IsAdjacent(SelectedMercenary.GridPosition, mon.GridPosition))
                DrawBorderHighlight(mon.GridPosition, Color.FromHtml("#FF3333"));
        }
    }

    private void DrawBorderHighlight(Vector2I pos, Color color)
    {
        int t = 2;
        var parts = new (Vector2 p, Vector2 s)[]
        {
            (new Vector2(pos.X * 32, pos.Y * 32), new Vector2(32, t)),
            (new Vector2(pos.X * 32, pos.Y * 32 + 32 - t), new Vector2(32, t)),
            (new Vector2(pos.X * 32, pos.Y * 32), new Vector2(t, 32)),
            (new Vector2(pos.X * 32 + 32 - t, pos.Y * 32), new Vector2(t, 32)),
        };
        foreach (var (p, s) in parts)
        {
            var r = new ColorRect
            {
                Color = color,
                Position = p,
                Size = s,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            OverlayContainer.AddChild(r);
            _overlayNodes.Add(r);
        }
    }

    private void ClearOverlays()
    {
        foreach (var n in _overlayNodes)
            if (IsInstanceValid(n)) n.QueueFree();
        _overlayNodes.Clear();
    }

    public void RefreshOverlays()
    {
        if (SelectedMercenary != null && TurnManager.Instance.IsMercenaryPhase)
        {
            RecalculateReachableCells();
            DrawOverlays();
        }
    }
}
