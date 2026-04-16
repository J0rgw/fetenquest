using Godot;
using System.Collections.Generic;

public enum CellVisibility { Hidden, CorridorRevealed, RoomRevealed }

public partial class FogOfWarSystem : Node
{
    public static FogOfWarSystem Instance { get; private set; }

    private Dictionary<Vector2I, CellVisibility> _visibility = new();

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

    // Revela una celda de pasillo al moverse por él
    public void RevealCorridorCell(Vector2I pos)
    {
        if (_visibility.ContainsKey(pos) && _visibility[pos] == CellVisibility.Hidden)
        {
            _visibility[pos] = CellVisibility.CorridorRevealed;
            GD.Print($"Pasillo revelado en ({pos.X}, {pos.Y})");
        }
    }

    // Revela toda una habitación de golpe al abrir la puerta
    public void RevealRoom(List<Vector2I> roomCells)
    {
        int revealed = 0;
        foreach (var cell in roomCells)
        {
            if (_visibility.ContainsKey(cell) && _visibility[cell] == CellVisibility.Hidden)
            {
                _visibility[cell] = CellVisibility.RoomRevealed;
                revealed++;
            }
        }
        GD.Print($"¡Habitación revelada! {revealed} celdas descubiertas.");
    }

    // Comprueba si un monstruo agresivo puede activarse por LOS a través de puerta abierta
    public bool CanMonsterActivateThroughDoor(MonsterInstance monster, List<MercenaryInstance> mercenaries)
    {
        foreach (var merc in mercenaries)
        {
            if (!merc.IsAlive) continue;
            if (GridManager.Instance.HasLineOfSight(monster.GridPosition, merc.GridPosition))
            {
                GD.Print($"{monster.EntityName} tiene LOS sobre {merc.EntityName} — se activa.");
                return true;
            }
        }
        return false;
    }
}