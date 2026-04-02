using Godot;
using System;
using System.Collections.Generic;

public partial class GameController : Node2D
{
    [Export] public PackedScene SnakeHeadScene;
    [Export] public PackedScene SnakeBodyScene;
    [Export] public PackedScene SnakeTailScene;
    [Export] public PackedScene FruitScene;
    [Export] public PackedScene WallScene;

    [Export] public Vector2I GridSize = new(24, 14);
    [Export] public float MoveIntervalSeconds = 0.45f; // 速度降低约3倍
    [Export] public float GravityIntervalSeconds = 0.15f;

    private readonly List<Vector2I> _snakeCells = new();
    private readonly List<Node2D> _snakeViews = new();
    private readonly HashSet<Vector2I> _obstacles = new();

    private readonly Vector2I[] _extraWalls =
    {
        new(7, 10), new(8, 10), new(9, 10),
        new(14, 8), new(14, 9), new(14, 10),
        new(17, 6), new(18, 6),
    };

    private Node2D _snakeRoot;
    private Node2D _wallRoot;
    private Node2D _fruitRoot;

    private Vector2I _direction = Vector2I.Right;
    private Vector2I _nextDirection = Vector2I.Right;
    private Vector2I _fruitCell;

    private float _moveTimer;
    private float _gravityTimer;
    private bool _dead;
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _snakeRoot = new Node2D { Name = "SnakeRoot" };
        _wallRoot = new Node2D { Name = "WallRoot" };
        _fruitRoot = new Node2D { Name = "FruitRoot" };
        AddChild(_wallRoot);
        AddChild(_fruitRoot);
        AddChild(_snakeRoot);

        BuildBoundaryWalls();
        BuildExtraWalls();
        InitializeSnake();
        SpawnFruit();
        RefreshSnakeViews();
    }

    public override void _Process(double delta)
    {
        if (_dead)
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                GetTree().ReloadCurrentScene();
            }
            return;
        }

        HandleInput();

        _moveTimer += (float)delta;
        _gravityTimer += (float)delta;

        while (_gravityTimer >= GravityIntervalSeconds)
        {
            _gravityTimer -= GravityIntervalSeconds;
            ApplyGravityStep();
            if (_dead)
            {
                return;
            }
        }

        while (_moveTimer >= MoveIntervalSeconds)
        {
            _moveTimer -= MoveIntervalSeconds;
            StepMove();
            if (_dead)
            {
                return;
            }
        }
    }

    private void HandleInput()
    {
        if (Input.IsActionJustPressed("ui_left")) TrySetDirection(Vector2I.Left);
        else if (Input.IsActionJustPressed("ui_right")) TrySetDirection(Vector2I.Right);
        else if (Input.IsActionJustPressed("ui_up")) TrySetDirection(Vector2I.Up);
        else if (Input.IsActionJustPressed("ui_down")) TrySetDirection(Vector2I.Down);
    }

    private void TrySetDirection(Vector2I candidate)
    {
        if (candidate == -_direction) return;
        _nextDirection = candidate;
    }

    private void StepMove()
    {
        _direction = _nextDirection;
        var newHead = _snakeCells[0] + _direction;

        if (HitsBoundary(newHead) || _obstacles.Contains(newHead))
        {
            Die("蛇头撞到围墙/障碍物");
            return;
        }

        bool grow = newHead == _fruitCell;
        var tail = _snakeCells[^1];

        if (_snakeCells.Contains(newHead) && (grow || newHead != tail))
        {
            Die("蛇头撞到自己");
            return;
        }

        _snakeCells.Insert(0, newHead);
        if (!grow)
        {
            _snakeCells.RemoveAt(_snakeCells.Count - 1);
        }
        else
        {
            SpawnFruit();
        }

        RefreshSnakeViews();
    }

    private void ApplyGravityStep()
    {
        if (IsSupported())
        {
            return;
        }

        var moved = new List<Vector2I>(_snakeCells.Count);
        foreach (var cell in _snakeCells)
        {
            var next = cell + Vector2I.Down;
            if (next.Y >= GridSize.Y - 1)
            {
                Die("蛇掉落碰到底部围墙");
                return;
            }

            if (_obstacles.Contains(next))
            {
                Die("蛇掉落撞到障碍物");
                return;
            }

            moved.Add(next);
        }

        _snakeCells.Clear();
        _snakeCells.AddRange(moved);
        RefreshSnakeViews();
    }

    private bool IsSupported()
    {
        var snakeSet = new HashSet<Vector2I>(_snakeCells);
        foreach (var cell in _snakeCells)
        {
            var below = cell + Vector2I.Down;
            if (snakeSet.Contains(below) || _obstacles.Contains(below))
            {
                return true;
            }
        }
        return false;
    }

    private bool HitsBoundary(Vector2I cell)
    {
        return cell.X <= 0 || cell.X >= GridSize.X - 1 || cell.Y <= 0 || cell.Y >= GridSize.Y - 1;
    }

    private void BuildBoundaryWalls()
    {
        for (int x = 0; x < GridSize.X; x++)
        {
            AddWallCell(new Vector2I(x, 0));
            AddWallCell(new Vector2I(x, GridSize.Y - 1), addToObstacle: false); // 底墙可见但判定为致死
        }

        for (int y = 1; y < GridSize.Y - 1; y++)
        {
            AddWallCell(new Vector2I(0, y));
            AddWallCell(new Vector2I(GridSize.X - 1, y));
        }
    }

    private void BuildExtraWalls()
    {
        foreach (var cell in _extraWalls)
        {
            AddWallCell(cell);
        }
    }

    private void AddWallCell(Vector2I cell, bool addToObstacle = true)
    {
        if (WallScene == null) return;

        var wall = WallScene.Instantiate<Node2D>();
        if (wall is CellView view)
        {
            view.SetCell(cell);
        }
        else
        {
            wall.Position = new Vector2(cell.X * GridConfig.CellSize, cell.Y * GridConfig.CellSize);
        }

        _wallRoot.AddChild(wall);
        if (addToObstacle)
        {
            _obstacles.Add(cell);
        }
    }

    private void InitializeSnake()
    {
        _snakeCells.Clear();
        _snakeCells.Add(new Vector2I(5, 4));
        _snakeCells.Add(new Vector2I(4, 4));
        _snakeCells.Add(new Vector2I(3, 4));
        _snakeCells.Add(new Vector2I(2, 4));
    }

    private void SpawnFruit()
    {
        var used = new HashSet<Vector2I>(_snakeCells);
        used.UnionWith(_obstacles);

        var candidates = new List<Vector2I>();
        for (int y = 1; y < GridSize.Y - 1; y++)
        {
            for (int x = 1; x < GridSize.X - 1; x++)
            {
                var cell = new Vector2I(x, y);
                if (!used.Contains(cell))
                {
                    candidates.Add(cell);
                }
            }
        }

        if (candidates.Count == 0)
        {
            Die("地图没有可生成水果的位置");
            return;
        }

        _fruitCell = candidates[_rng.RandiRange(0, candidates.Count - 1)];
        RenderFruit();
    }

    private void RenderFruit()
    {
        foreach (var child in _fruitRoot.GetChildren())
        {
            (child as Node)?.QueueFree();
        }

        if (FruitScene == null) return;

        var fruit = FruitScene.Instantiate<Node2D>();
        if (fruit is CellView view)
        {
            view.SetCell(_fruitCell);
        }
        else
        {
            fruit.Position = new Vector2(_fruitCell.X * GridConfig.CellSize, _fruitCell.Y * GridConfig.CellSize);
        }

        _fruitRoot.AddChild(fruit);
    }

    private void RefreshSnakeViews()
    {
        foreach (var node in _snakeViews)
        {
            node.QueueFree();
        }
        _snakeViews.Clear();

        for (int i = 0; i < _snakeCells.Count; i++)
        {
            PackedScene prefab = i switch
            {
                0 => SnakeHeadScene,
                _ when i == _snakeCells.Count - 1 => SnakeTailScene,
                _ => SnakeBodyScene,
            };

            if (prefab == null) continue;
            var segment = prefab.Instantiate<Node2D>();

            if (segment is CellView view)
            {
                view.SetCell(_snakeCells[i]);
            }
            else
            {
                segment.Position = new Vector2(_snakeCells[i].X * GridConfig.CellSize, _snakeCells[i].Y * GridConfig.CellSize);
            }

            _snakeRoot.AddChild(segment);
            _snakeViews.Add(segment);
        }
    }

    private void Die(string reason)
    {
        _dead = true;
        GD.Print($"Game Over: {reason}. 按 Enter 重开。");
    }
}
