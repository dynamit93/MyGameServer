using MyGameServer;
using OpenTibiaCommons.Domain;

public class Pathfinding
{
    private GameWorld gameWorld;

    public Pathfinding(GameWorld gameWorld)
    {
        this.gameWorld = gameWorld;
    }


    public List<Point3D> FindPath(Point3D start, Point3D goal)
    {
        var openSet = new PriorityQueue<Node, int>();
        openSet.Enqueue(new Node(start), 0);

        // Add a HashSet to keep track of nodes in the priority queue
        var openSetHash = new HashSet<Point3D> { start };

        var closedSet = new HashSet<Point3D>();
        var cameFrom = new Dictionary<Point3D, Point3D>();
        var gScore = new Dictionary<Point3D, int> { [start] = 0 };
        var fScore = new Dictionary<Point3D, int> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue().Data;
            openSetHash.Remove(current);  // Remove the node from the hash set

            if (current.Equals(goal))
            {
                return ReconstructPath(cameFrom, current);
            }

            closedSet.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                {
                    continue;
                }

                if (!gScore.ContainsKey(neighbor))
                {
                    gScore[neighbor] = int.MaxValue;
                }

                int tentativeGScore = gScore[current] + Distance(current, neighbor);

                if (tentativeGScore >= gScore[neighbor])
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, goal);

                if (!openSetHash.Contains(neighbor))
                {
                    openSet.Enqueue(new Node(neighbor), fScore[neighbor]);
                    openSetHash.Add(neighbor);  // Add the neighbor to the hash set
                }
            }
        }

        return new List<Point3D>();  // return an empty path if there is no path
    }

    private List<Point3D> ReconstructPath(Dictionary<Point3D, Point3D> cameFrom, Point3D current)
    {
        var totalPath = new List<Point3D> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }

        return totalPath;
    }

    private int Heuristic(Point3D a, Point3D b)
    {
        // Manhattan distance on a square grid
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private int Distance(Point3D a, Point3D b)
    {
        // Distance between two nodes, assuming adjacent nodes are at distance 1
        return 1;
    }

    private IEnumerable<Point3D> GetNeighbors(Point3D node)
    {
        var directions = new[]
        {
            new Point3D(0, 1, 0),  // Up
            new Point3D(0, -1, 0), // Down
            new Point3D(1, 0, 0),  // Right
            new Point3D(-1, 0, 0), // Left
            // Add diagonals if your game allows diagonal movement
        };

        foreach (var direction in directions)
        {
            var neighbor = new Point3D(node.X + direction.X, node.Y + direction.Y, node.Z);
            if (gameWorld.IsTileWalkable(neighbor))
            {
                yield return neighbor;
            }
        }
    }
    public IEnumerable<Point3D> GetWalkableNeighbors(Point3D node)
    {
        var directions = new List<Point3D>
    {
        new Point3D(0, 1, 0), // North
        new Point3D(1, 0, 0), // East
        new Point3D(0, -1, 0), // South
        new Point3D(-1, 0, 0), // West
        // Add more directions for a 3D grid if needed
    };

        foreach (var direction in directions)
        {
            var neighbor = new Point3D(node.X + direction.X, node.Y + direction.Y, node.Z + direction.Z);
            if (gameWorld.IsTileWalkable(neighbor))
            {
                yield return neighbor;
            }
        }
    }

    private class Node : IComparable<Node>
    {
        public Point3D Data { get; }
        public int Priority { get; }

        public Node(Point3D data, int priority = 0)
        {
            Data = data;
            Priority = priority;
        }

        public int CompareTo(Node other)
        {
            return Priority.CompareTo(other.Priority);
        }
    }
}
