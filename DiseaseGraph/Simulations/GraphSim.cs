using System.Diagnostics;
using System.Reflection;
using DiseaseGraph.DataProcessing;
using DiseaseGraph.Extensions;
using DiseaseGraph.Graph;

namespace DiseaseGraph.Simulations
{
    public static class RunGraphSim //literally anything you fucking run, just slap it in here as a function to be easy to come back to pls
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
        public static List<DataGraph> RunForRandomSeed<TNode,TGraph>(this TGraph graph,double maxTime,int numRuns,double infectionTime,double incubationTime = 0) 
            where TGraph : GraphBase<TNode> where TNode : Node,new()
        {
            RunParams runParams = new(infectionTime,incubationTime);
            List<DataGraph> graphData = [];
            for (int i=0; i<numRuns; i++)
            {
                Console.WriteLine(graph.RunRandom(maxTime,runParams));
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
        public static double RunSimForRandomSeed<TNode,TGraph>(this TGraph graph,double maxTime,int numRuns,RunParams runParams)
            where TGraph : GraphBase<TNode> where TNode : Node, new()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < numRuns; i++)
            {
                graph.RunRandom(maxTime,runParams);
                var dataGraph = graph.ToDataGraph();
                DataPlots.PlotTotalsGraph(dataGraph, Enum.GetValues<NodeState>(), "Node Counts by state over time.");
                DataPlots.PlotStateChangeGraph(dataGraph,Enum.GetValues<NodeState>(), "Change in Node Counts by state over time",true);
            }
            return stopwatch.Elapsed.TotalSeconds;
        }
    }
} 