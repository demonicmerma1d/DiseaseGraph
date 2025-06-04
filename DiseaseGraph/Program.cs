using DiseaseGraph.Graph;
using DiseaseGraph.DataProcessing;
namespace DiseaseGraph
{
    class Program
    {
        static void Main(string[] args)
        {
            //RunGraphSim.GraphParamTestNodeCountConst(5,[.. from i in Enumerable.Range(1,20) select 50*i], 0.1,0.02);
            //RunGraphSim.PlotDieoutBaseInfect(300, 50, 0.1, [.. from i in Enumerable.Range(0, 500) select i * 0.0001],5);
            RunGraphSim.PlotDieoutNodeCount([.. Enumerable.Range(20, 281)], 100, 0.11, 0.01,5);
        }
    }
}