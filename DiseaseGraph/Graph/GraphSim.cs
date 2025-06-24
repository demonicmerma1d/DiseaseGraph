using System.Reflection;
using DiseaseGraph.DataProcessing;
using DiseaseGraph.Extensions;
using FastDeepCloner;
using QuikGraph;
using System.Web;

namespace DiseaseGraph.Graph
{
    public static class RunGraphSim //literally anything you fucking run, just slap it in here as a function to be easy to come back to pls
    {
        public static List<DataGraph> RunForSeeds<TNode,TGraph>(this TGraph graph,double maxTime, List<int> seedInfections, int numRuns,double infectionTime,double incubationTime = 0)
            where TGraph : GraphBase<TNode> where TNode : Node,new()
        {
            List<DataGraph> graphData = [];
            for (int i=0; i<numRuns; i++)
            {
                Console.WriteLine(graph.Run(maxTime, seedInfections, infectionTime, incubationTime));
                graphData.Add(graph.ToDataGraph());
            }
            return graphData;
        }
        public static List<DataGraph> RunForRandomSeed<TNode,TGraph>(this TGraph graph,double maxTime,int numRuns,double infectionTime,double incubationTime = 0) 
            where TGraph : GraphBase<TNode> where TNode : Node,new()
        {
                List<DataGraph> graphData = [];
                for (int i=0; i<numRuns; i++)
                {
                    Console.WriteLine(graph.Run(maxTime, [graph.Random.Next(graph.NodeData.Count)], infectionTime, incubationTime));
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
        public static void GraphParamTestNodeCount(int numNodes)
        {
            var graph = new ERGraph<IncubateNode>(numNodes,EdgeDensity(numNodes,10),0.1,0.01,1);
            
        }
        public static void GraphParamTestNodeCountConst(int repeatCount,int[] numNodes,double proportion,double baseInfectionChance, double? aveNodeDegree = null)
        {
            foreach (var nodeCount in numNodes)
            {
                var graph = new ERGraph<IncubateNode>(nodeCount, aveNodeDegree != null ? EdgeDensity(nodeCount,(double)aveNodeDegree): proportion, 0.1, baseInfectionChance, 1,0);
                for (int i=0; i < repeatCount; i++)
                {
                    graph.Run(100, [graph.Random.Next(graph.NodeData.Count)], 1, 1);
                    var dataGraph = graph.ToDataGraph();
                    DataPlots.PlotTotalsGraph(dataGraph,Enum.GetValues<NodeState>(),$"Node state totals plot on {graph.NodeData.Count} nodes");
                    //DataPlots.DegreeDistributionGraph(dataGraph);
                }             
            }
        }
        public static double GetTotalProportionInfected(this DataGraph graph)
        {
            return 1 - DataProcessor.TotalStateMembers(graph).MaxBy(state => state.Key).Value[(int)NodeState.Susceptible]/graph.NodeData.Count;
        }
        public static List<double> InfectedProportions(int numNodes, int repeatCount, double proportion,double baseInfectionChance, double? aveNodeDegree = null)
        {
            var graph = new ERGraph<IncubateNode>(numNodes, aveNodeDegree != null ? EdgeDensity(numNodes, (double)aveNodeDegree) : proportion, 0.1, baseInfectionChance, 1);
            List<double> proportions = [];
            for (var i=0; i < repeatCount; i++)
            {
                graph.Run(100,[graph.Random.Next(graph.NodeData.Count)],1,1);
                proportions.Add(graph.ToDataGraph().GetTotalProportionInfected());
            }
            return proportions;
        }
        public static Dictionary<double,double[]> InfectedProportionsByBaseInfectionChance(int numNodes, int repeatCount, double proportion,double[] baseInfectionChances, double? aveNodeDegree = null)
        {
            var graph = new ERGraph<IncubateNode>(numNodes, aveNodeDegree != null ? EdgeDensity(numNodes, (double)aveNodeDegree) : proportion, 0.1, 0, 1);
            Dictionary<double, List<double>> proportionDict = [];
            foreach (double baseInfectionChance in baseInfectionChances)
            {
                graph.UpdateBaseInfectionChanceToAll(baseInfectionChance);
                proportionDict[baseInfectionChance] = [];
                for (var i=0; i < repeatCount; i++)
                {
                    graph.Run(100,[graph.Random.Next(graph.NodeData.Count)],1,1);
                    proportionDict[baseInfectionChance].Add(graph.ToDataGraph().GetTotalProportionInfected());
                }
            }
            return proportionDict.ToDictionary(x => x.Key,x => x.Value.ToArray());
        }
        public static Dictionary<double,double[]> InfectedProportionsByNodeCount(int[] nodeCounts,int repeatCount,double proportion,double baseInfectionChance,double? aveNodeDegree = null)
        {
            List<KeyValuePair<double, List<double>>> dieoutProportionsByNodeCount = [.. from nodeCount in nodeCounts
                select new KeyValuePair<double,List<double>>(nodeCount,InfectedProportions(nodeCount,repeatCount,proportion,baseInfectionChance,aveNodeDegree))];
            return dieoutProportionsByNodeCount.ToDictionary(x => x.Key,x=> x.Value.ToArray());
        }
        public static Dictionary<double,double[]> InfectedProportionsByProportion(int nodeCount,int repeatCount,double[] proportions,double baseInfectionChance)
        {
            List<KeyValuePair<double, List<double>>> dieoutProportionsByNodeCount = [.. from proportion in proportions
                select new KeyValuePair<double,List<double>>(nodeCount,InfectedProportions(nodeCount,repeatCount,proportion,baseInfectionChance))];
            return dieoutProportionsByNodeCount.ToDictionary(x => x.Key,x=> x.Value.ToArray());
        }
        public static Dictionary<double,double[]> InfectedProportionsByAveDeg(int nodeCount,int repeatCount,double baseInfectionChance,double[] aveNodeDegrees)
        {
            List<KeyValuePair<double, List<double>>> dieoutProportionsByNodeCount = [.. from aveNodeDegree in aveNodeDegrees
                select new KeyValuePair<double,List<double>>(nodeCount,InfectedProportions(nodeCount,repeatCount,0,baseInfectionChance,aveNodeDegree))];
            return dieoutProportionsByNodeCount.ToDictionary(x => x.Key,x=> x.Value.ToArray());
        }
        public static void PlotInfectedBaseInfect(int numNodes, int repeatCount, double proportion,double[] baseInfectionChances, double? aveNodeDegree = null)
        {
            var dieoutProportions = InfectedProportionsByBaseInfectionChance(numNodes, repeatCount, proportion, baseInfectionChances, aveNodeDegree);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Variance(), "Variance of infected proportion by base infection chance", "BaseInfectionChance","Variance", $"VarBIC-{numNodes}-{proportion}",10);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Average(), "Mean of infected proportion by base infection chance", "BaseInfectionChance","Mean", $"MeanBIC-{numNodes}-{proportion}",10);
        }
        public static void PlotInfectedNodeCount(int[] nodeCounts,int repeatCount,double proportion,double baseInfectionChance,double? aveNodeDegree = null)
        {
            var dieoutProportions = InfectedProportionsByNodeCount(nodeCounts, repeatCount, proportion, baseInfectionChance, aveNodeDegree);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Variance(), "Variance of infected proportion by node count", "NodeCount", "Variance", $"VarNC-{baseInfectionChance}-{proportion}",3);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Average(), "Mean of infected proportion by node count", "NodeCount", "Mean", $"MeanNC-{baseInfectionChance}-{proportion}",3);
        }
        public static void PlotInfectedByProportion(int nodeCount,int repeatCount,double baseInfectionChance,double[] proportions)
        {
            var dieoutProportions = InfectedProportionsByProportion(nodeCount, repeatCount, proportions, baseInfectionChance);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Variance(), "Variance of infected proportion by edge density", "density", "Variance", $"VarP-{nodeCount}-{baseInfectionChance}",5);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Average(), "Mean of infected proportion by edge density", "density", "Mean", $"MeanP-{nodeCount}-{baseInfectionChance}",5);
        }
        public static void PlotInfectedByAveNodeDeg(int nodeCount,int repeatCount,double baseInfectionChance,double[] aveNodeDegrees)
        {
            var dieoutProportions = InfectedProportionsByAveDeg(nodeCount, repeatCount,baseInfectionChance,aveNodeDegrees);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Variance(), "Variance of infected proportion by average node degree", "average node degree", "Variance", $"VarAND-{nodeCount}-{baseInfectionChance}",5);
            DataPlots.PlotInfectionStatGraph(dieoutProportions, x => x.Average(), "Mean of infected proportion by average node  degree", "average node degree", "Mean", $"MeanAND-{nodeCount}-{baseInfectionChance}",5);
        }
        
    }
} 