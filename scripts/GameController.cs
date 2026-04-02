using Godot;
using System.Collections.Generic;

public partial class GameController : Node2D
{
    [Export] public PackedScene SnakeHeadScene;
    [Export] public PackedScene SnakeBodyScene;
    [Export] public PackedScene SnakeTailScene;
    [Export] public PackedScene WallScene;

    [Export(PropertyHint.File, "*.tscn")] public string NextLevelScene = string.Empty;

    [Export] public Vector2I GridSize = new(24, 14);
    [Export] public float MoveIntervalSeconds = 0.45f;
    [Export] public float GravityIntervalSeconds = 0.12f;

    private readonly List<Vector2I> _snakeCells = new();
    private readonly List<Node2D> _snakeViews = new();
    private readonly HashSet<Vector2I> _obstacles = new();
    private readonly Dictionary<Vector2I, Node2D> _fruits = new();

    private Node2D _snakeRoot;
    private Node2D _boundaryRoot;
    private Node2D _obstacleLayer;
    private Node2D _fruitLayer;
    private Node2D _goalLayer;
    private Marker2D _spawnMarker;

    private CanvasLayer _uiLayer;
    private Label _statusLabel;
    private Label _finalLabel;

    private Vector2I _direction = Vector2I.Right;
    private Vector2I _nextDirection = Vector2I.Right;
    private Vector2I _barrelCell;

    private int _fruitsEaten;
    private float _moveTimer;
    private float _gravityTimer;
    private bool _dead;
    private bool _finished;

    public override void _Ready()
    {
        _obstacleLayer = GetNode<Node2D>("Obstacles");
        _fruitLayer = GetNode<Node2D>("Fruits");
        _goalLayer = GetNode<Node2D>("Goal");
        _spawnMarker = GetNode<Marker2D>("SnakeSpawn");

        _boundaryRoot = new Node2D { Name = "BoundaryRuntime" };
        _snakeRoot = new Node2D { Name = "SnakeRuntime" };
        AddChild(_boundaryRoot);
        AddChild(_snakeRoot);

        BuildUi();
        BuildBoundaryVisuals();
        ReadSceneData();
        InitializeSnake();
        RefreshSnakeViews();
        UpdateStatusUi();
    }

    public override void _Process(double delta)
    {
        if (_finished)
        {
            return;
        }

        if (_dead)
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                GetTree().ReloadCurrentScene();
            }
            return;
        }

        _gravityTimer += (float)delta;
        _moveTimer += (float)delta;

        bool fellThisFrame = false;
        while (_gravityTimer >= GravityIntervalSeconds)
        {
            _gravityTimer -= GravityIntervalSeconds;
            fellThisFrame |= ApplyGravityStep();
            if (_dead) return;
        }

        if (fellThisFrame)
        {
            return;
        }

        HandleInput();
        while (_moveTimer >= MoveIntervalSeconds)
        {
            _moveTimer -= MoveIntervalSeconds;
            StepMove();
            if (_dead || _finished) return;
        }
    }

    private void BuildUi()
    {
        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);

        _statusLabel = new Label { Position = new Vector2(12, 10) };
        _uiLayer.AddChild(_statusLabel);

        _finalLabel = new Label
        {
            Position = new Vector2(250, 180),
            Visible = false,
            Text = "恭喜通关"
        };
        _uiLayer.AddChild(_finalLabel);
    }

    private void ReadSceneData()
    {
        _obstacles.Clear();
        foreach (Node child in _obstacleLayer.GetChildren())
        {
            if (child is not Node2D node) continue;
            var cell = ToCell(node.Position);
            _obstacles.Add(cell);
        }

        _fruits.Clear();
        foreach (Node child in _fruitLayer.GetChildren())
        {
            if (child is not Node2D node) continue;
            var cell = ToCell(node.Position);
            _fruits[cell] = node;
        }

        _barrelCell = Vector2I.Zero;
        foreach (Node child in _goalLayer.GetChildren())
        {
            if (child is not Node2D node) continue;
            _barrelCell = ToCell(node.Position);
            break;
        }
    }

    private void InitializeSnake()
    {
        _snakeCells.Clear();
        var spawn = ToCell(_spawnMarker.Position);

        _snakeCells.Add(spawn);
        _snakeCells.Add(spawn + Vector2I.Left);
        _snakeCells.Add(spawn + Vector2I.Left * 2);
        _snakeCells.Add(spawn + Vector2I.Left * 3);

        _direction = Vector2I.Right;
        _nextDirection = Vector2I.Right;
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
            Die("撞到围墙或障碍物");
            return;
        }

        bool grow = _fruits.ContainsKey(newHead);
        var tail = _snakeCells[^1];
        if (_snakeCells.Contains(newHead) && (grow || newHead != tail))
        {
            Die("撞到自己");
            return;
        }

        _snakeCells.Insert(0, newHead);

        if (grow)
        {
            _fruitsEaten++;
            var fruitNode = _fruits[newHead];
            fruitNode.QueueFree();
            _fruits.Remove(newHead);
        }
        else
        {
            _snakeCells.RemoveAt(_snakeCells.Count - 1);
        }

        RefreshSnakeViews();
        TryClearLevel();
        UpdateStatusUi();
    }

    private bool ApplyGravityStep()
    {
        if (IsSupportedByObstacle())
        {
            return false;
        }

        var moved = new List<Vector2I>(_snakeCells.Count);
        foreach (var cell in _snakeCells)
        {
            var next = cell + Vector2I.Down;
            if (next.Y >= GridSize.Y - 1)
            {
                Die("掉到底部围墙");
                return false;
            }

            if (_obstacles.Contains(next))
            {
                Die("下落撞到障碍物");
                return false;
            }

            moved.Add(next);
        }

        _snakeCells.Clear();
        _snakeCells.AddRange(moved);
        RefreshSnakeViews();
        return true;
    }

    private bool IsSupportedByObstacle()
    {
        foreach (var cell in _snakeCells)
        {
            if (_obstacles.Contains(cell + Vector2I.Down))
            {
                return true;
            }
        }
        return false;
    }

    private void TryClearLevel()
    {
        if (_snakeCells[0] != _barrelCell)
        {
            return;
        }

        if (string.IsNullOrEmpty(NextLevelScene))
        {
            _finished = true;
            _finalLabel.Visible = true;
            _statusLabel.Text = "全部关卡完成";
            return;
        }

        GetTree().ChangeSceneToFile(NextLevelScene);
    }

    private bool HitsBoundary(Vector2I cell)
    {
        return cell.X <= 0 || cell.X >= GridSize.X - 1 || cell.Y <= 0 || cell.Y >= GridSize.Y - 1;
    }

    private void BuildBoundaryVisuals()
    {
        if (WallScene == null) return;

        for (int x = 0; x < GridSize.X; x++)
        {
            AddBoundaryWall(new Vector2I(x, 0));
            AddBoundaryWall(new Vector2I(x, GridSize.Y - 1));
        }

        for (int y = 1; y < GridSize.Y - 1; y++)
        {
            AddBoundaryWall(new Vector2I(0, y));
            AddBoundaryWall(new Vector2I(GridSize.X - 1, y));
        }
    }

    private void AddBoundaryWall(Vector2I cell)
    {
        var wall = WallScene.Instantiate<Node2D>();
        wall.Position = ToPosition(cell);
        _boundaryRoot.AddChild(wall);
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
            var prefab = i == 0 ? SnakeHeadScene : (i == _snakeCells.Count - 1 ? SnakeTailScene : SnakeBodyScene);
            if (prefab == null) continue;

            var segment = prefab.Instantiate<Node2D>();
            segment.Position = ToPosition(_snakeCells[i]);
            _snakeRoot.AddChild(segment);
            _snakeViews.Add(segment);
        }
    }

    private void UpdateStatusUi()
    {
        _statusLabel.Text = $"已吃水果: {_fruitsEaten} | 剩余水果: {_fruits.Count}";
    }

    private void Die(string reason)
    {
        _dead = true;
        _statusLabel.Text = $"失败：{reason}（Enter重开）";
    }

    private static Vector2I ToCell(Vector2 position)
    {
        return new Vector2I(Mathf.RoundToInt(position.X / GridConfig.CellSize), Mathf.RoundToInt(position.Y / GridConfig.CellSize));
    }

    private static Vector2 ToPosition(Vector2I cell)
    {
        return new Vector2(cell.X * GridConfig.CellSize, cell.Y * GridConfig.CellSize);
    }
}
