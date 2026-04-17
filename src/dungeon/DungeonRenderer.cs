using Godot;
using System.Collections.Generic;

public partial class DungeonRenderer : Node2D
{
    public static DungeonRenderer Instance { get; private set; }

    public const int TILE_SIZE = 32;

    [Export] public NodePath DungeonMapPath;
    private TileMapLayer DungeonMap;

    public int MapWidth { get; private set; }
    public int MapHeight { get; private set; }

    private Dictionary<Vector2I, DungeonRoom> _cellToRoom = new();
    private Dictionary<DungeonRoom, List<Vector2I>> _roomCells = new();
    private Dictionary<Vector2I, DungeonRoom> _doorToRoom = new();
    private Dictionary<Vector2I, DungeonRoom> _corridorToRoom = new();

    public override void _Ready()
    {
        Instance = this;
        if (DungeonMapPath != null && !DungeonMapPath.IsEmpty)
            DungeonMap = GetNode<TileMapLayer>(DungeonMapPath);
        else
            DungeonMap = GetNodeOrNull<TileMapLayer>("DungeonMap");
    }

    public void RenderDungeon(IReadOnlyList<DungeonRoom> rooms)
    {
        int maxX = 0, maxY = 0;
        foreach (var r in rooms)
        {
            maxX = Mathf.Max(maxX, r.GridOffset.X + r.Template.Width);
            maxY = Mathf.Max(maxY, r.GridOffset.Y + r.Template.Height);
        }
        MapWidth = maxX + 2;
        MapHeight = maxY + 4;

        GridManager.Instance.Initialize(MapWidth, MapHeight);

        CreateTileSet();

        foreach (var r in rooms)
            PaintRoom(r);

        var processed = new HashSet<string>();
        foreach (var r in rooms)
        {
            foreach (var kv in r.Connections)
            {
                var other = kv.Value;
                if (other == null) continue;
                var key = GetPairKey(r, other);
                if (processed.Contains(key)) continue;
                processed.Add(key);
                PaintCorridor(r, other);
            }
        }
    }

    private string GetPairKey(DungeonRoom a, DungeonRoom b)
    {
        int ha = a.GetHashCode();
        int hb = b.GetHashCode();
        return ha < hb ? $"{ha}-{hb}" : $"{hb}-{ha}";
    }

    private void CreateTileSet()
    {
        Color[] colors = {
            Color.FromHtml("#2A2A2A"),
            Color.FromHtml("#4A4A4A"),
            Color.FromHtml("#3A4A5A"),
            Color.FromHtml("#6B4226"),
            Color.FromHtml("#6B2626"),
            Color.FromHtml("#4A2666"),
        };

        int n = colors.Length;
        var image = Image.CreateEmpty(TILE_SIZE * n, TILE_SIZE, false, Image.Format.Rgba8);
        for (int i = 0; i < n; i++)
            for (int x = 0; x < TILE_SIZE; x++)
                for (int y = 0; y < TILE_SIZE; y++)
                    image.SetPixel(i * TILE_SIZE + x, y, colors[i]);
        var tex = ImageTexture.CreateFromImage(image);

        var atlas = new TileSetAtlasSource
        {
            Texture = tex,
            TextureRegionSize = new Vector2I(TILE_SIZE, TILE_SIZE)
        };
        for (int i = 0; i < n; i++)
            atlas.CreateTile(new Vector2I(i, 0));

        var tileset = new TileSet { TileSize = new Vector2I(TILE_SIZE, TILE_SIZE) };
        tileset.AddSource(atlas, 0);
        DungeonMap.TileSet = tileset;
    }

    private Vector2I AtlasCoordsFor(TileType t) => t switch
    {
        TileType.Wall => new Vector2I(0, 0),
        TileType.Floor => new Vector2I(1, 0),
        TileType.Corridor => new Vector2I(2, 0),
        TileType.Door => new Vector2I(3, 0),
        TileType.DoorLocked => new Vector2I(4, 0),
        TileType.DoorSealed => new Vector2I(5, 0),
        _ => new Vector2I(0, 0)
    };

    public void SetTile(Vector2I pos, TileType type)
    {
        GridManager.Instance.SetTile(pos.X, pos.Y, type);
        DungeonMap.SetCell(pos, 0, AtlasCoordsFor(type));
    }

    private void PaintRoom(DungeonRoom room)
    {
        var cells = new List<Vector2I>();
        for (int x = 0; x < room.Template.Width; x++)
        {
            for (int y = 0; y < room.Template.Height; y++)
            {
                if (!room.Template.Layout[x, y]) continue;
                var p = new Vector2I(room.GridOffset.X + x, room.GridOffset.Y + y);
                SetTile(p, TileType.Floor);
                cells.Add(p);
                _cellToRoom[p] = room;
            }
        }
        _roomCells[room] = cells;
    }

    private void PaintCorridor(DungeonRoom a, DungeonRoom b)
    {
        DungeonRoom left, right;
        if (a.GridOffset.X < b.GridOffset.X)
        {
            left = a; right = b;
        }
        else
        {
            left = b; right = a;
        }

        int leftCenterY = left.GridOffset.Y + left.Template.Height / 2;
        int rightCenterY = right.GridOffset.Y + right.Template.Height / 2;
        int yCenter = (leftCenterY + rightCenterY) / 2;

        int xStart = left.GridOffset.X + left.Template.Width;
        int xEnd = right.GridOffset.X - 1;

        for (int x = xStart; x <= xEnd; x++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                var p = new Vector2I(x, yCenter + dy);
                TileType type;
                if ((x == xStart || x == xEnd) && dy == 0)
                    type = TileType.Door;
                else
                    type = TileType.Corridor;
                SetTile(p, type);

                if (type == TileType.Door)
                {
                    _doorToRoom[p] = (x == xStart) ? left : right;
                }
                _corridorToRoom[p] = (x == xStart) ? left : right;
            }
        }
    }

    public Vector2 GridToWorld(Vector2I g)
    {
        return new Vector2(g.X * TILE_SIZE + TILE_SIZE / 2f, g.Y * TILE_SIZE + TILE_SIZE / 2f);
    }

    public Vector2I WorldToGrid(Vector2 w)
    {
        return new Vector2I(Mathf.FloorToInt(w.X / TILE_SIZE), Mathf.FloorToInt(w.Y / TILE_SIZE));
    }

    public DungeonRoom GetRoomAt(Vector2I pos)
    {
        return _cellToRoom.GetValueOrDefault(pos);
    }

    public DungeonRoom GetRoomAdjacentToDoor(Vector2I doorPos)
    {
        // Buscar una sala adyacente (por las celdas floor vecinas)
        foreach (var n in GridManager.Instance.GetNeighbors(doorPos))
        {
            var r = GetRoomAt(n);
            if (r != null) return r;
        }
        return null;
    }

    public List<Vector2I> GetRoomCells(DungeonRoom room)
    {
        return _roomCells.GetValueOrDefault(room) ?? new List<Vector2I>();
    }

    public Vector2 GetMapCenter()
    {
        return new Vector2(MapWidth * TILE_SIZE / 2f, MapHeight * TILE_SIZE / 2f);
    }

    public Vector2 GetMapSizeWorld()
    {
        return new Vector2(MapWidth * TILE_SIZE, MapHeight * TILE_SIZE);
    }
}
