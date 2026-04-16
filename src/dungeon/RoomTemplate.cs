using Godot;
using System.Collections.Generic;

public enum RoomType { Combat, Treasure, Event, Boss, Shop, StartExit }
public enum Biome { Sewers, AbandonedCastle, WizardCaves }

public partial class RoomTemplate : Resource
{
    // Tipo y bioma
    public RoomType Type { get; set; }
    public Biome Biome { get; set; }

    // Dimensiones del layout
    public int Width { get; set; }
    public int Height { get; set; }

    // Layout: true = floor, false = wall
    public bool[,] Layout { get; set; }

    // Conexiones posibles (norte, sur, este, oeste)
    public bool CanConnectNorth { get; set; }
    public bool CanConnectSouth { get; set; }
    public bool CanConnectEast { get; set; }
    public bool CanConnectWest { get; set; }

    // Spawn points
    public List<Vector2I> MonsterSpawnPoints { get; set; } = new();
    public List<Vector2I> FurnitureSpawnPoints { get; set; } = new();
    public List<Vector2I> TrapSpawnPoints { get; set; } = new();
    public Vector2I PlayerSpawnPoint { get; set; }

    // El pasillo de salida puede tener monstruo
    public bool CorridorCanHaveMonster { get; set; } = true;

    // Crea un layout rectangular simple
    public static RoomTemplate CreateRect(int width, int height,
        RoomType type, Biome biome)
    {
        var template = new RoomTemplate
        {
            Width = width,
            Height = height,
            Type = type,
            Biome = biome,
            Layout = new bool[width, height],
            CanConnectNorth = true,
            CanConnectSouth = true,
            CanConnectEast = true,
            CanConnectWest = true
        };

        // Todo floor por defecto
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                template.Layout[x, y] = true;

        // Spawn points basicos
        template.PlayerSpawnPoint = new Vector2I(width / 2, height / 2);

        if (type == RoomType.Combat)
        {
            template.MonsterSpawnPoints.Add(new Vector2I(1, 1));
            template.MonsterSpawnPoints.Add(new Vector2I(width - 2, 1));
            template.MonsterSpawnPoints.Add(new Vector2I(width / 2, height - 2));
            template.FurnitureSpawnPoints.Add(new Vector2I(1, height - 2));
            template.TrapSpawnPoints.Add(new Vector2I(width / 2, height / 2));
        }
        else if (type == RoomType.Treasure)
        {
            template.FurnitureSpawnPoints.Add(new Vector2I(1, 1));
            template.FurnitureSpawnPoints.Add(new Vector2I(width - 2, 1));
            template.FurnitureSpawnPoints.Add(new Vector2I(1, height - 2));
            template.FurnitureSpawnPoints.Add(new Vector2I(width - 2, height - 2));
            template.CorridorCanHaveMonster = false;
        }

        return template;
    }
}