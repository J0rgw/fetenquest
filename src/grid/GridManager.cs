using Godot;
using System.Collections.Generic;

public enum TileType { Wall, Floor, Corridor, Door, DoorLocked, DoorSealed }

public partial class GridManager : Node
{
    public static GridManager Instance { get; private set; }

    private TileType[,] _grid;
    private Dictionary<Vector2I, Entity> _entityPositions = new();

    public int Width { get; private set; }
    public int Height { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    // Inicializa el grid con un tamaño dado
    public void Initialize(int width, int height)
    {
        Width = width;
        Height = height;
        _grid = new TileType[width, height];
        GD.Print($"Grid inicializado: {width}x{height}");
    }

    // Asigna el tipo de una celda
    public void SetTile(int x, int y, TileType type)
    {
        if (!InBounds(x, y)) return;
        _grid[x, y] = type;
    }

    public TileType GetTile(int x, int y)
    {
        if (!InBounds(x, y)) return TileType.Wall;
        return _grid[x, y];
    }

    public TileType GetTile(Vector2I pos) => GetTile(pos.X, pos.Y);

    // ¿Es una celda transitable?
    public bool IsWalkable(Vector2I pos)
    {
        var tile = GetTile(pos);
        return tile == TileType.Floor ||
               tile == TileType.Corridor ||
               tile == TileType.Door;
    }

    // ¿Está ocupada por una entidad?
    public bool IsOccupied(Vector2I pos) => _entityPositions.ContainsKey(pos);

    public Entity GetEntityAt(Vector2I pos)
    {
        _entityPositions.TryGetValue(pos, out var entity);
        return entity;
    }

    public void PlaceEntity(Entity entity, Vector2I pos)
    {
        if (_entityPositions.ContainsKey(entity.GridPosition))
            _entityPositions.Remove(entity.GridPosition);

        entity.GridPosition = pos;
        _entityPositions[pos] = entity;
    }

    public void RemoveEntity(Entity entity)
    {
        _entityPositions.Remove(entity.GridPosition);
    }

    public bool InBounds(int x, int y) =>
        x >= 0 && y >= 0 && x < Width && y < Height;

    public bool InBounds(Vector2I pos) => InBounds(pos.X, pos.Y);

    // Vecinos ortogonales (4 direcciones)
    public List<Vector2I> GetNeighbors(Vector2I pos)
    {
        var neighbors = new List<Vector2I>();
        Vector2I[] directions = {
            new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
        };
        foreach (var dir in directions)
        {
            var next = pos + dir;
            if (InBounds(next)) neighbors.Add(next);
        }
        return neighbors;
    }

    // Coste de movimiento extra por zona de control de monstruos
    public int GetZoneOfControlCost(Vector2I pos, List<MonsterInstance> monsters)
    {
        int extra = 0;
        Vector2I[] directions = {
            new(0, -1), new(0, 1), new(-1, 0), new(1, 0)
        };
        foreach (var monster in monsters)
        {
            if (!monster.IsAlive) continue;
            foreach (var dir in directions)
            {
                if (monster.GridPosition == pos + dir || monster.GridPosition == pos)
                {
                    extra += monster.AttackDice;
                    GD.Print($"Zona de control de {monster.EntityName}: +{monster.AttackDice} puntos de movimiento");
                }
            }
        }
        return extra;
    }

    // A* Pathfinding
    public List<Vector2I> FindPath(Vector2I from, Vector2I to, List<MonsterInstance> monsters = null)
    {
        var openSet = new PriorityQueue<Vector2I, float>();
        var cameFrom = new Dictionary<Vector2I, Vector2I>();
        var gScore = new Dictionary<Vector2I, float>();
        var fScore = new Dictionary<Vector2I, float>();

        gScore[from] = 0;
        fScore[from] = Heuristic(from, to);
        openSet.Enqueue(from, fScore[from]);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();

            if (current == to)
                return ReconstructPath(cameFrom, current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (!IsWalkable(neighbor)) continue;
                if (IsOccupied(neighbor) && neighbor != to) continue;

                float moveCost = 1f;
                if (monsters != null)
                    moveCost += GetZoneOfControlCost(neighbor, monsters);

                float tentativeG = gScore.GetValueOrDefault(current, float.MaxValue) + moveCost;

                if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, to);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return new List<Vector2I>(); // Sin camino
    }

    // Línea de visión (LOS) — Bresenham
    public bool HasLineOfSight(Vector2I from, Vector2I to)
    {
        int x0 = from.X, y0 = from.Y;
        int x1 = to.X, y1 = to.Y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            if (x0 == x1 && y0 == y1) return true;

            // Si no es el origen ni el destino, comprobamos si bloquea
            if (!(x0 == from.X && y0 == from.Y))
            {
                var tile = GetTile(x0, y0);
                if (tile == TileType.Wall || tile == TileType.DoorLocked || tile == TileType.DoorSealed)
                    return false;
            }

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx)  { err += dx; y0 += sy; }
        }
    }

    private float Heuristic(Vector2I a, Vector2I b) =>
        Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);

    private List<Vector2I> ReconstructPath(Dictionary<Vector2I, Vector2I> cameFrom, Vector2I current)
    {
        var path = new List<Vector2I> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Insert(0, current);
        }
        return path;
    }
}