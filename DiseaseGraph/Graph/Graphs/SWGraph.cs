using DiseaseGraph.Extensions;
using System.Diagnostics;
using DiseaseGraph.Graph;
using System.Numerics;
using FastDeepCloner;

namespace QuikGraph.Graph
{
    public class SWGraph<TNode>: GraphBase<TNode> where TNode : Node ,new() //small world
    {
        public Dictionary<int, Vector2> NodePos { get; protected set;}
        public SWGraph(int numNodes,double timeStep, double baseInfectionChance, double baseViralLoad,int aveNodeDeg,double rewireProbability,int? seed = null) 
        : base(timeStep,baseViralLoad,seed)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            NodePos = NodeLocs(numNodes);
            MakeGraph([.. Enumerable.Range(0,numNodes)],[.. Enumerable.Repeat(baseInfectionChance,numNodes)]); //we want to just manually build this up
            AddGraphEdges(numNodes, aveNodeDeg, rewireProbability);
            Console.WriteLine($"SWGraph {numNodes}:{stopwatch.Elapsed.TotalSeconds}");
        }
        public void AddGraphEdges(int numNodes,int aveNodeDeg,double rewireProbability)
        {
            GenerateRingLattice(numNodes, aveNodeDeg);
            if (rewireProbability == 0) return;
            var edgesToIterate = _graph.Edges.ToHashSet();
            while (edgesToIterate.Count != 0)
            {
                var edge = edgesToIterate.First();
                edgesToIterate.Remove(edge);
                edgesToIterate.Remove(edge.Mirror());
                if (!(Random.NextDouble() < rewireProbability)) continue;
                _graph.TryGetOutEdges(edge.Source, out var outEdges);
                Edge<int> newEdge = new(edge.Source,Random.ChooseFrom(Enumerable.Range(0,numNodes).Except(outEdges.Select(x => x.Target).Except([edge.Source]))));
                _graph.AddEdgeRange([newEdge,newEdge.Mirror()]);
            }
        }
        private void GenerateRingLattice(int numNodes, int aveNodeDeg)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(aveNodeDeg, numNodes, "aveNodeDeg must be less than numNodes");
            ArgumentOutOfRangeException.ThrowIfNegative(numNodes, "numNodes must be positive");
            if (aveNodeDeg * numNodes % 2 != 0) throw new Exception("aveNodeDeg*numNodes must be even");
            _graph.AddEdgeRange([.. from node1 in Enumerable.Range(0,numNodes) from node2 in Enumerable.Range(0,numNodes)
                                 where node1 != node2 && (Math.Abs(node1 - node2) % (numNodes -1 - aveNodeDeg/2) <= aveNodeDeg/2 )
                                 select new Edge<int>(node1,node2)]); //even aveNodeDeg handling
            if (aveNodeDeg % 2 == 0) return; //odd case needs specific handling
            HashSet<Edge<int>> oddDegExtraEdges = [.. Enumerable.Range(0, numNodes).SelectMany(
            node => new HashSet<Edge<int>>([new Edge<int>(node, node + aveNodeDeg / 2 + 1 % numNodes), new Edge<int>(node, node - aveNodeDeg / 2 + 1 % numNodes)]))];
            while (oddDegExtraEdges.Count > 0) //assign exactly 1 more edge connected to each node
            {
                var currentEdge = Random.ChooseFrom(oddDegExtraEdges);
                HashSet<int> nowFinishedNodes = [currentEdge.Source, currentEdge.Target];
                var toRemove = oddDegExtraEdges.Where(edge => nowFinishedNodes.Contains(edge.Source) || nowFinishedNodes.Contains(edge.Target));
                oddDegExtraEdges.ExceptWith(toRemove);                
            }
        }
        private static Dictionary<int,Vector2> NodeLocs(int numNodes) // get numNodes equal spaceed points on circle for a ring lattice WS model
        {
            return Enumerable.Range(0, numNodes).ToDictionary(x => x,x => new Vector2(numNodes*MathF.Cos(x/numNodes),numNodes*MathF.Sin(x/numNodes)));
        }
    }
}