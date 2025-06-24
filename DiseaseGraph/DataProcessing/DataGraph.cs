using DiseaseGraph.Graph;
using FastDeepCloner;
using QuikGraph;

namespace DiseaseGraph.DataProcessing
{
    public class DataGraph(Dictionary<double, HashSet<NodeTimeEntry>> stateChanges, AdjacencyGraph<int, Edge<int>> graph, HashSet<int> nodeData,string baseFileName)
    {
        public readonly Dictionary<double,HashSet<NodeTimeEntry>> StateChanges = stateChanges;
        public readonly AdjacencyGraph<int,Edge<int>> Graph = graph.Clone();
        public readonly HashSet<int> NodeData = nodeData;
        private readonly string BaseFileName = baseFileName;
        public string FileName(string callFuncId = "",bool dateTime = false)
        {
            string name = callFuncId + BaseFileName;
            if (dateTime) name += $"-{DateTime.Now:yyyyMMddHHmmss}";
            return name;
        }
        public double EdgeDensity()
        {
            return (double)Graph.EdgeCount/(NodeData.Count * (NodeData.Count - 1));
        } 
        public double AverageNodeDegree {get{ return NodeData.Select(Graph.OutDegree).Average(); }}
    }
}