using Godot;
using System.Collections.Generic;

public partial class GameSceneController : Node
{
    [Export] public NodePath DungeonRendererPath;
    [Export] public NodePath FogRendererPath;
    [Export] public NodePath EntityManagerPath;
    [Export] public NodePath InputHandlerPath;
    [Export] public NodePath MonsterAIPath;
    [Export] public NodePath CameraPath;
    [Export] public NodePath UIPath;

    private DungeonRenderer DungeonRenderer;
    private FogOfWarRenderer FogRenderer;
    private EntityManager EntityManager;
    private InputHandler InputHandler;
    private MonsterAI MonsterAI;
    private CameraController Camera;
    private GameUI UI;

    private readonly Biome _biome = Biome.Sewers;
    private readonly int _targetRooms = 8;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        DungeonRenderer = GetNode<DungeonRenderer>(DungeonRendererPath);
        FogRenderer = GetNode<FogOfWarRenderer>(FogRendererPath);
        EntityManager = GetNode<EntityManager>(EntityManagerPath);
        InputHandler = GetNode<InputHandler>(InputHandlerPath);
        MonsterAI = GetNode<MonsterAI>(MonsterAIPath);
        Camera = GetNode<CameraController>(CameraPath);
        UI = GetNode<GameUI>(UIPath);
        CallDeferred(nameof(StartGame));
    }

    private void StartGame()
    {

        GD.Print("\n=== INICIANDO FETENQUEST - FASE 2 ===\n");

        GameState.Instance.StartRun();

        DungeonGenerator.Instance.GenerateDungeon(_biome, _targetRooms);
        DungeonRenderer.RenderDungeon(DungeonGenerator.Instance.Rooms);

        int mw = DungeonRenderer.MapWidth;
        int mh = DungeonRenderer.MapHeight;

        FogOfWarSystem.Instance.Initialize(mw, mh);
        FogRenderer.InitializeFog(mw, mh);

        TreasureSystem.Instance.InitializeDeck();

        // Configurar limites de camara
        Camera.SetMapBounds(Vector2.Zero, new Vector2(mw * DungeonRenderer.TILE_SIZE, mh * DungeonRenderer.TILE_SIZE));

        // Conectar signals
        ConnectSignals();

        // Crear Barbarian y colocarlo en la sala inicial
        var start = DungeonGenerator.Instance.StartRoom;
        var playerPos = start.GridOffset + start.Template.PlayerSpawnPoint;

        var barbarian = new MercenaryInstance();
        barbarian.Initialize(MercenaryClass.Barbarian);
        EntityManager.SpawnMercenary(barbarian, playerPos);
        TurnManager.Instance.RegisterMercenary(barbarian);

        // Camera sigue al mercenario
        var visual = EntityManager.GetVisual(barbarian);
        Camera.SetTarget(visual);
        Camera.Position = visual.Position;

        // Revelar la sala de inicio
        FogOfWarSystem.Instance.RevealRoom(DungeonRenderer.GetRoomCells(start));
        start.IsRevealed = true;

        // Spawnear todos los monstruos de cada sala
        foreach (var room in DungeonGenerator.Instance.Rooms)
        {
            SpawnMonstersInRoom(room);
        }

        // UI init
        UI.Initialize();

        // Iniciar combate (primer turno)
        TurnManager.Instance.StartCombat();
    }

    private void SpawnMonstersInRoom(DungeonRoom room)
    {
        if (room.Monsters.Count == 0) return;
        var spawnPts = room.Template.MonsterSpawnPoints;

        for (int i = 0; i < room.Monsters.Count; i++)
        {
            var monster = room.Monsters[i];
            Vector2I local = i < spawnPts.Count ? spawnPts[i] : new Vector2I(1 + i, 1);
            var global = room.GridOffset + local;

            // Evitar solapamiento
            while (GridManager.Instance.IsOccupied(global) || !GridManager.Instance.IsWalkable(global))
            {
                var cells = DungeonRenderer.GetRoomCells(room);
                Vector2I fallback = global;
                foreach (var c in cells)
                {
                    if (!GridManager.Instance.IsOccupied(c) && GridManager.Instance.IsWalkable(c))
                    {
                        fallback = c;
                        break;
                    }
                }
                if (fallback == global) break;
                global = fallback;
                break;
            }

            EntityManager.SpawnMonster(monster, global);
            TurnManager.Instance.RegisterMonster(monster);
        }
    }

    private void ConnectSignals()
    {
        TurnManager.Instance.TurnStarted += OnTurnStarted;
        TurnManager.Instance.MercenaryMovementUpdated += UI.OnMovementUpdated;
        TurnManager.Instance.MonsterPhaseStarted += MonsterAI.OnMonsterPhaseStarted;

        FogOfWarSystem.Instance.CellRevealed += FogRenderer.OnCellRevealedSignal;
        FogOfWarSystem.Instance.CellRevealed += OnCellRevealed;

        ChaosSystem.Instance.ThresholdReached += UI.ShowChaosNotif;
        ChaosSystem.Instance.WanderingMonsterRequested += OnWanderingMonsterRequested;

        GameState.Instance.ChaosChanged += UI.OnChaosChanged;
    }

    private void OnTurnStarted(string entityName)
    {
        UI.OnTurnStarted(entityName);

        var current = TurnManager.Instance.GetCurrentMercenary();
        if (current != null)
        {
            InputHandler.OnMercenaryTurnStarted(current);
            var visual = EntityManager.GetVisual(current);
            if (visual != null) Camera.SetTarget(visual);
        }
        else
        {
            InputHandler.OnMonsterPhaseStarted();
        }
    }

    private void OnCellRevealed(Vector2I pos)
    {
        EntityManager.OnCellRevealed(pos);
        // No redibujar overlays por cada celda revelada — al abrir una sala
        // o recorrer un pasillo esto dispara decenas de BFS+redraw por frame
        // y la mazmorra se congela. El refresh final lo hace MoveMercenaryToAsync.
        if (!InputHandler.Instance.InputBlocked && InputHandler.Instance.SelectedMercenary != null)
            InputHandler.RefreshOverlays();
    }

    private void OnWanderingMonsterRequested()
    {
        // Escoger una sala revelada aleatoria y spawnear un monstruo basico
        var revealed = new List<DungeonRoom>();
        foreach (var r in DungeonGenerator.Instance.Rooms)
            if (r.IsRevealed && r.Template.Type != RoomType.StartExit)
                revealed.Add(r);

        if (revealed.Count == 0)
        {
            // Fallback: la sala de inicio
            revealed.Add(DungeonGenerator.Instance.StartRoom);
        }

        var room = revealed[_rng.RandiRange(0, revealed.Count - 1)];

        var monster = new MonsterInstance();
        monster.Initialize("Errante", body: 3, mind: 0,
            attack: 2, defense: 1, MonsterBehavior.Aggressive,
            hasAggressiveTrait: true);
        monster.HomeRoom = room;

        // Buscar celda libre en la sala
        var cells = DungeonRenderer.GetRoomCells(room);
        Vector2I spawnPos = room.GridOffset + room.Template.PlayerSpawnPoint;
        foreach (var c in cells)
        {
            if (!GridManager.Instance.IsOccupied(c) && GridManager.Instance.IsWalkable(c))
            {
                spawnPos = c;
                break;
            }
        }

        EntityManager.SpawnMonster(monster, spawnPos);
        TurnManager.Instance.RegisterMonster(monster);
        GameUI.Instance?.AddCombatLog($"Aparece un monstruo errante: {monster.EntityName}", new Color(1f, 0.6f, 0.2f));
    }
}
