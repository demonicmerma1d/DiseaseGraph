using System.Diagnostics;
using QuikGraph;
namespace DiseaseGraph.Graph
{
    public class ERGraph<TNode> : GraphBase<TNode> where TNode:Node,new() //random graph
    {
        public ERGraph(int numNodes,double proportion, double timeStep,double baseInfectionChance,double baseViralLoad,int? seed=null) 
            : base(timeStep,baseViralLoad, seed)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                MakeGraph([.. Enumerable.Range(0,numNodes)],GraphEdgeList(numNodes,proportion,false),[.. Enumerable.Repeat(baseInfectionChance,numNodes)]);
            Console.WriteLine($"ERGraph {numNodes}:{stopwatch.Elapsed.TotalSeconds}");
            }
        public List<Edge<int>> GraphEdgeList(int numNodes,double proportion,bool requireConnected) 
        {
            return GenerateEdgeList([.. Enumerable.Range(0,numNodes)], proportion, requireConnected, true);
        }
    }
}