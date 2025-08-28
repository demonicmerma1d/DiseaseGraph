using DiseaseGraph.Graph;
using DiseaseGraph.DataProcessing;
using DiseaseGraph.Extensions;
using QuikGraph;
using ScottPlot.Interactivity;
using System.Collections.Concurrent;
using ScottPlot.AxisPanels;

namespace DiseaseGraph.Simulations
{
    public static class ParameterTuning
    {

        public static double EdgeDensity(int numNodes, double aveNodeDegree) //calculation method for edge density to prevent degree proportional to node count(for undirected)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(numNodes - 1, aveNodeDegree);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(numNodes, 1);
            return 2 * aveNodeDegree / (numNodes - 1);
        }
        public static void SEIRDemoGraphOffset()
        {
            int numNodes = 2000;
            ERGraph<IncubateNode> graph = new(numNodes, EdgeDensity(numNodes, 20), 0.08, 0.02, 1);
            RunParams runParams = new(3, 1);
            Console.WriteLine($"runtime:{graph.RunRandom(100, runParams)}");
            DataPlots.PlotStateChangeGraph(graph.ToDataGraph(), [NodeState.Exposed, NodeState.Infectious], "SEIR node type new exposed/infectious nodes over time", false);
        }
        public static double GetTotalProportionInfected(this DataGraph graph)
        {
            return 1 - DataProcessor.TotalStateMembers(graph).MaxBy(state => state.Key).Value[(int)NodeState.Susceptible] / graph.NodeData.Count;
        }
        private static double[] TimeSteps = [0.06, 0.03, 0.02, 0.015, 0.012, 0.01];
        private static string[] Labels = [.. TimeSteps.Select(t => t.ToString())];
        public static void TimeStepDemo(bool adjustInfectionChance)
        {
            int numNodes = 1000;
            ERGraph<IncubateNode> graph = new(numNodes, EdgeDensity(numNodes, 5), 0.01, 0.01, 1);
            List<DataGraph> dataGraphs = [.. TimeStepGraphs(graph, adjustInfectionChance)];
            DataPlots.MultiPlotChangesForState(dataGraphs, Labels, NodeState.Infectious, "New Infected Node Counts over time compared by timestep", false);
            DataPlots.MultiPlotTotalsForState(dataGraphs, Labels, NodeState.Infectious, "Total Infected Node Counts over time compared by timestep");
        }
        private static IEnumerable<DataGraph> TimeStepGraphs(ERGraph<IncubateNode> graph, bool adjustInfectionChance)
        {
            RunParams runParams = new(3, 1);
            int RepCount = 1;
            foreach (double timeStep in TimeSteps)
            {
                if (adjustInfectionChance) graph.UpdateBaseInfectionChanceToAll(graph.NodeData.Values.First().BaseInfectChance * timeStep / graph.TimeStep);
                graph.UpdateTimestep(timeStep);
                for (int i = 0; i < RepCount; i++)
                {
                    graph.RunRandom(100, runParams);
                    yield return graph.ToDataGraph();
                }
            }
        }
        public static Dictionary<double, double[]> InfectedProportionsByNodeCount(int[] nodeCounts, int repeatCount, double proportion, double baseInfectionChance, double? aveNodeDegree = null)
        {
            List<KeyValuePair<double, List<double>>> infectedProportionsByNodeCount = [.. from nodeCount in nodeCounts
                select new KeyValuePair<double,List<double>>(nodeCount,InfectedProportions(nodeCount,repeatCount,proportion,baseInfectionChance,aveNodeDegree))];
            return infectedProportionsByNodeCount.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }
        public static List<double> InfectedProportions(int numNodes, int repeatCount, double proportion, double baseInfectionChance, double? aveNodeDegree = null)
        {
            RunParams runParams = new(1, 1);
            var graph = new ERGraph<IncubateNode>(numNodes, aveNodeDegree != null ? EdgeDensity(numNodes, (double)aveNodeDegree) : proportion, 0.01, baseInfectionChance, 1);
            List<double> proportions = [];
            for (var i = 0; i < repeatCount; i++)
            {
                graph.RunRandom(100, runParams);
                proportions.Add(graph.ToDataGraph().GetTotalProportionInfected());
            }
            return proportions;
        }
        public static Dictionary<double, Dictionary<double, double[]>> InfectedProportionsForBICs(int[] nodeCounts, int repeatCount, double proportion, double[] baseInfectionChances, double? aveNodeDegree = null)
        {
            ConcurrentDictionary<double, ConcurrentDictionary<double, double[]>> allData = [];
            foreach (var baseInfectionChance in baseInfectionChances) allData.TryAdd(baseInfectionChance, []);
            RunParams runParams = new(1, 1);
            foreach (var nodeCount in nodeCounts)
            {
                List<KeyValuePair<double, List<double>>> nodeCountInfectedData = [];
                var graph = new ERGraph<IncubateNode>(nodeCount, aveNodeDegree != null ? EdgeDensity(nodeCount, (double)aveNodeDegree) : proportion, 0.01, 0, 1);
                foreach (double baseInfectionChance in baseInfectionChances)
                {
                    graph.UpdateBaseInfectionChanceToAll(baseInfectionChance);
                    List<double> proportions = [];
                    for (var i = 0; i < repeatCount; i++)
                    {
                        graph.RunRandom(100, runParams);
                        proportions.Add(graph.ToDataGraph().GetTotalProportionInfected());
                    }
                    allData[baseInfectionChance][nodeCount] = [.. proportions];
                }
            }
            ;
            return allData.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));
        }
        public static void NodeCountAveInfectPlot(int[] nodeCounts, int repeatCount, double proportion, double[] baseInfectionChances, double? aveNodeDegree = null)
        {
            Dictionary<double, Dictionary<double, double[]>> allInfectionData = InfectedProportionsForBICs(nodeCounts, repeatCount, proportion, baseInfectionChances, aveNodeDegree);
            List<Dictionary<double, double[]>> infectedProportions = [.. baseInfectionChances.Select(x => allInfectionData[x])];
            DataPlots.MultiPlotInfectionStatGraph(infectedProportions, x => x.Variance(), "Varience of infected proportion by node count", "NodeCount", "Varience", $"VarNC-{baseInfectionChances.Min()}_{baseInfectionChances.Max()}-{proportion}", baseInfectionChances.ArrString(), 10);
            DataPlots.MultiPlotInfectionStatGraph(infectedProportions, x => x.Average(), "Mean of infected proportion by node count", "NodeCount", "Mean", $"MeanNC-{baseInfectionChances.Min()}_{baseInfectionChances.Max()}-{proportion}", baseInfectionChances.ArrString(), 3);
        }
        public static void NodeCountDemo(double proportion, double? aveNodeDegree = null)
        {
            NodeCountAveInfectPlot([.. Enumerable.Range(0, 250).Select(x => 2 * x + 500)], 25, proportion, [0.0002, 0.0005, 0.001], aveNodeDegree);
        }
        public static void PlotInfectedByProportion(int nodeCount, int repeatCount, double baseInfectionChance, double[] proportions)
        {
            var infectedProportions = InfectedProportionsByProportion(nodeCount, repeatCount, proportions, baseInfectionChance);
            DataPlots.PlotInfectionStatGraph(infectedProportions, x => x.Variance(), "Variance of infected proportion by edge density", "density", "Variance", $"VarP-{nodeCount}-{baseInfectionChance}", 5);
            DataPlots.PlotInfectionStatGraph(infectedProportions, x => x.Average(), "Mean of infected proportion by edge density", "density", "Mean", $"MeanP-{nodeCount}-{baseInfectionChance}", 5);
        }
        public static void PlotInfectedByAveNodeDeg(int nodeCount, int repeatCount, double baseInfectionChance, double[] aveNodeDegrees)
        {
            var infectedProportions = InfectedProportionsByAveDeg(nodeCount, repeatCount, baseInfectionChance, aveNodeDegrees);
            DataPlots.PlotInfectionStatGraph(infectedProportions, x => x.Variance(), "Variance of infected proportion by average node degree", "average node degree", "Variance", $"VarAND-{nodeCount}-{baseInfectionChance}", 10);
            DataPlots.PlotInfectionStatGraph(infectedProportions, x => x.Average(), "Mean of infected proportion by average node degree", "average node degree", "Mean", $"MeanAND-{nodeCount}-{baseInfectionChance}", 5);
        }
        public static Dictionary<double, double[]> InfectedProportionsByProportion(int nodeCount, int repeatCount, double[] proportions, double baseInfectionChance)
        {
            List<KeyValuePair<double, List<double>>> infectedProportionsByNodeCount = [.. from proportion in proportions
                select new KeyValuePair<double,List<double>>(nodeCount,InfectedProportions(nodeCount,repeatCount,proportion,baseInfectionChance))];
            return infectedProportionsByNodeCount.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }
        public static Dictionary<double, double[]> InfectedProportionsByAveDeg(int nodeCount, int repeatCount, double baseInfectionChance, double[] aveNodeDegrees)
        {
            List<KeyValuePair<double, List<double>>> infectedProportionsByAveNodeDeg = [.. from aveNodeDegree in aveNodeDegrees
                select new KeyValuePair<double,List<double>>(aveNodeDegree,InfectedProportions(nodeCount,repeatCount,0,baseInfectionChance,aveNodeDegree))];
            return infectedProportionsByAveNodeDeg.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }
        public static void InfectionProportionParamTuning()
        {
            int nodeCount = 1000;
            int repeatCount = 25;
            double baseInfectionChance = 0.0005;
            int start = 0;
            int end = 25;
            double[] aveNodeDegrees = [.. DataUtilities.RangeDouble(start, end, 0.1)];
            PlotInfectedByAveNodeDeg(nodeCount, repeatCount, baseInfectionChance, aveNodeDegrees);
        }
        public static void PlotAveNodeDegInfectedCounts()
        {
            RunParams runParams = new(1, 1);
            var graph = new ERGraph<IncubateNode>(1000, EdgeDensity(1000, 3), 0.01, 0.0005, 1);
            graph.RunRandom(100, runParams);
            DataPlots.PlotTotalsGraph(graph.ToDataGraph(), [NodeState.Infectious], "Total Infected Node Counts over time");
            DataPlots.PlotStateChangeGraph(graph.ToDataGraph(), [NodeState.Infectious], "New Infected Node Counts over time", false);
        }
        public static void PlotInfectedBaseInfect(int numNodes, int repeatCount, double proportion, double[] baseInfectionChances, double? aveNodeDegree = null)
        {
            var infectedProportions = InfectedProportionsByBaseInfectionChance(numNodes, repeatCount, proportion, baseInfectionChances, aveNodeDegree);
            DataPlots.PlotInfectionStatGraph(infectedProportions, x => x.Variance(), "Variance of infected proportion by base infection chance", "Base Infection Chance", "Variance", $"VarBIC-{numNodes}-{proportion}", 10);
            DataPlots.PlotInfectionStatGraph(infectedProportions, x => x.Average(), "Mean of infected proportion by base infection chance", "Base Infection Chance", "Mean", $"MeanBIC-{numNodes}-{proportion}", 5);
        }
        public static Dictionary<double, double[]> InfectedProportionsByBaseInfectionChance(int numNodes, int repeatCount, double proportion, double[] baseInfectionChances, double? aveNodeDegree = null)
        {
            RunParams runParams = new(1, 1);
            var graph = new ERGraph<IncubateNode>(numNodes, aveNodeDegree != null ? EdgeDensity(numNodes, (double)aveNodeDegree) : proportion, 0.01, 0, 1);
            Dictionary<double, List<double>> proportionDict = [];
            foreach (double baseInfectionChance in baseInfectionChances)
            {
                graph.UpdateBaseInfectionChanceToAll(baseInfectionChance);
                proportionDict[baseInfectionChance] = [];
                for (var i = 0; i < repeatCount; i++)
                {
                    graph.RunRandom(100, runParams);
                    proportionDict[baseInfectionChance].Add(graph.ToDataGraph().GetTotalProportionInfected());
                }
            }
            return proportionDict.ToDictionary(x => x.Key, x => x.Value.ToArray());
        }
        public static void BaseInfectionChanceParamTuning()
        {
            int nodeCount = 1000;
            int repeatCount = 25;
            double[] baseInfectionChances = [.. Enumerable.Range(0, 401).Select(x => (double)x / 800000)];
            double aveNodeDegree = 18;
            PlotInfectedBaseInfect(nodeCount, repeatCount, 0, baseInfectionChances, aveNodeDegree);
        }
        public static void SFGraphDensityTuning(int numNodes)
        {
            foreach (int lvlConnections in Enumerable.Range(2, 1))
            {
                var graph1 = new SFGraph<IsolationNode>(numNodes, lvlConnections, 0, 0, 1);
                Console.WriteLine($"{graph1._graph.EdgeCount}:{lvlConnections}:{graph1.EdgeDensity}:{graph1.AverageDegree}");
            }
        }
    }
}