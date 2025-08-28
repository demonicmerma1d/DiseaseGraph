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
            if (allAvailableNodes.Count == 0) return false;
            int maxNodeLvls = PyramidHeight(allAvailableNodes.Count, lvlConnections); //maximise the number of lvls possible
            if (maxNodeLvls <= 1) return false;
            HashSet<int> lowerNodes = ChooseMultipleAndRemove(ref allAvailableNodes, (int)Math.Pow(lvlConnections, maxNodeLvls - 1)); //1st lvl nodes default
            HashSet<int> upperNodes = []; //need to initialise the variable
            int nodeLvl = 1;
            while (nodeLvl++ < maxNodeLvls)
            {
                upperNodes = ChooseMultipleAndRemove(ref allAvailableNodes, (int)Math.Pow(lvlConnections,maxNodeLvls - nodeLvl)); //select the set of control nodes
                foreach (var node in upperNodes)
                {
                    NodeScales[node] = ChooseMultipleAndRemove(ref lowerNodes, Math.Min(lvlConnections,lowerNodes.Count)); //pick lvlConnection nodes to "control"
                    AddEdgesForLvlNode(node, node); //recusively search down the chain to connect the node to the all the relevent 1st lvl nodes
                }
                //lowerNodes should be empty!!!!
                if (lowerNodes.Count > 0) throw new Exception($"There are {lowerNodes.Count} nodes unclaimed");
                lowerNodes.UnionWith(upperNodes); //move the "control" nodes to the next lower node set
            }
            return true;
        }
        private void AddEdgesForLvlNode(int sourceNode,int node)
        {
            if (NodeScales[node].Count == 0) //indicates it is a lvl 1 node for the algorithm thus termination of this branch
            {
                _graph.AddEdgeRange([new(node,sourceNode),new(sourceNode,node)]);
                return;
            }
            foreach (var childNode in NodeScales[node]) //else the node is not lvl 1 and has nodes in its "control"
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
            if (lvlConnections > numNodes) return 1;
            return (int)Math.Floor(Math.Log(numNodes * (lvlConnections - 1) + 1) / Math.Log(lvlConnections));
        }
    }
}