using Godot;
using System.Collections.Generic;

public class DungeonRoom
{
    public RoomTemplate Template { get; set; }
    public Vector2I GridOffset { get; set; } // Posicion en el mapa global
    public int Depth { get; set; } // Distancia desde la entrada

    public bool IsRevealed { get; set; } = false;
    public bool HasCorridorMonster { get; set; } = false;

    // Conexiones a otras salas (norte, sur, este, oeste)
    public Dictionary<string, DungeonRoom> Connections { get; set; } = new();

    // Monstruos activos en esta sala
    public List<MonsterInstance> Monsters { get; set; } = new();

    public bool IsBossRoom => Template.Type == RoomType.Boss;
    public bool IsStartRoom => Template.Type == RoomType.StartExit;
}