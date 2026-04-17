using Godot;
using System.Collections.Generic;

public partial class EntityManager : Node
{
    public static EntityManager Instance { get; private set; }

    [Export] public NodePath EntitiesContainerPath;
    private Node2D EntitiesContainer;

    private Dictionary<Entity, EntityVisual> _visuals = new();

    public override void _Ready()
    {
        Instance = this;
        if (EntitiesContainerPath != null && !EntitiesContainerPath.IsEmpty)
            EntitiesContainer = GetNode<Node2D>(EntitiesContainerPath);
    }

    public EntityVisual SpawnMercenary(MercenaryInstance m, Vector2I gridPos)
    {
        Color c = m.Class switch
        {
            MercenaryClass.Barbarian => Color.FromHtml("#CC3333"),
            MercenaryClass.Mage => Color.FromHtml("#3333CC"),
            MercenaryClass.Elf => Color.FromHtml("#33CC33"),
            MercenaryClass.Dwarf => Color.FromHtml("#CC8833"),
            _ => Colors.White
        };
        var v = EntityVisual.Create(m, c);
        EntitiesContainer.AddChild(m);
        EntitiesContainer.AddChild(v);
        GridManager.Instance.PlaceEntity(m, gridPos);
        v.UpdatePosition(gridPos);
        _visuals[m] = v;
        return v;
    }

    public EntityVisual SpawnMonster(MonsterInstance m, Vector2I gridPos)
    {
        Color c = m.IsBoss ? Color.FromHtml("#550055") : Color.FromHtml("#883388");
        bool hasBorder = m.IsBoss;
        Color borderColor = Color.FromHtml("#FFD700");

        var v = EntityVisual.Create(m, c, hasBorder, borderColor);
        EntitiesContainer.AddChild(m);
        EntitiesContainer.AddChild(v);
        m.SetHomePosition(gridPos);
        GridManager.Instance.PlaceEntity(m, gridPos);
        v.UpdatePosition(gridPos);
        _visuals[m] = v;

        // Oculto inicialmente si esta en niebla
        if (!FogOfWarSystem.Instance.IsVisible(gridPos))
            v.SetVisibility(false);

        return v;
    }

    public EntityVisual GetVisual(Entity e)
    {
        return _visuals.GetValueOrDefault(e);
    }

    public void OnEntityDied(Entity e)
    {
        if (_visuals.TryGetValue(e, out var v))
        {
            v.PlayDeathAnimation();
            _visuals.Remove(e);
            GridManager.Instance.RemoveEntity(e);
        }
    }

    public void OnCellRevealed(Vector2I pos)
    {
        // Si hay una entidad en esa celda, hacerla visible
        var ent = GridManager.Instance.GetEntityAt(pos);
        if (ent == null) return;
        if (_visuals.TryGetValue(ent, out var v))
            v.SetVisibility(true);
    }

    public void RefreshVisibility()
    {
        foreach (var kv in _visuals)
        {
            bool visible = FogOfWarSystem.Instance.IsVisible(kv.Key.GridPosition);
            kv.Value.SetVisibility(visible);
        }
    }
}
