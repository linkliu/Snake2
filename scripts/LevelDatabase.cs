using Godot;
using System.Collections.Generic;

public static class LevelDatabase
{
    public static IReadOnlyList<LevelData> CreateLevels()
    {
        return new List<LevelData>
        {
            new(
                "Level 1",
                new Vector2I(4, 9),
                Vector2I.Right,
                new Vector2I(18, 3),
                2,
                new List<Vector2I>
                {
                    new(4, 10), new(5, 10), new(6, 10),
                    new(10, 9), new(11, 9), new(12, 9),
                    new(15, 7), new(16, 7), new(17, 7),
                }),
            new(
                "Level 2",
                new Vector2I(5, 10),
                Vector2I.Right,
                new Vector2I(19, 4),
                3,
                new List<Vector2I>
                {
                    new(5, 11), new(6, 11),
                    new(8, 9), new(9, 9), new(10, 9),
                    new(13, 8), new(14, 8),
                    new(17, 6), new(18, 6),
                }),
            new(
                "Level 3",
                new Vector2I(3, 9),
                Vector2I.Right,
                new Vector2I(20, 2),
                3,
                new List<Vector2I>
                {
                    new(3, 10), new(4, 10),
                    new(7, 8), new(8, 8),
                    new(11, 7), new(12, 7), new(13, 7),
                    new(16, 5), new(17, 5),
                }),
            new(
                "Level 4",
                new Vector2I(6, 10),
                Vector2I.Right,
                new Vector2I(19, 3),
                4,
                new List<Vector2I>
                {
                    new(6, 11), new(7, 11),
                    new(9, 10), new(10, 10),
                    new(12, 8), new(13, 8), new(14, 8),
                    new(17, 6), new(18, 6),
                }),
            new(
                "Level 5",
                new Vector2I(4, 8),
                Vector2I.Right,
                new Vector2I(18, 2),
                4,
                new List<Vector2I>
                {
                    new(4, 9), new(5, 9),
                    new(7, 7), new(8, 7), new(9, 7),
                    new(12, 6), new(13, 6),
                    new(15, 5), new(16, 5),
                }),
            new(
                "Level 6",
                new Vector2I(5, 10),
                Vector2I.Right,
                new Vector2I(20, 3),
                5,
                new List<Vector2I>
                {
                    new(5, 11), new(6, 11),
                    new(8, 9), new(9, 9),
                    new(11, 8), new(12, 8), new(13, 8),
                    new(15, 7), new(16, 7),
                    new(18, 5), new(19, 5),
                }),
            new(
                "Level 7",
                new Vector2I(3, 10),
                Vector2I.Right,
                new Vector2I(18, 3),
                5,
                new List<Vector2I>
                {
                    new(3, 11), new(4, 11),
                    new(6, 10), new(7, 10),
                    new(9, 8), new(10, 8),
                    new(12, 7), new(13, 7),
                    new(15, 6), new(16, 6),
                }),
            new(
                "Level 8",
                new Vector2I(6, 9),
                Vector2I.Right,
                new Vector2I(20, 2),
                6,
                new List<Vector2I>
                {
                    new(6, 10), new(7, 10),
                    new(9, 9), new(10, 9),
                    new(12, 7), new(13, 7), new(14, 7),
                    new(16, 6), new(17, 6),
                    new(18, 4), new(19, 4),
                }),
            new(
                "Level 9",
                new Vector2I(4, 10),
                Vector2I.Right,
                new Vector2I(19, 2),
                6,
                new List<Vector2I>
                {
                    new(4, 11), new(5, 11),
                    new(7, 9), new(8, 9),
                    new(10, 8), new(11, 8),
                    new(13, 6), new(14, 6),
                    new(16, 5), new(17, 5),
                    new(18, 4),
                }),
            new(
                "Level 10",
                new Vector2I(5, 10),
                Vector2I.Right,
                new Vector2I(20, 2),
                7,
                new List<Vector2I>
                {
                    new(5, 11), new(6, 11),
                    new(8, 10), new(9, 10),
                    new(11, 9), new(12, 9),
                    new(14, 8), new(15, 8),
                    new(16, 6), new(17, 6),
                    new(18, 5), new(19, 5),
                }),
        };
    }
}
