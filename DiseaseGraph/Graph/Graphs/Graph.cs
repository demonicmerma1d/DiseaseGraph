using QuikGraph;
namespace DiseaseGraph.Graph
{
    public class Graph<TNode> : GraphBase<TNode> where TNode:Node,new() //general structure for subclassing out the base class,static infection probability
    {
        public Graph(int numNodes,double proportion, double timeStep,double baseInfectionChance,double baseViralLoad,int? seed=null) 
            : base(timeStep,baseViralLoad, seed)
            {
                MakeGraph([.. Enumerable.Range(0,numNodes)],GraphEdgeList(numNodes,proportion,false),[.. Enumerable.Repeat(baseInfectionChance,numNodes)]);
            }
        public List<Edge<int>> GraphEdgeList(int numNodes,double proportion,bool requireConnected) 
        {
            return GenerateEdgeList([.. Enumerable.Range(0,numNodes)], proportion, requireConnected, true);
        }
    }
}