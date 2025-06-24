using DiseaseGraph.Extensions;
using System.Diagnostics;
using DiseaseGraph.Graph;
using System.Numerics;

namespace QuikGraph.Graph
{
    public class SpatialGraph<TNode>: GraphBase<TNode> where TNode : Node ,new()
    {
        public Dictionary<int, Vector2> NodePos { get; protected set;}
        public SpatialGraph(int numNodes,double timeStep, double baseInfectionChance, double baseViralLoad,List<Vector2> possibleNodeLocations, Func<double,double> edgeChanceDist,int? seed = null) 
        : base(timeStep,baseViralLoad,seed)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            NodePos = [];
            GenerateNodePositions(numNodes,possibleNodeLocations,true);
            MakeGraph([.. Enumerable.Range(0,numNodes)],GraphEdgeList(edgeChanceDist),[.. Enumerable.Repeat(baseInfectionChance,numNodes)]);
            Console.WriteLine($"SpacialGraph {numNodes}:{stopwatch.Elapsed.TotalSeconds}");
        }
        public SpatialGraph(int numNodes,double timeStep, double baseInfectionChance, double baseViralLoad,Func<Random,Vector2>nodePosDist , Func<double,double> edgeChanceDist,int? seed = null) 
        : base(timeStep,baseViralLoad,seed)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            NodePos = [];
            GenerateNodePositions(numNodes,nodePosDist);
            MakeGraph([.. Enumerable.Range(0,numNodes)],GraphEdgeList(edgeChanceDist),[.. Enumerable.Repeat(baseInfectionChance,numNodes)]);
            Console.WriteLine($"SpacialGraph {numNodes}:{stopwatch.Elapsed.TotalSeconds}");
        }
        public void GenerateNodePositions(int numNodes, List<Vector2> possibleLocations,bool process)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(possibleLocations.Count, numNodes, "Insufficient locations provided");
            List<Vector2> randomLocsEnum = process && numNodes != possibleLocations.Count ? [.. possibleLocations.Shuffle(Random).Take(numNodes)] : possibleLocations;
            for (int i = 0; i < numNodes; i++) NodePos.Add(i, randomLocsEnum[i]);
        }
        public void GenerateNodePositions(int numNodes, Func<Random,Vector2> nodePosDist)
        {
            GenerateNodePositions(numNodes, [.. Enumerable.Range(0,numNodes).Select(i => nodePosDist(Random))],false);
        }
        public List<Edge<int>> GraphEdgeList(Func<double,double> edgeChanceDist)
        {
            List<Edge<int>> edges = [];
            foreach (Edge<int> edge in PossibleEdges(NodePos.Count))
            {
                double distance = Vector2.Distance(NodePos[edge.Source],NodePos[edge.Target]);
                if (Random.NextDouble() < edgeChanceDist(distance)) edges.Add(edge);
            }
            return edges;
        }
        private static IEnumerable<Edge<int>> PossibleEdges(int numNodes)
        {
            for (int node1 = 0;node1 < numNodes-1; node1++)
            {
                for (int node2 = node1+1; node2 < numNodes -1; node2++) yield return new Edge<int>(node1,node2);
            }
        }
    }
}