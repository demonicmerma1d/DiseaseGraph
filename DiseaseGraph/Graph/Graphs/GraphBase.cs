using System.Diagnostics;
using DiseaseGraph.Extensions;
using QuikGraph;
using FastDeepCloner;
using DiseaseGraph.DataProcessing;
using ScottPlot.FontResolvers;

namespace DiseaseGraph.Graph
{
    public readonly struct NodeTimeEntry(int nodeId,NodeState oldNodeState,NodeState nodeState,bool isAlive)
    {
        public readonly int NodeId = nodeId;
        public readonly NodeState OldNodeState = oldNodeState;
        public readonly NodeState NodeState = nodeState;
        public readonly bool IsAlive = isAlive;
    }
    public abstract class GraphBase<TNode>
    where TNode : Node, new()
    {
        private readonly TNode NodeObj = new();
        private static string SavePath 
        {
            get
            {
                string currentPath = Directory.GetCurrentDirectory();
                var directory = Directory.CreateDirectory(Path.Join(currentPath,"EdgeList"));
                return directory.FullName;
            }
        }
        public Dictionary<double,HashSet<NodeTimeEntry>> StateChanges;
        public AdjacencyGraph<int,Edge<int>> _graph;
        public Dictionary<int,TNode> NodeData;
        protected double TimeStep;
        private HashSet<int> TrackedNodes;
        private HashSet<int> InfectedNodes;
        public Random Random;
        protected double BaseViralLoad;
        protected GraphBase(double timeStep,double baseViralLoad, int? seed = null) //yes stuff is left unassigned, its fine
        {
            TimeStep = timeStep;
            TrackedNodes = [];
            InfectedNodes = [];
            Random = seed != null ? new((int)seed) : new();
            StateChanges = [];
            BaseViralLoad = baseViralLoad;
        }
        protected virtual void MakeGraph(List<int> vertexList,List<Edge<int>> edgeList,List<double> baseInfectionChances)
        {
            _graph = new();
            _graph.AddVertexRange(vertexList);
            _graph.AddEdgeRange(edgeList);
            NodeData = _graph.Vertices.ToDictionary(x => x,x => (TNode)NodeObj.Create(TimeStep,baseInfectionChances[x]));
        }
        protected virtual bool UpdateInfection(double currentTime, double infectionTime,double incubationTime) //only works with 1 type of transmission vector
        {
            HashSet<int> newInfectionChances = [];
            foreach (var nodeId in TrackedNodes)
            {
                NodeState nodeState = NodeData[nodeId].Update();
                if (NodeData[nodeId].ChangeState) ReportNodeChange(currentTime, nodeId);
                switch (nodeState) 
                {
                    case NodeState.Susceptible:
                        TrackedNodes.Remove(nodeId);
                        break;
                    case NodeState.Removed:
                        if (NodeData[nodeId].MarkedAsInfected)
                        {
                            NodeData[nodeId].MarkedAsInfected = false;
                            InfectedNodes.Remove(nodeId);
                        }
                        if (!NodeData[nodeId].IsAlive) TrackedNodes.Remove(nodeId);
                        break;
                    case NodeState.Exposed:
                        break;
                    case NodeState.Infectious:  //case of active infection spreading                         
                        foreach (var edge in _graph.OutEdges(nodeId))
                        {
                            if (!(NodeData[edge.Target].NodeState == NodeState.Susceptible)) continue;
                            newInfectionChances.Add(edge.Target);
                            NodeData[edge.Target].ViralLoad += NodeData[nodeId].ViralLoad;
                        }
                        break;
                    default:
                        throw new Exception($"Invalid NodeState for node {nodeId} at time {currentTime}");
                }
            }
            foreach (int nodeId in newInfectionChances) TryInfectNode(nodeId, infectionTime, incubationTime);
            return InfectedNodes.Count != 0;
        }
        public double Run(double maxTime,List<int> seedInfections,double infectionTime,double incubationTime = 0)
        {
            ResetNodes();
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (var nodeId in seedInfections)
            {
                InfectNode(nodeId, infectionTime, incubationTime);
                ReportNodeChange(0, nodeId);
            }
            for (double currentTime = 0; currentTime < maxTime; currentTime += TimeStep)
            {
                if (!UpdateInfection(currentTime, infectionTime, incubationTime)) break;
            }
            return stopwatch.Elapsed.TotalSeconds;
        }
        private void ResetNodes()
        {
            StateChanges.Clear();
            foreach (var node in NodeData.Values) node.Reset();
        }
        public void ReplaceInfectionChances(List<int> newInfectionChances)
        {
            if (newInfectionChances.Count != NodeData.Count) throw new Exception($"{newInfectionChances.Count} does not match the number of nodes,{NodeData.Count}");
            for (int idx = 0; idx < newInfectionChances.Count; idx++)
            {
                NodeData[idx].BaseInfectChance = newInfectionChances[idx];
            }
        }
        protected virtual bool TryInfectNode(int nodeId,double infectionTime,double incubationTime = 0) 
        {
            double infectionThreshold = NodeData[nodeId].BaseInfectChance * NodeData[nodeId].ViralLoad;
            double infectedCall = Random.NextDouble();
            if (infectedCall <= infectionThreshold)
            {
                InfectNode(nodeId, infectionTime, incubationTime, infectionThreshold,infectedCall);
                return true;
            }
            NodeData[nodeId].ViralLoad = 0;
            return false;
        }
        protected virtual void InfectNode(int nodeId,double infectionTime,double incubationTime,double infectionThreshold=0,double infectedCall=0) //change here for modififying infection chance behaviour
        {
            NodeData[nodeId].Infect(infectionTime,incubationTime,NodeData[nodeId].GetViralLoad(infectionThreshold,infectedCall,BaseViralLoad));
            InfectedNodes.Add(nodeId);
            TrackedNodes.Add(nodeId);
            NodeData[nodeId].MarkedAsInfected = true;
        }
        private void ReportNodeChange(double currentTime,int nodeId)
        {
            NodeData[nodeId].ChangeState = false;
            if (!StateChanges.ContainsKey(currentTime)) StateChanges.Add(currentTime, []);
            //Console.WriteLine($"{currentTime}:{nodeId}:"+NodeData[nodeId].ToString());
            var staticNode = new StaticNodeData<TNode>(NodeData[nodeId]);
            StateChanges[currentTime].Add(new(nodeId,staticNode.OldNodeState,staticNode.NodeState,staticNode.IsAlive));
        }
        protected List<Edge<int>> GenerateEdgeList(List<int> nodeList,double proportion,bool requireConnected = true,
                bool symmetric = true)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            ArgumentOutOfRangeException.ThrowIfNegative(proportion);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(proportion,1);
            HashSet<Edge<int>> possibleEdges = CompleteGraphEdges(nodeList);
            if (proportion == 1) return [.. possibleEdges];
            List<Edge<int>> edgeList = [];
            double goalEdges = nodeList.Count*(nodeList.Count-1)*proportion; //if undirected graph,double counted and no halving required, no self edges
            int edgeCount = 0;
            List<List<int>> connectedSets = [.. Enumerable.Range(0,nodeList.Count).Select(x=>new List<int>(){x})];
            Edge<int> edge;
            Edge<int> mirror;
            while (requireConnected && connectedSets.Count > 1)
            {
                var set1 = Random.ChooseFrom(connectedSets);
                connectedSets.Remove(set1);
                int idx2 = Random.Next(connectedSets.Count);
                var set2 = connectedSets[idx2];
                HashSet<Edge<int>> possibleChoices = [.. possibleEdges.Where(x => set1.Contains(x.Source) && set2.Contains(x.Target))];
                edge = Random.ChooseFrom(possibleChoices);
                connectedSets[idx2].AddRange(set1);
                possibleEdges.Remove(edge);
                edgeList.Add(edge);
                edgeCount++;
                if (!symmetric) continue;
                mirror = edge.Mirror();
                possibleEdges.Remove(mirror);
                edgeList.Add(mirror);
                edgeCount++;
            }
            while (edgeCount++ < goalEdges)
            {
                edge = Random.ChooseFrom(possibleEdges);
                possibleEdges.Remove(edge);
                edgeList.Add(edge);
                if (!symmetric) continue;
                mirror = edge.Mirror();
                possibleEdges.Remove(mirror);
                edgeList.Add(mirror);
                edgeCount++;
            }
            Console.WriteLine(stopwatch.Elapsed.TotalSeconds);
            return edgeList;
        }
        public GraphBase<TNode> Copy()
        {
            var clone = this.Clone();
            clone.TrackedNodes.Clear();
            clone.InfectedNodes.Clear();
            clone.StateChanges.Clear();
            return clone;
        }
        public void SaveEdgeListToFile()
        {
            string baseName = FileName($"edges");
            string name = baseName;
            int i = 0;
            var files = Directory.GetFiles(SavePath, "*.txt");
            while (files.Contains(name))
            {
                name = $"{i++}-{baseName}";
            }
            using (var sw = new StreamWriter(Path.Combine(SavePath, name)))
            {
                foreach (var edge in _graph.Edges) sw.WriteLine($"{edge.Source}:{edge.Target}"); //may need to add to this in future if add weighted edges
            }
        }
        public List<Edge<int>> LoadEdgeListFromFile(string name)
        {
            string path = Path.Combine(SavePath, name);
            if (!Path.Exists(path)) throw new FileNotFoundException(path);
            List<string[]> edgeStrings = [.. from line in File.ReadAllLines(path) select line.Replace(Environment.NewLine,"").Split(":")];
            List<Edge<int>> edges = [.. from edgeString in edgeStrings 
                select new Edge<int>(int.Parse(edgeString[0]),int.Parse(edgeString[1]))];
            return edges;
        }
        protected static HashSet<Edge<int>> CompleteGraphEdges(List<int> nodeSet) //complete graph
        {
            if (nodeSet.Count == 0) throw new ArgumentOutOfRangeException("There are no nodes");
            return [.. from node1 in nodeSet from node2 in nodeSet where node1!=node2 select new Edge<int>(node1,node2)];
        }
        public string FileName(string callFuncId = "",bool dateTime = false)
        {
            string graphTypeName = this.NameOf();
            string nodeTypeName = NodeObj.NameOf();
            graphTypeName = graphTypeName[..graphTypeName.IndexOf("`")];
            string name = callFuncId + $"-{NodeData.Count}-{graphTypeName}-{nodeTypeName}";
            if (dateTime) name += $"-{DateTime.Now:yyyyMMddHHmmss}";
            return name;
        }
        private Dictionary<double,HashSet<NodeTimeEntry>> StateChangesCopy() //manual deep clone function
        {
            Dictionary<double, HashSet<NodeTimeEntry>> copy = [];
            foreach (double time in StateChanges.Keys)
            {
                copy[time] = [];
                foreach (NodeTimeEntry entry in StateChanges[time])
                {
                    copy[time].Add(new(entry.NodeId,entry.OldNodeState,entry.NodeState,entry.IsAlive));
                }
            }
            return copy;            
        }
        public DataGraph ToDataGraph()
        {
            return new(StateChangesCopy(), _graph, [..NodeData.Keys],FileName());
        }
        public void UpdateBaseInfectionChance(double newChance)
        {
            foreach (TNode node in NodeData.Values) node.BaseInfectChance = newChance;
        }
    }
}