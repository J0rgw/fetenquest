using Godot;

public partial class ChaosSystem : Node
{
    public static ChaosSystem Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void CheckThresholds(int chaosCounter)
    {
        switch (chaosCounter)
        {
            case 10:
                GD.Print("⚠ CC=10: ¡El brujo envía un monstruo errante!");
                SpawnWanderingMonster();
                break;
            case 20:
                GD.Print("⚠ CC=20: Todos los monstruos ganan +1 dado de ataque.");
                BuffAllMonsters();
                break;
            case 30:
                GD.Print("⚠ CC=30: Se activan todas las trampas no descubiertas.");
                break;
            case 40:
                GD.Print("⚠ CC=40: ¡El jefe se activa y avanza hacia los héroes!");
                break;
        }

        if (chaosCounter >= 50 && chaosCounter % 1 == 0)
            GD.Print("⚠ CC≥50: Monstruo errante cada turno. La run es casi injugable.");
    }

    private void SpawnWanderingMonster()
    {
        // Por ahora solo log — la lógica real va cuando tengamos el grid
        GD.Print("Monstruo errante spawneado en sala revelada aleatoria.");
    }

    private void BuffAllMonsters()
    {
        GD.Print("Buff aplicado a todos los monstruos vivos.");
    }
}