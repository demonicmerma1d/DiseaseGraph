using DiseaseGraph.Graph;
using DiseaseGraph.Extensions;
using QuikGraph.Algorithms.ShortestPath;
using QuikGraph;
using QuikGraph.Algorithms;

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
/*         public static Dictionary<double,double[]> InfectionStatsByDistance(DataGraph graph,int rootNode)
        {
            var distanceFunc = ShortestPathFromRoot(graph, rootNode);
            
        }  */      
    } 
}