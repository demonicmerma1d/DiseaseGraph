using DiseaseGraph.Graph;
using DiseaseGraph.Extensions;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph;
using QuikGraph.Algorithms;
using System.Security.Cryptography.X509Certificates;
using QuikGraph.Graph;

namespace DiseaseGraph.DataProcessing
{
    public static class DataProcessor  // need to add all the various analyitical metrics and shit still
    {
        private static int NewNodeStateCount(HashSet<NodeTimeEntry> stateTimeChange, NodeState nodeState,bool all)
        {
            return stateTimeChange.Count(x => x.NodeState == nodeState) - (all ? stateTimeChange.Count(x => x.OldNodeState == nodeState) : 0);
        }
        public static Dictionary<double,double[]> GraphStateChangesByTime(DataGraph graph,bool all = true) //convert the data into non spatial state changes
        {
            Dictionary<double, double[]> totalStateChangeByTime = [];
            var stateChangeData = graph.StateChanges.OrderBy(x => x.Key).ToList();
            foreach (var stateTimeChange in stateChangeData)
            {
                double[] changes = [NewNodeStateCount(stateTimeChange.Value,NodeState.Susceptible,all),NewNodeStateCount(stateTimeChange.Value,NodeState.Exposed,all),
                NewNodeStateCount(stateTimeChange.Value,NodeState.Infectious,all),NewNodeStateCount(stateTimeChange.Value,NodeState.Removed,all),
                stateTimeChange.Value.Count(x => !x.IsAlive) ];
                totalStateChangeByTime.Add(stateTimeChange.Key, changes);
            }
            return totalStateChangeByTime;
        }
        public static Dictionary<double,double[]> TotalStateMembers(DataGraph graph)
        {
            Dictionary<double, double[]> runningStateTotals = [];
            double[] currentTotals = [graph.NodeData.Count,0,0,0,0];
            foreach (var stateTimeChange in GraphStateChangesByTime(graph))
            {
                currentTotals = currentTotals.Add(stateTimeChange.Value);
                runningStateTotals.Add(stateTimeChange.Key, [.. currentTotals]);
            }
            return runningStateTotals;
        }
        public static double AverageShortestPath(DataGraph graph)
        {
            var shortestPaths = new FloydWarshallAllShortestPathAlgorithm<int, Edge<int>>(graph.Graph, x => 1);
            shortestPaths.Compute();
            List<double> pathLengths = [];
            foreach (int nodeSource in graph.NodeData)
            {
                foreach (int nodeTarget in graph.NodeData)
                {
                    if (shortestPaths.TryGetDistance(nodeSource,nodeTarget,out var distance)) pathLengths.Add(distance);
                }
            }
            return pathLengths.Average();
        }
        public static TryFunc<int, IEnumerable<Edge<int>>> ShortestPathFromRoot(DataGraph graph, int rootNode)
        {
            if (!graph.NodeData.Contains(rootNode)) throw new ArgumentException($"{rootNode} is not present in the graph");
            TryFunc<int, IEnumerable<Edge<int>>> tryGetPath = graph.Graph.ShortestPathsDijkstra(x => 1, rootNode);
            return tryGetPath;
        }
        public static double GlobalClusterCoeff(DataGraph graph)
        {
            var graphCopy = new UndirectedGraph<int,Edge<int>>();
            graphCopy.AddVertexRange(graph.Graph.Vertices);
            graphCopy.AddEdgeRange(graph.Graph.Edges.Where(e => e.Source < e.Target));
            double triplets = 0;
            double triangleCount = 0;
            while (graphCopy.EdgeCount > 1)
            {
                Edge<int> edge = graphCopy.Edges.First();
                graphCopy.RemoveEdge(edge);
                triplets += graphCopy.AdjacentEdges(edge.Source).Count();
                triplets += graphCopy.AdjacentEdges(edge.Target).Count();
                triangleCount += graphCopy.AdjacentEdges(edge.Source).Select(e => e.Target).Intersect(graphCopy.AdjacentEdges(edge.Target).Select(e => e.Target)).Count();
            }
            return 3*triangleCount/triplets; //global clustering coeff
        }
        public static double SmallWorldIndex(DataGraph graph) //relies on having an even numNodes or even integer ave degree
        {
            int numNodes = graph.NodeData.Count;
            var randomGraph = new ERGraph<Node>(numNodes, graph.EdgeDensity(), 0, 0, 0).ToDataGraph();
            Console.WriteLine(graph.Graph.Edges.Count() / numNodes);
            var latticeGraph = new SWGraph<Node>(numNodes, 0, 0, 0,graph.Graph.EdgeCount/numNodes, 0).ToDataGraph();
            var clusterCoeff_R = GlobalClusterCoeff(randomGraph);
            var clusterCoeff_L = GlobalClusterCoeff(latticeGraph);
            var avePath_R = AverageShortestPath(randomGraph);
            var avePath_L = AverageShortestPath(latticeGraph);
            return SmallWorldIndex(graph, clusterCoeff_R, clusterCoeff_L, avePath_R, avePath_L);
        }
        public static double SmallWorldIndex(DataGraph graph,double clusterCoeff_R,double clusterCoeff_L,double avePath_R,double avePath_L)
        {
            var clusterCoeff = GlobalClusterCoeff(graph);
            var avePath = AverageShortestPath(graph);
            return (avePath - avePath_L) * (clusterCoeff - clusterCoeff_R) / ((avePath_R - avePath_L) * (clusterCoeff_L-clusterCoeff_R));
        }
    } 
}