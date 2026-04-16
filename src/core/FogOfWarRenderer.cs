using Godot;

public partial class FogOfWarRenderer : Node2D
{
    public static FogOfWarRenderer Instance { get; private set; }

    [Export] public TileMapLayer FogLayer;

    public override void _Ready()
    {
        Instance = this;
    }

    public void InitializeFog(int width, int height)
    {
        var image = Image.CreateEmpty(32, 32, false, Image.Format.Rgba8);
        image.Fill(Colors.Black);
        var tex = ImageTexture.CreateFromImage(image);

        var atlas = new TileSetAtlasSource
        {
            Texture = tex,
            TextureRegionSize = new Vector2I(32, 32)
        };
        atlas.CreateTile(Vector2I.Zero);

        var ts = new TileSet { TileSize = new Vector2I(32, 32) };
        ts.AddSource(atlas, 0);
        FogLayer.TileSet = ts;

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                FogLayer.SetCell(new Vector2I(x, y), 0, Vector2I.Zero);
    }

    public void RevealCell(Vector2I pos)
    {
        FogLayer.EraseCell(pos);
    }

    public void OnCellRevealedSignal(Vector2I pos)
    {
        RevealCell(pos);
    }
}
