using Godot;
using System.Collections.Generic;

public enum TreasureCardType
{
    Gold, Equipment, BodyPotion, MindPotion,
    Trap, WanderingMonster, NarrativeEvent, Nothing
}

public struct TreasureCard
{
    public TreasureCardType Type;
    public string Description;
    public int GoldAmount; // Solo para cartas de oro
}

public partial class TreasureSystem : Node
{
    public static TreasureSystem Instance { get; private set; }

    private List<TreasureCard> _deck = new();
    private Dictionary<Vector2I, int> _roomSearchCount = new();

    public override void _Ready()
    {
        Instance = this;
    }

    // Construye y baraja el mazo del bioma (20 cartas segun el GDD)
    public void InitializeDeck()
    {
        _deck.Clear();
        _roomSearchCount.Clear();

        // 4x Oro
        _deck.Add(new TreasureCard { Type = TreasureCardType.Gold, Description = "5 monedas de oro", GoldAmount = 5 });
        _deck.Add(new TreasureCard { Type = TreasureCardType.Gold, Description = "10 monedas de oro", GoldAmount = 10 });
        _deck.Add(new TreasureCard { Type = TreasureCardType.Gold, Description = "15 monedas de oro", GoldAmount = 15 });
        _deck.Add(new TreasureCard { Type = TreasureCardType.Gold, Description = "25 monedas de oro", GoldAmount = 25 });

        // 3x Equipamiento
        for (int i = 0; i < 3; i++)
            _deck.Add(new TreasureCard { Type = TreasureCardType.Equipment, Description = "Equipamiento del bioma" });

        // 2x Pocion de Cuerpo
        for (int i = 0; i < 2; i++)
            _deck.Add(new TreasureCard { Type = TreasureCardType.BodyPotion, Description = "Pocion de Cuerpo: restaura puntos de cuerpo" });

        // 2x Pocion de Mente
        for (int i = 0; i < 2; i++)
            _deck.Add(new TreasureCard { Type = TreasureCardType.MindPotion, Description = "Pocion de Mente: restaura puntos de mente" });

        // 3x Trampa
        for (int i = 0; i < 3; i++)
            _deck.Add(new TreasureCard { Type = TreasureCardType.Trap, Description = "Trampa: dano inmediato sin tirada de defensa" });

        // 2x Monstruo Errante
        for (int i = 0; i < 2; i++)
            _deck.Add(new TreasureCard { Type = TreasureCardType.WanderingMonster, Description = "Monstruo Errante: aparece en la habitacion" });

        // 2x Evento Narrativo
        for (int i = 0; i < 2; i++)
            _deck.Add(new TreasureCard { Type = TreasureCardType.NarrativeEvent, Description = "Evento narrativo del bioma" });

        // 2x Nada
        for (int i = 0; i < 2; i++)
            _deck.Add(new TreasureCard { Type = TreasureCardType.Nothing, Description = "No encuentras nada." });

        Shuffle();
        GD.Print($"Mazo de tesoro inicializado: {_deck.Count} cartas.");
    }

    private void Shuffle()
    {
        var rng = new RandomNumberGenerator();
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }

    public bool HasCardsLeft => _deck.Count > 0;

    // Roba la carta superior y aplica su efecto
    public TreasureCard DrawCard(Vector2I roomPos)
    {
        if (!HasCardsLeft)
        {
            GD.Print("El mazo de tesoro esta agotado.");
            return new TreasureCard { Type = TreasureCardType.Nothing, Description = "El mazo esta agotado." };
        }

        var card = _deck[0];
        _deck.RemoveAt(0);

        _roomSearchCount.TryGetValue(roomPos, out int count);
        _roomSearchCount[roomPos] = count + 1;

        GD.Print($"Carta robada: [{card.Type}] {card.Description}");
        GD.Print($"Cartas restantes en el mazo: {_deck.Count}");

        ApplyCard(card);
        return card;
    }

    private void ApplyCard(TreasureCard card)
    {
        switch (card.Type)
        {
            case TreasureCardType.Gold:
                GameState.Instance.AddRunGold(card.GoldAmount);
                break;
            case TreasureCardType.WanderingMonster:
                ChaosSystem.Instance.SpawnWanderingMonster();
                break;
            case TreasureCardType.Trap:
                GD.Print("Una trampa se activa!");
                break;
        }
    }

    public int GetSearchCount(Vector2I roomPos)
    {
        _roomSearchCount.TryGetValue(roomPos, out int count);
        return count;
    }
}