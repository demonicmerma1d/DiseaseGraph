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
            Console.WriteLine(DateTime.Now);
            //ParameterTuning.TimeStepDemo(true);
            //ParameterTuning.NodeCountDemo(0.02,null);
            //ParameterTuning.NodeCountDemo(0.02,10);
            RunGraphSim.AveragePathLengths();
            //ParameterTuning.BaseInfectionChanceParamTuning();
            //ParameterTuning.InfectionProportionParamTuning();
            //ParameterTuning.SFGraphDensityTuning(135000);
            RunParams runParams = new(1, 1, 1.5);
            //RunGraphSim.RunSimStats<IsolationNode>(100, 1000,0.0005,25,runParams);
            //RunGraphSim.RunSimStats<IsolationNode>(100, 1000,0.0003,25,runParams);
            //RunGraphSim.RunSimStats<IsolationNodeSS>(100, 1000,0.0005,25,runParams);
            //RunGraphSim.RunSimStats<IsolationNodeSS>(100, 1000,0.0003,25,runParams);
            //RunGraphSim.TestRunSim();
            //RunGraphSim.CopyTest();
            Console.WriteLine(DateTime.Now);
            Console.ReadKey();
        }
        
    }
}