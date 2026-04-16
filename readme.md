# Fetenquest

Dungeon crawler por turnos, grid-based, con permadeath por mercenario. Una reimaginación roguelike de HeroQuest con profundidad moderna, mi intencion es un mix del core de fire emblem con el loop de darkest dungeon

## Concepto

Fetenquest toma la esencia del HeroQuest clásico de tablero y la lleva a un loop roguelike completo. La tensión viene de tres fuentes:

- **Contador de Caos** — cada turno que pasa el brujo refuerza la mazmorra. No puedes tomarte tu tiempo.
- **Permadeath de mercenarios** — perder un personaje es perder su equipo para siempre.
- **El escape** — matar al jefe no termina la run. Tienes que salir vivo de la mazmorra para conservar el botín.

## Stack

- **Motor:** Godot 4.6.2 Mono
- **Lenguaje:** C# (.NET 8)
- **Plataforma objetivo:** Steam/PC

## Clases

| Clase | Cuerpo | Mente | Ataque | Defensa | Rol |
|-------|--------|-------|--------|---------|-----|
| Orco Barbaro | 8 | 2 | 3 dados | 2 dados | DPS melee, primera linea |
| Humano Mago | 3 | 5 | 1 dado | 1 dado | Control, dano a distancia |
| Elfo Explorador | 5 | 4 | 2 dados | 2 dados | Explorador, ranged, trampas |
| Enano Tanque | 6 | 3 | 2 dados | 3 dados | Tanque puro, proteger pasillos |

## Loop principal

```
Ciudad Hub
  └── Reclutar mercenarios / equipar / comprar mochilas
        └── Mazmorra (generacion procedural)
              └── Explorar / combatir / buscar tesoros
                    └── Derrotar al jefe
                          └── Fase de escape
                                ├── Escape exitoso → conservas todo
                                └── Derrota → perdida escalonada de oro
```

## Sistemas implementados (Fase 1)

- `GameState` — singleton global, oro permanente, oro de run, contador de caos
- `DiceSystem` — dados de combate custom (calavera/escudo blanco/escudo negro), 2d6 de movimiento, probabilidades por combinatoria exacta
- `Entity` — clase base de mercenarios y monstruos
- `MercenaryInstance` — stats por clase, pool de movimiento dividido, permadeath
- `MonsterInstance` — comportamiento Territorial/Agresivo, conversion automatica
- `TurnManager` — cola de turnos mercenarios → monstruos
- `ChaosSystem` — contador con 5 umbrales de escalada
- `GridManager` — grid 2D, pathfinding A*, linea de vision Bresenham, zona de control de monstruos
- `FogOfWarSystem` — revelacion celda a celda en pasillos, revelacion total de habitacion al abrir puerta
- `CombatSystem` — resolucion de ataque/defensa, preview de probabilidades antes de confirmar

## Estructura del proyecto

```
res://
├── src/
│   ├── core/        # Sistemas principales
│   ├── entities/    # Entity, MercenaryInstance, MonsterInstance
│   ├── grid/        # GridManager, MovementPool
│   ├── dungeon/     # DungeonGenerator, RoomTemplate
│   └── data/        # Resources: clases, items
├── scenes/          # Escenas .tscn
├── assets/          # Sprites placeholder
└── resources/       # Archivos .tres
```

## Biomas (pendiente)

| Bioma | Dificultad | Enemigos |
|-------|-----------|----------|
| Las Alcantarillas | Introductorio | Goblins, ratas, trampas simples |
| El Castillo Abandonado | Medio | Muertos vivientes, trampas mecanicas |
| Las Cuevas del Brujo | Dificil | Cultistas, criaturas magicas, jefe |

## Estado actual

Fase 1 completa: arquitectura base y sistemas core funcionando con escena de prueba en consola. Proximos pasos: `EscapeSystem`, `TreasureSystem` y `DungeonGenerator`.