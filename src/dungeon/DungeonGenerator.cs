using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class DungeonGenerator : Node
{
    public static DungeonGenerator Instance { get; private set; }

    private List<DungeonRoom> _rooms = new();
    private DungeonRoom _startRoom;
    private DungeonRoom _bossRoom;

    private readonly RandomNumberGenerator _rng = new();

    // Probabilidad de monstruo en pasillo
    private const float CorridorMonsterChance = 0.30f;

    public IReadOnlyList<DungeonRoom> Rooms => _rooms;
    public DungeonRoom StartRoom => _startRoom;
    public DungeonRoom BossRoom => _bossRoom;

    public override void _Ready()
    {
        Instance = this;
    }

    public void GenerateDungeon(Biome biome, int targetRooms = 10)
    {
        _rooms.Clear();
        GD.Print($"\n=== GENERANDO MAZMORRA: {biome} ({targetRooms} salas) ===");

        // 1. Crear pool de plantillas segun el GDD
        var templates = CreateTemplatePool(biome, targetRooms);

        // 2. Colocar salas en el mapa
        PlaceRooms(templates);

        // 3. Garantizar camino valido inicio → jefe
        EnsureValidPath();

        // 4. Asignar monstruos de pasillo
        AssignCorridorMonsters();

        // 5. Poblar salas con monstruos segun bioma
        PopulateRooms(biome);

        GD.Print($"Mazmorra generada: {_rooms.Count} salas");
        GD.Print($"Sala de inicio: profundidad 0");
        GD.Print($"Sala del jefe: profundidad {_bossRoom.Depth}");
        PrintDungeonMap();
    }

    private List<RoomTemplate> CreateTemplatePool(Biome biome, int targetRooms)
    {
        var pool = new List<RoomTemplate>();

        // Distribucion segun el GDD:
        // ~6-7 combate, 1-2 tesoro, 1-2 evento, 1 jefe, 1 inicio/salida
        int combatRooms = Mathf.Max(4, targetRooms - 4);
        int treasureRooms = _rng.RandiRange(1, 2);
        int eventRooms = _rng.RandiRange(1, 2);

        // Sala de inicio/salida
        pool.Add(RoomTemplate.CreateRect(8, 8, RoomType.StartExit, biome));

        // Salas de combate (variedad de tamanios)
        for (int i = 0; i < combatRooms; i++)
        {
            int w = _rng.RandiRange(6, 10);
            int h = _rng.RandiRange(6, 10);
            pool.Add(RoomTemplate.CreateRect(w, h, RoomType.Combat, biome));
        }

        // Salas de tesoro (sin monstruos)
        for (int i = 0; i < treasureRooms; i++)
            pool.Add(RoomTemplate.CreateRect(6, 6, RoomType.Treasure, biome));

        // Salas de evento
        for (int i = 0; i < eventRooms; i++)
            pool.Add(RoomTemplate.CreateRect(7, 7, RoomType.Event, biome));

        // Sala del jefe (grande)
        pool.Add(RoomTemplate.CreateRect(12, 12, RoomType.Boss, biome));

        // Barajar manteniendo inicio primero y jefe ultimo
        var start = pool[0];
        var boss = pool[pool.Count - 1];
        var middle = pool.Skip(1).Take(pool.Count - 2).ToList();

        for (int i = middle.Count - 1; i > 0; i--)
        {
            int j = _rng.RandiRange(0, i);
            (middle[i], middle[j]) = (middle[j], middle[i]);
        }

        var result = new List<RoomTemplate> { start };
        result.AddRange(middle);
        result.Add(boss);

        return result;
    }

    private void PlaceRooms(List<RoomTemplate> templates)
    {
        // Colocamos salas en una cadena principal con bifurcaciones opcionales
        var mainChain = new List<DungeonRoom>();
        string[] directions = { "east", "south", "north", "west" };

        for (int i = 0; i < templates.Count; i++)
        {
            var room = new DungeonRoom
            {
                Template = templates[i],
                GridOffset = Vector2I.Zero,
                Depth = i
            };

            if (i == 0)
            {
                room.GridOffset = new Vector2I(0, 0);
                _startRoom = room;
            }
            else
            {
                var prev = mainChain[i - 1];
                // Colocamos la sala al este del anterior por defecto
                room.GridOffset = new Vector2I(
                    prev.GridOffset.X + prev.Template.Width + 5,
                    prev.GridOffset.Y
                );

                // Conectar
                prev.Connections["east"] = room;
                room.Connections["west"] = prev;
            }

            mainChain.Add(room);
            _rooms.Add(room);
        }

        _bossRoom = mainChain[mainChain.Count - 1];
    }

    private void EnsureValidPath()
    {
        // Verificar que hay camino desde inicio hasta jefe
        var visited = new HashSet<DungeonRoom>();
        var queue = new Queue<DungeonRoom>();
        queue.Enqueue(_startRoom);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (visited.Contains(current)) continue;
            visited.Add(current);

            foreach (var conn in current.Connections.Values)
                if (!visited.Contains(conn))
                    queue.Enqueue(conn);
        }

        bool pathExists = visited.Contains(_bossRoom);
        GD.Print($"Camino valido inicio → jefe: {pathExists}");

        if (!pathExists)
            GD.PrintErr("ERROR: No hay camino valido al jefe.");
    }

    private void AssignCorridorMonsters()
    {
        int corridorsWithMonsters = 0;
        foreach (var room in _rooms)
        {
            if (!room.Template.CorridorCanHaveMonster) continue;
            if (_rng.Randf() < CorridorMonsterChance)
            {
                room.HasCorridorMonster = true;
                corridorsWithMonsters++;
            }
        }
        GD.Print($"Pasillos con monstruo: {corridorsWithMonsters}");
    }

    private void PopulateRooms(Biome biome)
    {
        foreach (var room in _rooms)
        {
            if (room.Template.Type == RoomType.StartExit) continue;
            if (room.Template.Type == RoomType.Treasure) continue;

            int monsterCount = room.Template.Type switch
            {
                RoomType.Combat => _rng.RandiRange(1, 3),
                RoomType.Boss   => 1,
                RoomType.Event  => _rng.RandiRange(0, 1),
                _               => 0
            };

            for (int i = 0; i < monsterCount; i++)
            {
                bool isBoss = room.Template.Type == RoomType.Boss;
                var monster = CreateMonsterForBiome(biome, isBoss);
                monster.IsBoss = isBoss;
                monster.HomeRoom = room;
                room.Monsters.Add(monster);
            }

            if (room.Monsters.Count > 0)
                GD.Print($"Sala {room.Template.Type} (prof. {room.Depth}): {room.Monsters.Count} monstruos");
        }
    }

    private MonsterInstance CreateMonsterForBiome(Biome biome, bool isBoss)
    {
        var monster = new MonsterInstance();

        if (isBoss)
        {
            switch (biome)
            {
                case Biome.Sewers:
                    monster.Initialize("Rata Gigante", body: 8, mind: 1,
                        attack: 3, defense: 2, MonsterBehavior.Territorial);
                    break;
                case Biome.AbandonedCastle:
                    monster.Initialize("Caballero Muerto", body: 10, mind: 2,
                        attack: 4, defense: 3, MonsterBehavior.Territorial);
                    break;
                case Biome.WizardCaves:
                    monster.Initialize("Brujo", body: 12, mind: 5,
                        attack: 3, defense: 2, MonsterBehavior.Territorial);
                    break;
            }
        }
        else
        {
            switch (biome)
            {
                case Biome.Sewers:
                    bool isZombie = _rng.Randf() > 0.5f;
                    if (isZombie)
                        monster.Initialize("Zombie", body: 4, mind: 0,
                            attack: 2, defense: 1, MonsterBehavior.Territorial,
                            hasAggressiveTrait: true);
                    else
                        monster.Initialize("Goblin", body: 3, mind: 2,
                            attack: 2, defense: 1, MonsterBehavior.Territorial);
                    break;
                case Biome.AbandonedCastle:
                    monster.Initialize("Esqueleto", body: 5, mind: 1,
                        attack: 2, defense: 2, MonsterBehavior.Territorial);
                    break;
                case Biome.WizardCaves:
                    monster.Initialize("Cultista", body: 4, mind: 3,
                        attack: 2, defense: 1, MonsterBehavior.Territorial);
                    break;
            }
        }

        return monster;
    }

    private void PrintDungeonMap()
    {
        GD.Print("\n--- Mapa de la mazmorra ---");
        foreach (var room in _rooms)
        {
            string connections = string.Join(", ", room.Connections.Keys);
            string corridorInfo = room.HasCorridorMonster ? " [pasillo con monstruo]" : "";
            string monsterInfo = room.Monsters.Count > 0 ? $" [{room.Monsters.Count} monstruos]" : "";
            GD.Print($"  [{room.Template.Type}] prof.{room.Depth} " +
                     $"({room.Template.Width}x{room.Template.Height}) " +
                     $"→ {connections}{corridorInfo}{monsterInfo}");
        }
    }
}