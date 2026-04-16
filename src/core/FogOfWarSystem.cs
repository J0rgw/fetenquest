using Godot;
using System.Collections.Generic;

public enum CellVisibility { Hidden, CorridorRevealed, RoomRevealed }

public partial class FogOfWarSystem : Node
{
    public static FogOfWarSystem Instance { get; private set; }

    private Dictionary<Vector2I, CellVisibility> _visibility = new();

    [Signal] public delegate void CellRevealedEventHandler(Vector2I pos);
    [Signal] public delegate void RoomRevealedEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }

    public void Initialize(int width, int height)
    {
        _visibility.Clear();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                _visibility[new Vector2I(x, y)] = CellVisibility.Hidden;

        GD.Print("Niebla de guerra inicializada.");
    }

    public CellVisibility GetVisibility(Vector2I pos)
    {
        return _visibility.GetValueOrDefault(pos, CellVisibility.Hidden);
    }

    public bool IsVisible(Vector2I pos)
    {
        return GetVisibility(pos) != CellVisibility.Hidden;
    }

    public void RevealCorridorCell(Vector2I pos)
    {
        if (_visibility.ContainsKey(pos) && _visibility[pos] == CellVisibility.Hidden)
        {
            _visibility[pos] = CellVisibility.CorridorRevealed;
            EmitSignal(SignalName.CellRevealed, pos);
        }
    }

    public void RevealRoom(List<Vector2I> roomCells)
    {
        int revealed = 0;
        foreach (var cell in roomCells)
        {
            if (_visibility.ContainsKey(cell) && _visibility[cell] == CellVisibility.Hidden)
            {
                _visibility[cell] = CellVisibility.RoomRevealed;
                EmitSignal(SignalName.CellRevealed, cell);
                revealed++;
            }
        }
        if (revealed > 0)
            EmitSignal(SignalName.RoomRevealed);
        GD.Print($"Habitacion revelada: {revealed} celdas.");
    }

    public bool CanMonsterActivateThroughDoor(MonsterInstance monster, List<MercenaryInstance> mercenaries)
    {
        foreach (var merc in mercenaries)
        {
            if (!merc.IsAlive) continue;
            if (GridManager.Instance.HasLineOfSight(monster.GridPosition, merc.GridPosition))
            {
                GD.Print($"{monster.EntityName} tiene LOS sobre {merc.EntityName}.");
                return true;
            }
        }
        return false;
    }
}
