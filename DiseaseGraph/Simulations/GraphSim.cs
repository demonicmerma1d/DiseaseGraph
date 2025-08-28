using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using DiseaseGraph.DataProcessing;
using DiseaseGraph.Extensions;
using DiseaseGraph.Graph;
using QuikGraph.Graph;

namespace DiseaseGraph.Simulations
{
    public static class RunGraphSim
    {
        public static List<DataGraph> RunForSeeds<TGraph,TNode>(this TGraph graph,double maxTime, List<int> seedInfections, int numRuns,double infectionTime,double incubationTime = 0)
            where TGraph : GraphBase<TNode> where TNode : Node,new()
        {
            List<DataGraph> graphData = [];
            RunParams runParams = new(infectionTime, incubationTime);
            for (int i=0; i<numRuns; i++)
            {
                Console.WriteLine(graph.Run(maxTime, seedInfections, runParams));
                graphData.Add(graph.ToDataGraph());
            }
            return graphData;
        }
        public static double EdgeDensity(int numNodes,double aveNodeDegree) //calculation method for edge density to prevent degree proportional to node count(for undirected)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(numNodes - 1, aveNodeDegree);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(numNodes, 1);
            return 2 * aveNodeDegree / (numNodes - 1);
        }
        public static double RunSimForRandomSeed<TNode,TGraph>(TGraph graph,double maxTime,int numRuns,RunParams runParams)
            where TGraph : GraphBase<TNode> where TNode : Node, new()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < numRuns; i++)
            {
                graph.RunRandom(maxTime,runParams);
                var dataGraph = graph.ToDataGraph();
                DataPlots.PlotTotalsGraph(dataGraph, Enum.GetValues<NodeState>(), "Node Counts by state over time.");
                Console.WriteLine(dataGraph.BaseFileName+$":{dataGraph.GetTotalProportionInfected()}");
                //DataPlots.PlotStateChangeGraph(dataGraph,Enum.GetValues<NodeState>(), "Change in Node Counts by state over time",true);
            }
            double simTime = stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"{simTime} for {numRuns} runs ");
            return simTime;
        }
        public static void RunSimForRandomSeedInfectStats<TNode,TGraph>(TGraph graph,double maxTime,int numRuns,RunParams runParams,int threshold)
            where TGraph : GraphBase<TNode> where TNode : Node, new()
        {
            ConcurrentBag<double> dataInfProp = [];
            ConcurrentBag<double> dataInfRunTime = [];
            for (int i = 0; i < numRuns; i++)
            {
                graph.RunRandom(maxTime, runParams, out var simInternalTime);
                dataInfRunTime.Add(simInternalTime);
                var dataGraph = graph.ToDataGraph();
                dataInfProp.Add(dataGraph.GetTotalProportionInfected());
            };
            var arrDataInfProp = dataInfProp.ToArray();
            var arrDataRunTime = dataInfRunTime.ToArray();
            Console.WriteLine(graph.FileName()+$" BIC:{graph.NodeData[0].BaseInfectChance}");
            Console.WriteLine($"Infection Proportion M:V:Fails {arrDataInfProp.Average()}:{arrDataInfProp.Variance()}:{arrDataInfProp.Count(x => x*graph.NodeData.Count < threshold)}");
            Console.WriteLine($"Sim RunTime M:SD {arrDataRunTime.Average()}:{Math.Sqrt(arrDataRunTime.Variance())}");
            double infectionsPerUnit = Enumerable.Range(0, arrDataInfProp.Length).Select(i => arrDataInfProp[i]/arrDataRunTime[i]).ToArray().Average();
            Console.WriteLine($"Infections per unit time M {infectionsPerUnit}");
            Console.WriteLine($"{DataUtilities.ValidFileName(DataPlots.SavePath, "InfPropHist1000",".png").Split("\\").Last().Split(".").First()}\n");
            InfectionHistogram(arrDataInfProp);
        }
        public static void RunSimStats<TNode>(double maxTime, int numRuns, double baseInfectionChance, int threshold, RunParams runParams) where TNode : Node, new()
        {
            double timeStep = 0.01;
            ERGraph<TNode> erGraph = new(1000, EdgeDensity(1000, 18), timeStep, baseInfectionChance, 1);
            SFGraph<TNode> sfGraph = new(135000, 2, timeStep, baseInfectionChance, 1); //works out to have average node deg of 17.x 
            SWGraph<TNode> swGraphLatt = new(1000, timeStep, baseInfectionChance, 1, 18, 0); //lattice
            SWGraph<TNode> swGraphLess = new(1000, timeStep, baseInfectionChance, 1, 18, 0.3); //small world less
            SWGraph<TNode> swGraphMore = new(1000, timeStep, baseInfectionChance, 1, 18, 0.6); //small world more
            IEnumerable<GraphBase<TNode>> graphs = [erGraph,sfGraph,swGraphLatt,swGraphLess,swGraphMore];
            var isoChances = new List<double>([0, 0.1, 0.2]);
            foreach (var isoChance in isoChances)
                {
                    Console.WriteLine("\n");
                    foreach (var graph in graphs) graph.UpdateIsolationChances(isoChance);
                    foreach (var graph in graphs) RunSimForRandomSeedInfectStats<TNode, GraphBase<TNode>>(graph, maxTime, numRuns, runParams, threshold,isoChance);
                };
        }
        public static void RunSimForRandomSeedInfectStats<TNode,TGraph>(TGraph graph,double maxTime,int numRuns,RunParams runParams,int threshold,double isoChance)
            where TGraph : GraphBase<TNode> where TNode : Node, new()
            {
                Console.WriteLine($"Isolate chance {isoChance}");
                RunSimForRandomSeedInfectStats<TNode, TGraph>(graph, maxTime, numRuns, runParams, threshold);
            }
        public static void RunSim<TNode>(double maxTime,int numRuns,double baseInfectionChance) where TNode: Node,new()
        {
            double timeStep = 0.01;
            //ERGraph<TNode> erGraph = new(1000, EdgeDensity(1000, 18), timeStep, baseInfectionChance, 1);
            //SFGraph<TNode> sfGraph = new(4000, 2, timeStep, baseInfectionChance, 1); //works out to have average node deg of 17.x 
            //SWGraph<TNode> swGraph = new(1000, timeStep, baseInfectionChance, 1, 18,0); //lattice
            SWGraph<TNode> swGraph = new(1000, timeStep, baseInfectionChance, 1, 18, 0.3); //small world less
            //SWGraph<TNode> swGraph = new(1000, timeStep, baseInfectionChance, 1, 18, 0.6); //small world more
            IEnumerable<GraphBase<TNode>> graphs = [swGraph];
            
            //BIC T_e=T_i=1
            RunParams runParams1 = new(1, 1, 0);
            foreach (var graph in graphs) RunSimForRandomSeed<TNode,GraphBase<TNode>>(graph,maxTime, numRuns, runParams1); 
        }
        public static void TestRunSim()
        {
            double timeStep = 0.01;
            SFGraph<IsolationNode> Graph = new(400, 4, timeStep, 0.0005, 1); //works out to have average node deg of 17.x
            RunParams runParams1 = new(1, 1, 0);
            RunSimForRandomSeedInfectStats<IsolationNode, GraphBase<IsolationNode>>(Graph, 100, 50,runParams1,25);
        }
        public static void InfectionHistogram(double[] infectionProportions)
        {
            DataPlots.PlotHist(infectionProportions, 100, "Infected Proportion", "Number of Simulations",
            "Distribution of infected proportions", "InfPropHist1000");
        }
        public static void AveragePathLengths()
        {
            double baseInfectionChance = 0; //we arent running simulations just need graphs
            double timeStep = 0;
            ERGraph<Node> erGraph = new(1000, EdgeDensity(1000, 18), timeStep, baseInfectionChance, 1);
            SFGraph<Node> sfGraph = new(135000, 2, timeStep, baseInfectionChance, 1); //works out to have average node deg of 17.x 
            SWGraph<Node> swGraphLatt = new(1000, timeStep, baseInfectionChance, 1, 18, 0); //lattice
            SWGraph<Node> swGraphLess = new(1000, timeStep, baseInfectionChance, 1, 18, 0.3); //small world less
            SWGraph<Node> swGraphMore = new(1000, timeStep, baseInfectionChance, 1, 18, 0.6); //small world more
            IEnumerable<GraphBase<Node>> graphs =[erGraph,sfGraph,swGraphLatt,swGraphLess,swGraphMore];
            foreach (GraphBase<Node> graph in graphs)
            {
                var dataGraph = graph.ToDataGraph();
                var pathLength = DataProcessor.AverageShortestPath(dataGraph);
                Console.WriteLine($"graphID:{graph.FileName()}:{pathLength}");
            }    
        }
    }
} 