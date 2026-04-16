using Godot;
using System;

public partial class ChaosSystem : Node
{
    public static ChaosSystem Instance { get; private set; }

    [Signal] public delegate void ThresholdReachedEventHandler(int threshold, string message);
    [Signal] public delegate void WanderingMonsterRequestedEventHandler();
    [Signal] public delegate void AllMonstersBuffedEventHandler();

    public override void _Ready()
    {
        Instance = this;
    }

    public void CheckThresholds(int chaosCounter)
    {
        switch (chaosCounter)
        {
            case 10:
                GD.Print("CC=10: El brujo envia un monstruo errante.");
                EmitSignal(SignalName.ThresholdReached, 10, "CC=10: El brujo envia un monstruo errante");
                SpawnWanderingMonster();
                break;
            case 20:
                GD.Print("CC=20: Todos los monstruos ganan +1 dado de ataque.");
                EmitSignal(SignalName.ThresholdReached, 20, "CC=20: Monstruos +1 dado de ataque");
                BuffAllMonsters();
                break;
            case 30:
                GD.Print("CC=30: Se activan todas las trampas no descubiertas.");
                EmitSignal(SignalName.ThresholdReached, 30, "CC=30: Trampas activas");
                break;
            case 40:
                GD.Print("CC=40: El jefe se activa y avanza hacia los heroes.");
                EmitSignal(SignalName.ThresholdReached, 40, "CC=40: El jefe se activa");
                ActivateBoss();
                break;
        }

        if (chaosCounter >= 50)
        {
            GD.Print("CC>=50: Monstruo errante cada turno.");
            SpawnWanderingMonster();
        }
    }

    public void SpawnWanderingMonster()
    {
        GD.Print("Monstruo errante spawneado.");
        EmitSignal(SignalName.WanderingMonsterRequested);
    }

    private void BuffAllMonsters()
    {
        if (TurnManager.Instance == null) return;
        foreach (var monster in TurnManager.Instance.Monsters)
            if (monster.IsAlive)
                monster.BuffAttack(1);
        EmitSignal(SignalName.AllMonstersBuffed);
    }

    private void ActivateBoss()
    {
        if (TurnManager.Instance == null) return;
        foreach (var monster in TurnManager.Instance.Monsters)
            if (monster.IsAlive && monster is MonsterInstance m && m.IsBoss)
                m.MakeAggressive();
    }
}
