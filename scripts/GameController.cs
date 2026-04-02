using Godot;
using System.Collections.Generic;

public partial class GameController : Node2D
{
    [Export] public PackedScene SnakeHeadScene;
    [Export] public PackedScene SnakeBodyScene;
    [Export] public PackedScene SnakeTailScene;
    [Export] public PackedScene FruitScene;
    [Export] public PackedScene WallScene;
    [Export] public PackedScene OilBarrelScene;

    [Export] public Vector2I GridSize = new(24, 14);
    [Export] public float MoveIntervalSeconds = 0.45f;
    [Export] public float GravityIntervalSeconds = 0.12f;

    private readonly List<Vector2I> _snakeCells = new();
    private readonly List<Node2D> _snakeViews = new();
    private readonly HashSet<Vector2I> _obstacles = new();
    private readonly List<LevelData> _levels = new();

    private Node2D _snakeRoot;
    private Node2D _wallRoot;
    private Node2D _fruitRoot;
    private Node2D _barrelRoot;

    private CanvasLayer _uiLayer;
    private Label _statusLabel;
    private Label _finalLabel;

    private Vector2I _direction = Vector2I.Right;
    private Vector2I _nextDirection = Vector2I.Right;
    private Vector2I _fruitCell;
    private Vector2I _barrelCell;

    private int _currentLevelIndex;
    private int _fruitsEaten;
    private int _requiredFruits;

    private float _moveTimer;
    private float _gravityTimer;
    private bool _dead;
    private bool _gameCompleted;

    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _levels.AddRange(LevelDatabase.CreateLevels());

        _wallRoot = new Node2D { Name = "WallRoot" };
        _fruitRoot = new Node2D { Name = "FruitRoot" };
        _barrelRoot = new Node2D { Name = "BarrelRoot" };
        _snakeRoot = new Node2D { Name = "SnakeRoot" };

        AddChild(_wallRoot);
        AddChild(_fruitRoot);
        AddChild(_barrelRoot);
        AddChild(_snakeRoot);

        BuildUi();
        LoadLevel(0);
    }

    public override void _Process(double delta)
    {
        if (_gameCompleted)
        {
            return;
        }

        if (_dead)
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                LoadLevel(_currentLevelIndex);
            }
            return;
        }

        _gravityTimer += (float)delta;
        _moveTimer += (float)delta;

        bool fellThisFrame = false;
        while (_gravityTimer >= GravityIntervalSeconds)
        {
            _gravityTimer -= GravityIntervalSeconds;
            bool fell = ApplyGravityStep();
            fellThisFrame |= fell;

            if (_dead)
            {
                return;
            }
        }

        // 下落过程中禁止移动
        if (fellThisFrame)
        {
            return;
        }

        HandleInput();
        while (_moveTimer >= MoveIntervalSeconds)
        {
            _moveTimer -= MoveIntervalSeconds;
            StepMove();

            if (_dead || _gameCompleted)
            {
                return;
            }
        }
    }

    private void BuildUi()
    {
        _uiLayer = new CanvasLayer();
        AddChild(_uiLayer);

        _statusLabel = new Label
        {
            Position = new Vector2(12, 10),
            Text = string.Empty
        };
        _uiLayer.AddChild(_statusLabel);

        _finalLabel = new Label
        {
            Position = new Vector2(250, 180),
            Text = string.Empty,
            Visible = false
        };
        _uiLayer.AddChild(_finalLabel);
    }

    private void LoadLevel(int levelIndex)
    {
        _currentLevelIndex = levelIndex;
        _dead = false;
        _moveTimer = 0;
        _gravityTimer = 0;

        ClearChildren(_snakeRoot);
        ClearChildren(_wallRoot);
        ClearChildren(_fruitRoot);
        ClearChildren(_barrelRoot);
        _snakeViews.Clear();
        _obstacles.Clear();

        BuildBoundaryWalls();

        var level = _levels[levelIndex];
        _requiredFruits = level.RequiredFruits;
        _fruitsEaten = 0;
        _barrelCell = level.BarrelCell;

        _direction = level.InitialDirection;
        _nextDirection = level.InitialDirection;

        BuildLevelObstacles(level);
        InitializeSnake(level);
        RenderOilBarrel();
        SpawnFruit();
        RefreshSnakeViews();
        UpdateStatusUi($"{level.Name} | 水果: 0/{_requiredFruits}");
    }

    private void BuildLevelObstacles(LevelData level)
    {
        foreach (var cell in level.Obstacles)
        {
            AddWallCell(cell, true);
        }

        // 确保蛇出生点下方有障碍物
        var support = level.SnakeSpawnHead + Vector2I.Down;
        if (support.Y < GridSize.Y - 1)
        {
            AddWallCell(support, true);
        }
    }

    private void InitializeSnake(LevelData level)
    {
        _snakeCells.Clear();
        var head = level.SnakeSpawnHead;
        _snakeCells.Add(head);
        _snakeCells.Add(head - level.InitialDirection);
        _snakeCells.Add(head - level.InitialDirection * 2);
        _snakeCells.Add(head - level.InitialDirection * 3);
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

        bool grow = newHead == _fruitCell;
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
            SpawnFruit();
        }
        else
        {
            _snakeCells.RemoveAt(_snakeCells.Count - 1);
        }

        RefreshSnakeViews();
        TryConsumeBarrel();
        UpdateStatusUi($"{_levels[_currentLevelIndex].Name} | 水果: {_fruitsEaten}/{_requiredFruits}");
    }

    // 返回 true 表示本步发生了下落
    private bool ApplyGravityStep()
    {
        // 规则：只要蛇任意格子下方有障碍物，就不下坠
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
            var below = cell + Vector2I.Down;
            if (_obstacles.Contains(below))
            {
                return true;
            }
        }
        return false;
    }

    private void TryConsumeBarrel()
    {
        if (_snakeCells[0] != _barrelCell)
        {
            return;
        }

        if (_fruitsEaten < _requiredFruits)
        {
            UpdateStatusUi($"还需要吃水果: {_requiredFruits - _fruitsEaten}");
            return;
        }

        if (_currentLevelIndex == _levels.Count - 1)
        {
            _gameCompleted = true;
            _finalLabel.Text = "恭喜通关";
            _finalLabel.Visible = true;
            UpdateStatusUi("全部关卡完成");
            return;
        }

        LoadLevel(_currentLevelIndex + 1);
    }

    private void SpawnFruit()
    {
        var used = new HashSet<Vector2I>(_snakeCells);
        used.UnionWith(_obstacles);
        used.Add(_barrelCell);

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
            Die("没有可生成水果的位置");
            return;
        }

        _fruitCell = candidates[_rng.RandiRange(0, candidates.Count - 1)];
        RenderFruit();
    }

    private void RenderFruit()
    {
        ClearChildren(_fruitRoot);
        if (FruitScene == null) return;

        var fruit = FruitScene.Instantiate<Node2D>();
        SetNodeCell(fruit, _fruitCell);
        _fruitRoot.AddChild(fruit);
    }

    private void RenderOilBarrel()
    {
        ClearChildren(_barrelRoot);
        if (OilBarrelScene == null) return;

        var barrel = OilBarrelScene.Instantiate<Node2D>();
        SetNodeCell(barrel, _barrelCell);
        _barrelRoot.AddChild(barrel);
    }

    private void BuildBoundaryWalls()
    {
        for (int x = 0; x < GridSize.X; x++)
        {
            AddWallCell(new Vector2I(x, 0), false);
            AddWallCell(new Vector2I(x, GridSize.Y - 1), false);
        }

        for (int y = 1; y < GridSize.Y - 1; y++)
        {
            AddWallCell(new Vector2I(0, y), false);
            AddWallCell(new Vector2I(GridSize.X - 1, y), false);
        }
    }

    private bool HitsBoundary(Vector2I cell)
    {
        return cell.X <= 0 || cell.X >= GridSize.X - 1 || cell.Y <= 0 || cell.Y >= GridSize.Y - 1;
    }

    private void AddWallCell(Vector2I cell, bool asObstacle)
    {
        if (WallScene != null)
        {
            var wall = WallScene.Instantiate<Node2D>();
            SetNodeCell(wall, cell);
            _wallRoot.AddChild(wall);
        }

        if (asObstacle)
        {
            _obstacles.Add(cell);
        }
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
            SetNodeCell(segment, _snakeCells[i]);
            _snakeRoot.AddChild(segment);
            _snakeViews.Add(segment);
        }
    }

    private static void SetNodeCell(Node2D node, Vector2I cell)
    {
        if (node is CellView view)
        {
            view.SetCell(cell);
        }
        else
        {
            node.Position = new Vector2(cell.X * GridConfig.CellSize, cell.Y * GridConfig.CellSize);
        }
    }

    private static void ClearChildren(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            (child as Node)?.QueueFree();
        }
    }

    private void UpdateStatusUi(string text)
    {
        _statusLabel.Text = text;
    }

    private void Die(string reason)
    {
        _dead = true;
        UpdateStatusUi($"{_levels[_currentLevelIndex].Name} 失败：{reason}（Enter重开）");
    }
}
