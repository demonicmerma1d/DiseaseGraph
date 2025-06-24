using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using DiseaseGraph.Extensions;
using QuikGraph;
using static MoreLinq.Extensions.PairwiseExtension;

namespace DiseaseGraph.Graph
{
    public class SFGraph<TNode> : GraphBase<TNode> where TNode : Node, new() //k-pyramid scale free
    {
        public Dictionary<int, HashSet<int>> NodeScales { get; protected set; }
        public SFGraph(int numNodes, int lvlConnections, double timeStep, double baseInfectionChance, double baseViralLoad, int? seed = null)
        : base(timeStep, baseViralLoad, seed)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MakeGraph([.. Enumerable.Range(0, numNodes)], [.. Enumerable.Repeat(baseInfectionChance, numNodes)]);
            NodeScales = Enumerable.Range(0, numNodes).ToDictionary(x => x, x => new HashSet<int>());
            AddGraphEdges(numNodes, lvlConnections);
            Console.WriteLine($"SFGraph {numNodes}:{stopwatch.Elapsed.TotalSeconds}");
        }
        public void AddGraphEdges(int numNodes, int lvlConnections)
        {
            HashSet<int> allAvailableNodes = [.. Enumerable.Range(0, numNodes)];
            while (AddPyramid(ref allAvailableNodes, lvlConnections));
            //step D of algorithm, cycle without subcycles for lvl 1 nodes
            IEnumerable<int> allLvl1Nodes = NodeScales.Where(x => x.Value.Count == 0).Select(x => x.Key).Shuffle(Random);
            _graph.AddEdgeRange([new(allLvl1Nodes.First(), allLvl1Nodes.Last()),new(allLvl1Nodes.Last(),allLvl1Nodes.First())]);
            var cycleEdges = allLvl1Nodes.Pairwise((n1, n2) => new Edge<int>(n1, n2));
            foreach (var edge in cycleEdges) _graph.AddEdgeRange([edge, edge.Mirror()]);
        }
        private bool AddPyramid(ref HashSet<int> allAvailableNodes,int lvlConnections)
        {
            int maxNodeLvls = PyramidHeight(allAvailableNodes.Count, lvlConnections);
            if (maxNodeLvls <= 1) return false;
            HashSet<int> lvl1Nodes = ChooseMultipleAndRemove(ref allAvailableNodes, (int)Math.Pow(lvlConnections, maxNodeLvls - 1));
            for (int nodeLvl = 2; nodeLvl <= maxNodeLvls; nodeLvl++ )
            {
                var lvlNodes = ChooseMultipleAndRemove(ref allAvailableNodes, (int)Math.Pow(lvlConnections, maxNodeLvls - nodeLvl));
                foreach (var node in lvlNodes)
                {
                    var selectedLowerNodes = ChooseMultipleAndRemove(ref allAvailableNodes, lvlConnections);
                    NodeScales[node] = selectedLowerNodes;
                    AddEdgesForLvlNode(node, node);
                }
            }
            return true;
        }
        private void AddEdgesForLvlNode(int sourceNode,int node)
        {
            if (NodeScales[node].Count == 0) //indicates it is a lvl 1 node for the algorithm
            {
                _graph.AddEdgeRange([new Edge<int>(sourceNode, node), new Edge<int>(node, sourceNode)]);
                return;
            }
            foreach (var childNode in NodeScales[node]) //else the node is not lvl 1 and has children
            {
                AddEdgesForLvlNode(sourceNode, childNode);
            }
        }
        private HashSet<int> ChooseMultipleAndRemove(ref HashSet<int> nodes, int numToChoose)
        {
            HashSet<int> chosen = [.. ChooseMultiple(nodes, numToChoose)];
            nodes.ExceptWith(chosen);
            return chosen;
        }
        private IEnumerable<int> ChooseMultiple(HashSet<int> nodes, int numToChoose)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(numToChoose, nodes.Count);
            for (int i = 0; i < numToChoose; i++)
            {
                var nextNode = Random.ChooseFrom(nodes);
                nodes.Remove(nextNode);
                yield return nextNode;
            }
        }
        private static int PyramidHeight(int numNodes, int lvlConnections)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(lvlConnections, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(lvlConnections, numNodes);
            return (int)Math.Floor(Math.Log(numNodes * (lvlConnections - 1) + 1) / Math.Log(lvlConnections));
        }
    }
}