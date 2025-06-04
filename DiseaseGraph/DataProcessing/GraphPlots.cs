using System.Drawing;
using DiseaseGraph.Graph;
using QuikGraph;
using QuikGraph.Graphviz;
using QuikGraph.Graphviz.Dot;
using GraphVizNet;
using ScottPlot;

namespace DiseaseGraph.DataProcessing
{
    public static class GraphPlots
    {
        private static string SavePath 
        {
            get
            {
                string currentPath = Directory.GetCurrentDirectory();
                var directory = Directory.CreateDirectory(Path.Join(currentPath,"GraphPlots"));
                return directory.FullName;
            }
        }
        public static Dictionary<NodeState,IEnumerable<int>> GraphStateAtTime(this DataGraph graph, double time)
        {
            Dictionary<int, NodeState> nodeStates = graph.NodeData.ToDictionary(nodeId => nodeId,nodeId => NodeState.Susceptible);
            foreach (var timeData in graph.StateChanges.OrderBy(x => x.Key))
            {
                if (timeData.Key > time) break;
                foreach (NodeTimeEntry nodeChanges in timeData.Value) nodeStates[nodeChanges.NodeId] = nodeChanges.NodeState;
            }
            return Enum.GetValues<NodeState>().ToDictionary(nodeState => nodeState, nodeState => nodeStates.Keys.Where(nodeId => nodeStates[nodeId] == nodeState));
        }
        private static UndirectedGraph<int,Edge<int>> ToUndirectedGraph(this AdjacencyGraph<int,Edge<int>> graph)
        {
            UndirectedGraph<int, Edge<int>> undirected = new();
            undirected.AddVertexRange(graph.Vertices);
            undirected.AddEdgeRange([.. graph.Edges.Where(edge => edge.Source < edge.Target)]);
            return undirected;
        }
        private static GraphvizColor NodeColorByState(NodeState nodeState)
            => nodeState switch
            {
                NodeState.Susceptible => GraphvizColor.BlueViolet,
                NodeState.Exposed => GraphvizColor.Green,
                NodeState.Infectious => GraphvizColor.Red,
                NodeState.Removed => GraphvizColor.Blue,
                NodeState.Dead => GraphvizColor.Black,
                _ => throw new NotImplementedException()
            };
        private static GraphvizColor NodeColor(this Dictionary<NodeState,IEnumerable<int>> nodeStates,int nodeId)
        {
            NodeState currentNodeState = NodeState.Susceptible;
            foreach (var nodeState in nodeStates.Keys)
            {
                currentNodeState = nodeState;
                if (nodeStates[nodeState].Contains(nodeId)) break;
            }
            return NodeColorByState(currentNodeState);
        }
        public static void RenderToPNG(this DataGraph graph,double time) //not tested, should be fine idk???
        {
            UndirectedGraph<int,Edge<int>> reducedGraph = graph.Graph.ToUndirectedGraph();
            var graphState = graph.GraphStateAtTime(time);
            string dotGraph = reducedGraph.ToGraphviz(algorithm =>
            {
                algorithm.FormatVertex += (sender, args) =>
                {
                    args.VertexFormat.StrokeColor = graphState.NodeColor(args.Vertex);
                };
            });
            var graphviz = new GraphViz();
            graphviz.LayoutAndRenderDotGraph(dotGraph, DataUtilities.ValidFileName(SavePath, graph.FileName($"{graph.NodeData.Count}-{time}"), ".png"), "png");
        }
    }
    
}