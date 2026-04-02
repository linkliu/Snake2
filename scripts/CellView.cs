using Godot;

public partial class CellView : Node2D
{
    [Export] public Color Color = Colors.White;

    public override void _Ready()
    {
        if (GetNodeOrNull<ColorRect>("ColorRect") is { } rect)
        {
            rect.Color = Color;
            rect.Size = GridConfig.CellPixelSize;
        }
    }

    public void SetCell(Vector2I gridCell)
    {
        Position = new Vector2(gridCell.X * GridConfig.CellSize, gridCell.Y * GridConfig.CellSize);
    }
}
