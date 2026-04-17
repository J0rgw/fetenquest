using Godot;
using System;
using System.Collections.Generic;

public partial class GameUI : Control
{
    public static GameUI Instance { get; private set; }

    private Label _turnLabel;
    private Label _moveLabel;
    private Label _chaosLabel;
    private Button _attackBtn;
    private Button _searchBtn;
    private Button _endTurnBtn;

    private VBoxContainer _logContainer;
    private ScrollContainer _logScroll;

    private VBoxContainer _mercPanel;
    private Dictionary<MercenaryInstance, Control> _mercCards = new();

    private Panel _combatPanel;
    private Label _combatTitle;
    private Label _combatTargetInfo;
    private Label _combatExpected;
    private VBoxContainer _combatProbs;
    private Button _combatAttackBtn;
    private Button _combatCancelBtn;

    private Panel _chaosNotifPanel;
    private Label _chaosNotifLabel;

    private Panel _searchNotifPanel;
    private Label _searchNotifTitle;
    private Label _searchNotifBody;

    private Action<MercenaryInstance, MonsterInstance> _onAttackConfirm;
    private MercenaryInstance _combatAttacker;
    private MonsterInstance _combatDefender;

    private readonly List<string> _logLines = new();

    public override void _Ready()
    {
        Instance = this;
        AnchorRight = 1;
        AnchorBottom = 1;
        OffsetRight = 0;
        OffsetBottom = 0;
        MouseFilter = MouseFilterEnum.Pass;

        BuildTopBar();
        BuildLogPanel();
        BuildMercenaryPanel();
        BuildCombatPanel();
        BuildChaosNotif();
        BuildSearchNotif();
    }

    public void Initialize()
    {
        foreach (var m in TurnManager.Instance.Mercenaries)
            CreateMercenaryCard(m);
        UpdateChaosLabel(GameState.Instance.ChaosCounter);
    }

    private void BuildTopBar()
    {
        var bg = new ColorRect
        {
            Color = new Color(0.08f, 0.08f, 0.1f, 0.9f),
            Position = new Vector2(0, 0),
            Size = new Vector2(1920, 40),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(bg);

        var hbox = new HBoxContainer
        {
            Position = new Vector2(10, 8),
            Size = new Vector2(1900, 30)
        };
        hbox.AddThemeConstantOverride("separation", 20);
        AddChild(hbox);

        _turnLabel = MakeLabel("Turno: -");
        hbox.AddChild(_turnLabel);

        _moveLabel = MakeLabel("Movimiento: -/-");
        hbox.AddChild(_moveLabel);

        _chaosLabel = MakeLabel("Caos: 0");
        hbox.AddChild(_chaosLabel);

        var sep = new Control { CustomMinimumSize = new Vector2(40, 0) };
        hbox.AddChild(sep);

        _attackBtn = new Button { Text = "Atacar", Disabled = true };
        hbox.AddChild(_attackBtn);

        _searchBtn = new Button { Text = "Buscar", Disabled = true };
        _searchBtn.Pressed += OnSearchPressed;
        hbox.AddChild(_searchBtn);

        _endTurnBtn = new Button { Text = "Terminar turno" };
        _endTurnBtn.Pressed += OnEndTurnPressed;
        hbox.AddChild(_endTurnBtn);
    }

    private Label MakeLabel(string text)
    {
        var l = new Label { Text = text };
        l.AddThemeColorOverride("font_color", Colors.White);
        l.AddThemeFontSizeOverride("font_size", 14);
        return l;
    }

    private void BuildLogPanel()
    {
        var bg = new ColorRect
        {
            Color = new Color(0.08f, 0.08f, 0.1f, 0.9f),
            Position = new Vector2(1500, 500),
            Size = new Vector2(400, 200),
            MouseFilter = MouseFilterEnum.Stop
        };
        AddChild(bg);

        _logScroll = new ScrollContainer
        {
            Position = new Vector2(1510, 510),
            Size = new Vector2(380, 180)
        };
        AddChild(_logScroll);

        _logContainer = new VBoxContainer();
        _logContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _logScroll.AddChild(_logContainer);
    }

    private void BuildMercenaryPanel()
    {
        _mercPanel = new VBoxContainer
        {
            Position = new Vector2(10, 60),
            Size = new Vector2(220, 500)
        };
        _mercPanel.AddThemeConstantOverride("separation", 8);
        AddChild(_mercPanel);
    }

    private void CreateMercenaryCard(MercenaryInstance merc)
    {
        var card = new Panel();
        card.CustomMinimumSize = new Vector2(220, 90);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        style.BorderWidthLeft = style.BorderWidthRight = style.BorderWidthTop = style.BorderWidthBottom = 2;
        style.BorderColor = new Color(0.3f, 0.3f, 0.3f);
        card.AddThemeStyleboxOverride("panel", style);

        var nameLabel = new Label
        {
            Text = $"{merc.EntityName}",
            Position = new Vector2(8, 6)
        };
        nameLabel.AddThemeColorOverride("font_color", Colors.White);
        nameLabel.AddThemeFontSizeOverride("font_size", 14);
        card.AddChild(nameLabel);

        var classLabel = new Label
        {
            Text = $"Clase: {merc.Class}",
            Position = new Vector2(8, 24)
        };
        classLabel.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        classLabel.AddThemeFontSizeOverride("font_size", 10);
        card.AddChild(classLabel);

        // Body bar
        var bodyBg = new ColorRect
        {
            Color = new Color(0.2f, 0.05f, 0.05f),
            Position = new Vector2(8, 50),
            Size = new Vector2(200, 10)
        };
        card.AddChild(bodyBg);

        var bodyFg = new ColorRect
        {
            Color = Color.FromHtml("#CC3333"),
            Position = new Vector2(8, 50),
            Size = new Vector2(200, 10)
        };
        card.AddChild(bodyFg);
        bodyFg.Name = "BodyFg";

        // Mind bar
        var mindBg = new ColorRect
        {
            Color = new Color(0.05f, 0.05f, 0.2f),
            Position = new Vector2(8, 66),
            Size = new Vector2(200, 8)
        };
        card.AddChild(mindBg);

        var mindFg = new ColorRect
        {
            Color = Color.FromHtml("#3333CC"),
            Position = new Vector2(8, 66),
            Size = new Vector2(200, 8)
        };
        card.AddChild(mindFg);
        mindFg.Name = "MindFg";

        _mercPanel.AddChild(card);
        _mercCards[merc] = card;

        RefreshMercenaryCard(merc);
    }

    public void RefreshMercenaryCard(MercenaryInstance merc)
    {
        if (!_mercCards.TryGetValue(merc, out var card)) return;
        var bodyFg = card.GetNode<ColorRect>("BodyFg");
        var mindFg = card.GetNode<ColorRect>("MindFg");

        float bodyRatio = merc.MaxBodyPoints > 0 ? (float)merc.BodyPoints / merc.MaxBodyPoints : 0;
        float mindRatio = merc.MaxMindPoints > 0 ? (float)merc.MindPoints / merc.MaxMindPoints : 0;
        bodyFg.Size = new Vector2(200 * Mathf.Clamp(bodyRatio, 0, 1), 10);
        mindFg.Size = new Vector2(200 * Mathf.Clamp(mindRatio, 0, 1), 8);

        var style = (StyleBoxFlat)card.GetThemeStylebox("panel").Duplicate();
        bool isCurrent = TurnManager.Instance.GetCurrentMercenary() == merc;
        if (merc.IsDead)
            style.BorderColor = new Color(0.3f, 0.3f, 0.3f);
        else if (isCurrent)
            style.BorderColor = Color.FromHtml("#FFFF00");
        else
            style.BorderColor = new Color(0.3f, 0.3f, 0.3f);
        card.AddThemeStyleboxOverride("panel", style);

        card.Modulate = merc.IsDead ? new Color(0.4f, 0.4f, 0.4f) : Colors.White;
    }

    private void BuildCombatPanel()
    {
        _combatPanel = new Panel
        {
            Position = new Vector2(680, 250),
            Size = new Vector2(500, 300),
            Visible = false
        };

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.1f, 0.1f, 0.14f, 0.98f),
            BorderColor = new Color(0.6f, 0.5f, 0.2f),
            BorderWidthLeft = 3,
            BorderWidthRight = 3,
            BorderWidthTop = 3,
            BorderWidthBottom = 3
        };
        _combatPanel.AddThemeStyleboxOverride("panel", style);
        AddChild(_combatPanel);

        _combatTitle = new Label
        {
            Text = "",
            Position = new Vector2(20, 15),
            Size = new Vector2(460, 24)
        };
        _combatTitle.AddThemeFontSizeOverride("font_size", 16);
        _combatTitle.AddThemeColorOverride("font_color", Colors.White);
        _combatPanel.AddChild(_combatTitle);

        _combatTargetInfo = new Label
        {
            Text = "",
            Position = new Vector2(20, 50),
            Size = new Vector2(460, 20)
        };
        _combatTargetInfo.AddThemeFontSizeOverride("font_size", 12);
        _combatTargetInfo.AddThemeColorOverride("font_color", new Color(0.9f, 0.9f, 0.9f));
        _combatPanel.AddChild(_combatTargetInfo);

        _combatExpected = new Label
        {
            Text = "",
            Position = new Vector2(20, 75),
            Size = new Vector2(460, 20)
        };
        _combatExpected.AddThemeFontSizeOverride("font_size", 12);
        _combatExpected.AddThemeColorOverride("font_color", new Color(0.8f, 0.9f, 0.7f));
        _combatPanel.AddChild(_combatExpected);

        _combatProbs = new VBoxContainer
        {
            Position = new Vector2(20, 110),
            Size = new Vector2(460, 120)
        };
        _combatProbs.AddThemeConstantOverride("separation", 4);
        _combatPanel.AddChild(_combatProbs);

        _combatAttackBtn = new Button
        {
            Text = "ATACAR",
            Position = new Vector2(100, 250),
            Size = new Vector2(120, 36)
        };
        _combatAttackBtn.Pressed += OnCombatAttackPressed;
        _combatPanel.AddChild(_combatAttackBtn);

        _combatCancelBtn = new Button
        {
            Text = "CANCELAR",
            Position = new Vector2(280, 250),
            Size = new Vector2(120, 36)
        };
        _combatCancelBtn.Pressed += OnCombatCancelPressed;
        _combatPanel.AddChild(_combatCancelBtn);
    }

    private void BuildChaosNotif()
    {
        _chaosNotifPanel = new Panel
        {
            Position = new Vector2(560, 200),
            Size = new Vector2(800, 120),
            Visible = false
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.5f, 0.05f, 0.05f, 0.9f),
            BorderColor = new Color(1f, 0.2f, 0.2f),
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderWidthTop = 4,
            BorderWidthBottom = 4
        };
        _chaosNotifPanel.AddThemeStyleboxOverride("panel", style);
        AddChild(_chaosNotifPanel);

        _chaosNotifLabel = new Label
        {
            Position = new Vector2(20, 30),
            Size = new Vector2(760, 60),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _chaosNotifLabel.AddThemeFontSizeOverride("font_size", 22);
        _chaosNotifLabel.AddThemeColorOverride("font_color", Colors.White);
        _chaosNotifPanel.AddChild(_chaosNotifLabel);
    }

    private void BuildSearchNotif()
    {
        _searchNotifPanel = new Panel
        {
            Position = new Vector2(560, 200),
            Size = new Vector2(800, 140),
            Visible = false
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.06f, 0.3f, 0.12f, 0.92f),
            BorderColor = new Color(0.3f, 1f, 0.45f),
            BorderWidthLeft = 4,
            BorderWidthRight = 4,
            BorderWidthTop = 4,
            BorderWidthBottom = 4
        };
        _searchNotifPanel.AddThemeStyleboxOverride("panel", style);
        AddChild(_searchNotifPanel);

        _searchNotifTitle = new Label
        {
            Position = new Vector2(20, 18),
            Size = new Vector2(760, 36),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _searchNotifTitle.AddThemeFontSizeOverride("font_size", 24);
        _searchNotifTitle.AddThemeColorOverride("font_color", new Color(0.8f, 1f, 0.85f));
        _searchNotifPanel.AddChild(_searchNotifTitle);

        _searchNotifBody = new Label
        {
            Position = new Vector2(20, 62),
            Size = new Vector2(760, 64),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        _searchNotifBody.AddThemeFontSizeOverride("font_size", 18);
        _searchNotifBody.AddThemeColorOverride("font_color", Colors.White);
        _searchNotifPanel.AddChild(_searchNotifBody);
    }

    public async void ShowSearchNotif(TreasureCard card)
    {
        string title = card.Type switch
        {
            TreasureCardType.Gold => $"+{card.GoldAmount} ORO",
            TreasureCardType.Equipment => "EQUIPAMIENTO",
            TreasureCardType.BodyPotion => "POCION DE CUERPO",
            TreasureCardType.MindPotion => "POCION DE MENTE",
            TreasureCardType.Trap => "TRAMPA",
            TreasureCardType.WanderingMonster => "MONSTRUO ERRANTE",
            TreasureCardType.NarrativeEvent => "EVENTO",
            TreasureCardType.Nothing => "NADA",
            _ => "TESORO"
        };

        _searchNotifTitle.Text = title;
        _searchNotifBody.Text = card.Description;
        _searchNotifPanel.Visible = true;
        InputHandler.Instance.InputBlocked = true;

        await ToSignal(GetTree().CreateTimer(2.0), "timeout");

        _searchNotifPanel.Visible = false;
        if (TurnManager.Instance.IsMercenaryPhase)
            InputHandler.Instance.InputBlocked = false;
    }

    public void OnTurnStarted(string entityName)
    {
        _turnLabel.Text = $"Turno: {entityName}";

        var current = TurnManager.Instance.GetCurrentMercenary();
        UpdateActionButtons(current);
        UpdateMovementLabel(current);

        foreach (var merc in TurnManager.Instance.Mercenaries)
            RefreshMercenaryCard(merc);
    }

    public void UpdateActionButtons(MercenaryInstance merc)
    {
        bool inPlayerPhase = TurnManager.Instance.IsMercenaryPhase && merc != null && merc.IsAlive;
        _attackBtn.Disabled = !inPlayerPhase || merc.HasAttackedThisTurn;
        _searchBtn.Disabled = !inPlayerPhase || merc.HasSearchedThisTurn;
        _endTurnBtn.Disabled = !inPlayerPhase;
    }

    public void UpdateMovementLabel(MercenaryInstance merc)
    {
        if (merc == null)
        {
            _moveLabel.Text = "Movimiento: -/-";
            return;
        }
        _moveLabel.Text = $"Movimiento: {merc.MovementPool}";
    }

    public void OnMovementUpdated()
    {
        var current = TurnManager.Instance.GetCurrentMercenary();
        UpdateMovementLabel(current);
        UpdateActionButtons(current);
    }

    public void UpdateChaosLabel(int cc)
    {
        _chaosLabel.Text = $"Caos: {cc}";
        _chaosLabel.RemoveThemeColorOverride("font_color");
        if (cc >= 30)
            _chaosLabel.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        else
            _chaosLabel.AddThemeColorOverride("font_color", Colors.White);
    }

    public void OnChaosChanged(int cc)
    {
        UpdateChaosLabel(cc);
    }

    public void AddCombatLog(string text, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", color);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.CustomMinimumSize = new Vector2(360, 0);
        _logContainer.AddChild(label);
        _logLines.Add(text);

        // Limitar a 30 lineas
        while (_logContainer.GetChildCount() > 30)
        {
            var first = _logContainer.GetChild(0);
            _logContainer.RemoveChild(first);
            first.QueueFree();
        }

        // Scroll al final
        CallDeferred(nameof(ScrollLogToBottom));
    }

    private void ScrollLogToBottom()
    {
        _logScroll.ScrollVertical = (int)_logScroll.GetVScrollBar().MaxValue;
    }

    public void ShowCombatPreview(MercenaryInstance attacker, MonsterInstance defender,
        Action<MercenaryInstance, MonsterInstance> onConfirm)
    {
        _combatAttacker = attacker;
        _combatDefender = defender;
        _onAttackConfirm = onConfirm;

        _combatTitle.Text = $"{attacker.EntityName} ataca a {defender.EntityName}";
        _combatTargetInfo.Text = $"{defender.EntityName}: {defender.BodyPoints}/{defender.MaxBodyPoints} Cuerpo";

        var probs = DiceSystem.CalculateCombatProbabilities(attacker.AttackDice, defender.DefenseDice, true);
        float expected = 0f;
        foreach (var kv in probs) expected += kv.Key * kv.Value;
        _combatExpected.Text = $"Dano esperado: {expected:F2}";

        foreach (var child in _combatProbs.GetChildren())
            child.QueueFree();

        var sortedKeys = new List<int>(probs.Keys);
        sortedKeys.Sort();
        foreach (var damage in sortedKeys)
        {
            float pct = probs[damage] * 100f;
            var row = new HBoxContainer { CustomMinimumSize = new Vector2(460, 16) };

            var lbl = new Label
            {
                Text = $"Dano {damage}: {pct:F1}%",
                CustomMinimumSize = new Vector2(160, 16)
            };
            lbl.AddThemeFontSizeOverride("font_size", 12);
            lbl.AddThemeColorOverride("font_color", Colors.White);
            row.AddChild(lbl);

            var barBg = new ColorRect
            {
                Color = new Color(0.2f, 0.2f, 0.2f),
                CustomMinimumSize = new Vector2(250, 12)
            };
            row.AddChild(barBg);

            var barFg = new ColorRect
            {
                Color = new Color(0.8f, 0.4f, 0.2f),
                CustomMinimumSize = new Vector2(250f * probs[damage], 12)
            };
            barBg.AddChild(barFg);

            _combatProbs.AddChild(row);
        }

        _combatPanel.Visible = true;
        InputHandler.Instance.InputBlocked = true;
    }

    private void OnCombatAttackPressed()
    {
        _combatPanel.Visible = false;
        InputHandler.Instance.InputBlocked = false;
        _onAttackConfirm?.Invoke(_combatAttacker, _combatDefender);
        _onAttackConfirm = null;
    }

    private void OnCombatCancelPressed()
    {
        _combatPanel.Visible = false;
        InputHandler.Instance.InputBlocked = false;
        _onAttackConfirm = null;
    }

    public async void ShowChaosNotif(int threshold, string message)
    {
        AddCombatLog(message, new Color(1f, 0.6f, 0.2f));

        _chaosNotifLabel.Text = message;
        _chaosNotifPanel.Visible = true;
        InputHandler.Instance.InputBlocked = true;

        await ToSignal(GetTree().CreateTimer(2.0), "timeout");

        _chaosNotifPanel.Visible = false;
        if (TurnManager.Instance.IsMercenaryPhase)
            InputHandler.Instance.InputBlocked = false;
    }

    private void OnEndTurnPressed()
    {
        if (!TurnManager.Instance.IsMercenaryPhase) return;
        InputHandler.Instance.InputBlocked = true;
        TurnManager.Instance.EndCurrentMercenaryTurn();
    }

    private void OnSearchPressed()
    {
        if (!TurnManager.Instance.IsMercenaryPhase) return;

        var merc = TurnManager.Instance.GetCurrentMercenary();
        if (merc == null || !merc.IsAlive || merc.HasSearchedThisTurn) return;

        var room = DungeonRenderer.Instance.GetRoomAt(merc.GridPosition);
        if (room == null)
        {
            AddCombatLog($"{merc.EntityName} solo puede buscar dentro de una sala.", new Color(0.9f, 0.7f, 0.3f));
            return;
        }

        // Solo salas despejadas (ningun monstruo vivo dentro) permiten buscar.
        foreach (var mon in room.Monsters)
        {
            if (mon.IsAlive)
            {
                AddCombatLog("No se puede buscar con monstruos vivos en la sala.", new Color(0.9f, 0.7f, 0.3f));
                return;
            }
        }

        merc.RegisterSearch();
        var card = TreasureSystem.Instance.DrawCard(merc.GridPosition);

        Color logColor = card.Type switch
        {
            TreasureCardType.Gold => new Color(1f, 0.85f, 0.2f),
            TreasureCardType.Trap => new Color(1f, 0.3f, 0.3f),
            TreasureCardType.WanderingMonster => new Color(1f, 0.6f, 0.2f),
            _ => Colors.White,
        };
        AddCombatLog($"{merc.EntityName} busca en la sala: {card.Description}", logColor);

        ShowSearchNotif(card);
        UpdateActionButtons(merc);
        InputHandler.Instance.RefreshOverlays();
    }
}
