using Godot;

public partial class CameraController : Camera2D
{
    public static CameraController Instance { get; private set; }

    public Node2D Target;

    private bool _dragging = false;
    private Vector2 _mapMin = Vector2.Zero;
    private Vector2 _mapMax = Vector2.Zero;
    private bool _hasBounds = false;

    public override void _Ready()
    {
        Instance = this;
        Zoom = new Vector2(2.0f, 2.0f);
    }

    public void SetMapBounds(Vector2 min, Vector2 max)
    {
        _mapMin = min;
        _mapMax = max;
        _hasBounds = true;

        LimitLeft = (int)min.X;
        LimitTop = (int)min.Y;
        LimitRight = (int)max.X;
        LimitBottom = (int)max.Y;
    }

    public void SetTarget(Node2D target)
    {
        Target = target;
    }

    public override void _Process(double delta)
    {
        if (Target != null && !_dragging)
        {
            Position = Position.Lerp(Target.Position, 0.08f);
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                var newZoom = Zoom * 1.1f;
                newZoom = new Vector2(
                    Mathf.Clamp(newZoom.X, 1f, 4f),
                    Mathf.Clamp(newZoom.Y, 1f, 4f));
                Zoom = newZoom;
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                var newZoom = Zoom / 1.1f;
                newZoom = new Vector2(
                    Mathf.Clamp(newZoom.X, 1f, 4f),
                    Mathf.Clamp(newZoom.Y, 1f, 4f));
                Zoom = newZoom;
            }
            else if (mb.ButtonIndex == MouseButton.Middle)
            {
                _dragging = mb.Pressed;
            }
        }
        else if (@event is InputEventMouseMotion mm && _dragging)
        {
            Position -= mm.Relative / Zoom;
        }
    }
}
