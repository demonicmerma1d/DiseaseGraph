using QuikGraph;
using DiseaseGraph.Extensions;
using System.Diagnostics;
using FastDeepCloner;

namespace DiseaseGraph.Graph
{
    public class CommunityGraph<TNode> : GraphBase<TNode> where TNode : Node,new()
    {
        public CommunityGraph(int numNodes,int communitySize,double nodeOverlapProportion,double averageEdgeDensity,double internalEdgeProportion,
        double timeStep,double baseInfectionChance,double baseViralLoad,int? seed = null) : base(timeStep,baseViralLoad,seed)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            MakeGraph([.. Enumerable.Range(0,numNodes)],GraphEdgeList(numNodes, communitySize, nodeOverlapProportion, averageEdgeDensity, internalEdgeProportion),
                     [.. Enumerable.Repeat(baseInfectionChance, numNodes)]);
            Console.WriteLine($"CommunityGraph {numNodes}:{stopwatch.Elapsed.TotalSeconds}");
        }
        public List<Edge<int>> GraphEdgeList(int numNodes,int communitySize,double nodeOverlapProportion,
                double averageEdgeDensity,double internalEdgeProportion)
        {
            List<HashSet<int>> communities = [];
            List<int> allNodes = [.. Enumerable.Range(0, numNodes)];
            List<int> nodeSet = allNodes.Clone();
            while (true)
            {
                if (TryGenCommunity(nodeSet, communitySize, nodeOverlapProportion, out List<int> remainingNodeSet, out HashSet<int> newCommunity) ||
                TryGenCommunity(nodeSet, communitySize, nodeOverlapProportion, out remainingNodeSet, out newCommunity))
                {
                    communities.Add(newCommunity);
                    nodeSet = remainingNodeSet;
                    continue;
                }
                break;
            }
            return GenerateEdgeList(allNodes, communities, averageEdgeDensity, internalEdgeProportion);
            
            
        }
        protected List<Edge<int>> GenerateEdgeList(List<int> nodeList, List<HashSet<int>> communities, double averageEdgeDensity, double internalEdgeProportion)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(averageEdgeDensity);
            ArgumentOutOfRangeException.ThrowIfNegative(internalEdgeProportion);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(averageEdgeDensity, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(internalEdgeProportion, 1);
            HashSet<Edge<int>> edgeSet = [];
            HashSet<Edge<int>> allEdges = CompleteGraphEdges(nodeList);
            if (averageEdgeDensity == 1) return [.. allEdges];
            HashSet<Edge<int>> externalEdges = allEdges;
            Edge<int> edge;
            Edge<int> mirror;
            var randomEdges = GenerateEdgeList(nodeList, averageEdgeDensity,false);
            HashSet<Edge<int>> possibleEdges = [..allEdges.Except(randomEdges)]; //available edges to add
            foreach (HashSet<int> community in communities)
            {
                HashSet<Edge<int>> randomCommunityEdges = randomEdges.Where(edge => community.Contains(edge.Source)).ToHashSet(); //this is being empty????
                int goalCommunityDirectedInternalEdges = (int)Math.Floor(averageEdgeDensity * internalEdgeProportion * community.Count * (nodeList.Count - 1));
                int communityInternalEdgeCount = randomCommunityEdges.Where(edge => community.Contains(edge.Target)).Count();
                
                bool filterFunc(Edge<int> edge) => communityInternalEdgeCount > goalCommunityDirectedInternalEdges ?
                             community.Contains(edge.Target) : !community.Contains(edge.Target);
                
                HashSet<Edge<int>> possibleCommunityEdges = [..possibleEdges.Where(edge => community.Contains(edge.Source)).Where(filterFunc)];
                HashSet<Edge<int>> randomValidCommunityEdges = [..randomCommunityEdges.Where(filterFunc)];
                bool direction = communityInternalEdgeCount > goalCommunityDirectedInternalEdges;
                
                while (true)
                {
                    edge = Random.ChooseFrom(randomValidCommunityEdges);
                    mirror = edge.Mirror();
                    //remove original edge
                    randomValidCommunityEdges.Remove(edge);
                    randomEdges.Remove(edge);
                    //symetric mirror
                    randomValidCommunityEdges.Remove(mirror);
                    randomEdges.Remove(mirror);
                    //add new replacement edge and mirror
                    edge = Random.ChooseFrom(possibleCommunityEdges.Where(_edge => _edge.Source == edge.Source));
                    mirror = edge.Mirror();
                    possibleCommunityEdges.Remove(edge);
                    possibleCommunityEdges.Remove(mirror);
                    randomEdges.Add(edge);
                    randomEdges.Add(mirror);
                    if (direction) //update count based on which case we are in
                    {
                        communityInternalEdgeCount -= 2;
                        if (!(communityInternalEdgeCount > goalCommunityDirectedInternalEdges)) break;
                    }
                    else
                    {
                        communityInternalEdgeCount += 2;
                        if (!(communityInternalEdgeCount < goalCommunityDirectedInternalEdges)) break;
                    }
                }
            }
            return randomEdges;
        }
        private static HashSet<int> ConnectedNodesForNode(int sourceNode,Dictionary<int, HashSet<int>> connectedNodeData)
        {
            var connectedNodes = new HashSet<int>(sourceNode);
            foreach (int node in connectedNodeData[sourceNode]) connectedNodes = (HashSet<int>)connectedNodes.Union(CommunityGraph<TNode>.ConnectedNodesForNode(node, connectedNodeData));
            return connectedNodes;
        }
        public bool TryGenCommunity(List<int> nodeSet,int baseCommunitySize,double nodeOverlapProportion,out List<int> remainingNodeSet,out HashSet<int> community)
        {
            community = GenCommunity((int)Math.Floor(baseCommunitySize * Math.Pow(2, 2 * Random.NextDouble() - 1)),
                    nodeSet,out remainingNodeSet, nodeOverlapProportion);
            return community.Count > 0;
        }
        private HashSet<int> GenCommunity(int communitySize,List<int> nodeSet,out List<int> remainingNodeSet,double nodeOverlapProportion)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(communitySize);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(nodeOverlapProportion, 1);
            ArgumentOutOfRangeException.ThrowIfNegative(nodeOverlapProportion);
            remainingNodeSet = nodeSet;
            if (remainingNodeSet.Count < communitySize) return [];
            HashSet<int> community = [];
            for (int i = 0; i< communitySize; i++)
            {
                int nodeIdx = Random.Next(remainingNodeSet.Count);
                community.Add(remainingNodeSet[nodeIdx]);
                if (Random.NextDouble() < nodeOverlapProportion) continue;
                remainingNodeSet.RemoveAt(nodeIdx);
            }
            return community;
        }
    }
}