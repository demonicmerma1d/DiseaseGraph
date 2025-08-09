using DiseaseGraph.Graph;
using DiseaseGraph.Simulations;
using QuikGraph.Graph;
using DiseaseGraph.Extensions;
namespace DiseaseGraph
{
    class Program
    {
        static void Main(string[] args)
        {
            //RunGraphSim.GraphParamTestNodeCountConst(5,[.. from i in Enumerable.Range(1,20) select 50*i], 0.1,0.02);
            Console.WriteLine(DateTime.Now);
            //ParameterTuning.TimeStepDemo(true);
            //RunGraphSim.PlotInfectedNodeCount([100, 200], 2, 0.01, 0.01);
            //ParameterTuning.NodeCountDemo(0.01,null);
            //ParameterTuning.NodeCountDemo(0.01,3);
            //ParameterTuning.BaseInfectionChanceParamTuning();
            ParameterTuning.InfectionProportionParamTuning();
            Console.WriteLine(DateTime.Now);
            Console.ReadKey();
        }
        
    }
}