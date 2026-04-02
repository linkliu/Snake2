using Godot;
using System.Collections.Generic;

public sealed class LevelData
{
    public string Name { get; }
    public Vector2I SnakeSpawnHead { get; }
    public Vector2I InitialDirection { get; }
    public Vector2I BarrelCell { get; }
    public int RequiredFruits { get; }
    public IReadOnlyList<Vector2I> Obstacles { get; }

    public LevelData(
        string name,
        Vector2I snakeSpawnHead,
        Vector2I initialDirection,
        Vector2I barrelCell,
        int requiredFruits,
        IReadOnlyList<Vector2I> obstacles)
    {
        Name = name;
        SnakeSpawnHead = snakeSpawnHead;
        InitialDirection = initialDirection;
        BarrelCell = barrelCell;
        RequiredFruits = requiredFruits;
        Obstacles = obstacles;
    }
}
