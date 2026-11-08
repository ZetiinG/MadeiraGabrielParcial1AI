using System;
using System.Collections.Generic;
using UnityEngine;

namespace Navigation
{
    public static class AStar
    {
        public static List<NavNode> FindPath(NavNode start, NavNode goal, Predicate<NavNode> isWalkable)
        {
            var open = new List<NavNode> { start };
            var closed = new HashSet<NavNode>();
            var cameFrom = new Dictionary<NavNode, NavNode>();
            var gScore = new Dictionary<NavNode, float> { [start] = 0f };
            var fScore = new Dictionary<NavNode, float> { [start] = Heuristic(start, goal) };

            while (open.Count > 0)
            {
                NavNode current = PopLowestScore(open, fScore);
                if (current == goal)
                    return ReconstructPath(cameFrom, current);

                closed.Add(current);

                foreach (NavNode neighbor in current.Neighbors)
                {
                    if (closed.Contains(neighbor) || !isWalkable(neighbor))
                        continue;

                    float tentativeScore = gScore[current] + Vector3.Distance(current.Position, neighbor.Position);
                    if (gScore.TryGetValue(neighbor, out float knownScore) && tentativeScore >= knownScore)
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeScore;
                    fScore[neighbor] = tentativeScore + Heuristic(neighbor, goal);

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }

            return null;
        }

        private static float Heuristic(NavNode from, NavNode to) => Vector3.Distance(from.Position, to.Position);

        private static NavNode PopLowestScore(List<NavNode> open, Dictionary<NavNode, float> fScore)
        {
            int bestIndex = 0;
            for (int i = 1; i < open.Count; i++)
            {
                if (fScore[open[i]] < fScore[open[bestIndex]])
                    bestIndex = i;
            }

            NavNode best = open[bestIndex];
            open.RemoveAt(bestIndex);
            return best;
        }

        private static List<NavNode> ReconstructPath(Dictionary<NavNode, NavNode> cameFrom, NavNode current)
        {
            var path = new List<NavNode> { current };
            while (cameFrom.TryGetValue(current, out current))
                path.Add(current);

            path.Reverse();
            return path;
        }
    }
}
