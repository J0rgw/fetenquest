using Godot;

public partial class EntityVisual : Node2D
{
    public Entity Target { get; private set; }

    private ColorRect _body;
    private ColorRect _border;
    private ColorRect _healthBarBg;
    private ColorRect _healthBarFg;
    private Label _nameLabel;
    private Color _baseColor;
    private bool _hasBorder;
    private Color _borderColor;

    public static EntityVisual Create(Entity entity, Color color, bool hasBorder = false, Color borderColor = default)
    {
        var v = new EntityVisual
        {
            Target = entity,
            _baseColor = color,
            _hasBorder = hasBorder,
            _borderColor = borderColor == default ? Colors.Yellow : borderColor
        };
        return v;
    }

    public override void _Ready()
    {
        if (_hasBorder)
        {
            _border = new ColorRect
            {
                Color = _borderColor,
                Size = new Vector2(30, 30),
                Position = new Vector2(-15, -15),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            AddChild(_border);
        }

        _body = new ColorRect
        {
            Color = _baseColor,
            Size = new Vector2(28, 28),
            Position = new Vector2(-14, -14),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_body);

        _nameLabel = new Label
        {
            Text = Target.EntityName,
            Position = new Vector2(-30, -28),
            Size = new Vector2(60, 10),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _nameLabel.AddThemeFontSizeOverride("font_size", 8);
        _nameLabel.AddThemeColorOverride("font_color", Colors.White);
        _nameLabel.HorizontalAlignment = HorizontalAlignment.Center;
        AddChild(_nameLabel);

        _healthBarBg = new ColorRect
        {
            Color = new Color(0.15f, 0.05f, 0.05f),
            Size = new Vector2(32, 4),
            Position = new Vector2(-16, 16),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_healthBarBg);

        _healthBarFg = new ColorRect
        {
            Color = new Color(0.85f, 0.15f, 0.15f),
            Size = new Vector2(32, 4),
            Position = new Vector2(-16, 16),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        AddChild(_healthBarFg);

        UpdateHealthBar(Target.BodyPoints, Target.MaxBodyPoints);
    }

    public void UpdatePosition(Vector2I gridPos)
    {
        Position = DungeonRenderer.Instance.GridToWorld(gridPos);
    }

    public void UpdateHealthBar(int current, int max)
    {
        if (_healthBarFg == null) return;
        float ratio = max > 0 ? Mathf.Clamp((float)current / max, 0f, 1f) : 0f;
        _healthBarFg.Size = new Vector2(32f * ratio, 4f);
    }

    public void PlayDamageFlash()
    {
        if (_body == null) return;
        var tween = CreateTween();
        tween.TweenProperty(_body, "color", Colors.White, 0.1);
        tween.TweenProperty(_body, "color", _baseColor, 0.1);
    }

    public void PlayDeathAnimation()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), 0.3);
        tween.TweenCallback(Callable.From(() => QueueFree()));
    }

    public void SetVisibility(bool visible)
    {
        Visible = visible;
    }
}
