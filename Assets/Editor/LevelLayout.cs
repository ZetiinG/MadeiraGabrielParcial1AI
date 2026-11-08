using System.Collections.Generic;
using UnityEngine;
using Navigation;

namespace EditorTools
{
    internal static class LevelLayout
    {
        public const float CellSize = 3f;

        public const char WallCell = '#';
        public const char EmptyCell = ' ';
        public const char CorridorCell = '.';
        public const char StartCell = 'S';
        public const char HuntingGroundCell = 'C';
        public const char SafeCell = 'Z';
        public const char ExitCell = 'E';

        // Variante A (anillo) de la consigna. Los pasillos ocupan filas impares y columnas pares para que caigan sobre los nodos.
        private static readonly string[] Layout =
        {
            "     #######################    ",
            "     #.....................#    ",
            "     #.#####.#####.#######.#    ",
            "     #.#####.......###ZZZ#.#    ",
            "     #.#####.......###ZZZ#.#    ",
            "     #.#######.#######ZZZ#.#    ",
            "######.####CCCCCCCCC##.###.#####",
            "#SSS##.####CCCCCCCCC##.###.#EEE#",
            "#SSS##.####CCCCCCCCC##.###.#EEE#",
            "#SSS.......CCCCCCCCC........EEE#",
            "#SSS##.#.##CCCCCCCCC######.#EEE#",
            "#SSS##.#.##CCCCCCCCC######.#EEE#",
            "######.#.##CCCCCCCCC######.#####",
            "     #.#ZZ####.###########.#    ",
            "     #.#ZZ##.......#######.#    ",
            "     #.#ZZ##.......#######.#    ",
            "     #.#####.#####.#######.#    ",
            "     #.....................#    ",
            "     #######################    ",
        };

        public static readonly WolfRoute[] WolfRoutes =
        {
            new WolfRoute("L1", WaypointTraversalMode.PingPong,
                new Vector2Int(8, 1), new Vector2Int(6, 1), new Vector2Int(6, 17), new Vector2Int(8, 17)),
            new WolfRoute("L2", WaypointTraversalMode.Loop,
                new Vector2Int(10, 1), new Vector2Int(12, 3), new Vector2Int(18, 3), new Vector2Int(22, 1)),
            new WolfRoute("L3", WaypointTraversalMode.PingPong,
                new Vector2Int(24, 1), new Vector2Int(26, 1), new Vector2Int(26, 17), new Vector2Int(24, 17)),
            new WolfRoute("L4", WaypointTraversalMode.Loop,
                new Vector2Int(10, 17), new Vector2Int(12, 15), new Vector2Int(18, 15), new Vector2Int(22, 17)),
        };

        private static readonly Vector2Int[] CardinalDirections =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        public static int Width => Layout[0].Length;
        public static int Height => Layout.Length;

        public readonly struct WolfRoute
        {
            public readonly string Name;
            public readonly WaypointTraversalMode Mode;
            public readonly Vector2Int[] Cells;

            public WolfRoute(string name, WaypointTraversalMode mode, params Vector2Int[] cells)
            {
                Name = name;
                Mode = mode;
                Cells = cells;
            }
        }

        // Un nodo cada dos celdas sobre la grilla de pasillos: cubre cruces, esquinas y el interior de las salas.
        public static bool IsNodeCell(int col, int row)
        {
            char cell = Layout[row][col];
            return col % 2 == 0 && row % 2 == 1 && cell != WallCell && cell != EmptyCell;
        }

        public static List<RectInt> MergeCells(char cell)
        {
            var rects = new List<RectInt>();
            var used = new bool[Width, Height];

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (used[col, row] || Layout[row][col] != cell)
                        continue;

                    int width = 1;
                    while (col + width < Width && !used[col + width, row] && Layout[row][col + width] == cell)
                        width++;

                    int height = 1;
                    while (row + height < Height && IsSpanAvailable(cell, used, col, row + height, width))
                        height++;

                    MarkUsed(used, new RectInt(col, row, width, height));
                    rects.Add(new RectInt(col, row, width, height));
                }
            }

            return rects;
        }

        public static List<RectInt> FindRegions(char cell)
        {
            var regions = new List<RectInt>();
            var visited = new bool[Width, Height];

            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    if (!visited[col, row] && Layout[row][col] == cell)
                        regions.Add(FloodFillBounds(cell, new Vector2Int(col, row), visited));
                }
            }

            return regions;
        }

        public static Vector3 CellToWorld(float col, float row)
        {
            return new Vector3((col + 0.5f - Width * 0.5f) * CellSize, 0f, (Height * 0.5f - row - 0.5f) * CellSize);
        }

        public static Vector3 RectCenter(RectInt rect)
        {
            return CellToWorld(rect.x + (rect.width - 1) * 0.5f, rect.y + (rect.height - 1) * 0.5f);
        }

        public static Vector2 ZoneSize(RectInt region) => new Vector2(region.width * CellSize, region.height * CellSize);

        private static bool IsSpanAvailable(char cell, bool[,] used, int col, int row, int width)
        {
            for (int c = col; c < col + width; c++)
            {
                if (used[c, row] || Layout[row][c] != cell)
                    return false;
            }

            return true;
        }

        private static void MarkUsed(bool[,] used, RectInt rect)
        {
            for (int row = rect.yMin; row < rect.yMax; row++)
            {
                for (int col = rect.xMin; col < rect.xMax; col++)
                    used[col, row] = true;
            }
        }

        private static RectInt FloodFillBounds(char cell, Vector2Int start, bool[,] visited)
        {
            var pending = new Stack<Vector2Int>();
            pending.Push(start);
            visited[start.x, start.y] = true;
            Vector2Int min = start;
            Vector2Int max = start;

            while (pending.Count > 0)
            {
                Vector2Int current = pending.Pop();
                min = Vector2Int.Min(min, current);
                max = Vector2Int.Max(max, current);

                foreach (Vector2Int direction in CardinalDirections)
                {
                    Vector2Int next = current + direction;
                    if (next.x < 0 || next.x >= Width || next.y < 0 || next.y >= Height)
                        continue;
                    if (visited[next.x, next.y] || Layout[next.y][next.x] != cell)
                        continue;

                    visited[next.x, next.y] = true;
                    pending.Push(next);
                }
            }

            return new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
        }
    }
}
