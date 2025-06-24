using DiseaseGraph.Graph;
using DiseaseGraph.Extensions;
using ScottPlot;
using System;
using ScottPlot.AxisPanels;

namespace DiseaseGraph.DataProcessing
{
    public static class DataPlots
    {
        private static string SavePath 
        {
            get
            {
                string currentPath = Directory.GetCurrentDirectory();
                var directory = Directory.CreateDirectory(Path.Join(currentPath,"Plots"));
                return directory.FullName;
            }
        }
        private static string LegendName(NodeState plotState)
            => plotState switch
            {
                NodeState.Susceptible => "Susceptible",
                NodeState.Exposed => "Exposed",
                NodeState.Infectious => "Infectious",
                NodeState.Removed => "Removed",
                NodeState.Dead => "Dead",
                _ => "Unknown state"
            };
        public static void PlotData(NodeState[] plotNodeStates, string saveName,string title = "",string xAxis = "",string yAxis = "",params Dictionary<double, double[]>[] AllData)
        {
            Plot plot = new();
            double maxTime = 0;
            foreach (var data in AllData)
            {
                AddToPlot(ref plot, plotNodeStates, data,ref maxTime);
            }
            SetAxes(ref plot,xAxis:xAxis,yAxis:yAxis);
            plot.Axes.SetLimitsX(0, maxTime);
            plot.Save(DataUtilities.ValidFileName(SavePath, saveName, ".png"), 800, 500);
            plot.ShowLegend(Alignment.MiddleRight);
        }
        private static void AddToPlot(ref Plot plot,NodeState[] plotNodeStates, Dictionary<double, double[]> data, ref double maxTime)
        {
            var listData = data.OrderBy(x => x.Key).ToList();
            var times = listData.Select(x => x.Key).ToList();
            maxTime = Math.Max(times.Max(),maxTime);
            foreach (var nodeState in plotNodeStates)
            {
                var popData = listData.Select(x => x.Value[(int)nodeState]).ToList();
                var plt = plot.Add.ScatterLine(times, popData);
                plt.LegendText = LegendName(nodeState);
            }
        }
        public static void PlotDataArr(List<Dictionary<double, double[]>> AllData,NodeState[] plotNodeStates, string saveName,string title = "",string xAxis = "",string yAxis = "")
        {
            Plot plot = new();
            double maxTime = 0;
            foreach (var data in AllData)
            {
                AddToPlot(ref plot, plotNodeStates, data,ref maxTime);
            }
            SetAxes(ref plot,title,xAxis,yAxis);
            plot.Axes.SetLimitsX(0, maxTime);
            plot.Save(DataUtilities.ValidFileName(SavePath, saveName, ".png"), 800, 500);
            plot.ShowLegend(Alignment.MiddleRight);
        }
        public static void PlotTotalsGraph(DataGraph graph,NodeState[] plotNodeStates,string title)
        {
            PlotData(plotNodeStates,graph.FileName("totals",true),title,"Time","Node Count",DataProcessor.TotalStateMembers(graph));
        }
        public static void PlotStateChangeGraph(DataGraph graph,NodeState[] plotNodeStates,string title, bool all)
        {
            PlotData(plotNodeStates, graph.FileName(all ? "net" : "new",true),
            title,"Time","Node Count",DataProcessor.GraphStateChangesByTime(graph, all));
        }
        public static void MultiPlotStateChangeGraph(List<DataGraph> graphs,NodeState[] plotNodeStates,string title, bool all)
        {
            List<Dictionary<double, double[]>> allGraphData = [.. from graph in graphs select DataUtilities.SmoothData(DataProcessor.GraphStateChangesByTime(graph, all),0.2)];
            PlotDataArr(allGraphData, plotNodeStates, graphs[0].FileName((all ? "net" : "new") + $"{graphs.Count}", true), title, "Time", "Node Count");
        }
        public static void DegreeDistributionGraph(DataGraph graph)
        {
            Dictionary<int, int> degDist = [];
            foreach (var vertex in graph.NodeData)
            {
                int degree = graph.Graph.OutEdges(vertex).Count();
                if (!degDist.ContainsKey(degree)) degDist.Add(degree,0);
                degDist[degree] += 1;
            }
            var plot = new Plot();
            plot.Add.Bars((Bar[])[.. from deg in degDist.Keys select new Bar(){Position = deg,Value = degDist[deg]}]);
            SetAxes(ref plot, $"Node degree distribution for {graph.NodeData.Count} nodes", "Degree of Nodes", "Node Count by Degree");
            plot.Save(DataUtilities.ValidFileName(SavePath, graph.FileName("DegDist"), ".png"), 800, 500);
        }
        private static void SetAxes(ref Plot plot,string title = "",string xAxis = "", string yAxis = "")
        {
            plot.Axes.Title.Label.Text = title;
            plot.Axes.Bottom.Label.Text = xAxis;
            plot.Axes.Left.Label.Text = yAxis;
        }
        public static void PlotInfectionStatGraph(Dictionary<double,double[]> infectedProportions,Func<double[],double> aggregate,string title,string xVar,string yVar,string saveNameData,int size)
        {
            var plot = new Plot();
            
            Dictionary<double,double> infectedDataProcessed = infectedProportions.ToDictionary(x => x.Key,x => aggregate(x.Value));
            var plt = plot.Add.ScatterLine(infectedProportions.Select(x => x.Key).ToList(), [.. infectedProportions.Select(x => aggregate(x.Value))]);
            plt.LegendText = "normal";
            var smoothedData = DataUtilities.SmoothDataAverage(infectedDataProcessed,size).OrderBy(x => x.Key);
            var smhplt = plot.Add.ScatterLine(smoothedData.Select(x => x.Key).ToList(), [.. smoothedData.Select(x => x.Value)]);
            smhplt.LegendText = "smooth";
            SetAxes(ref plot, title,xVar,yVar);
            plot.ShowLegend();
            plot.Save(DataUtilities.ValidFileName(SavePath,saveNameData+$"-{DateTime.Now:yyyyMMddHHmmss}", ".png"), 800, 500);
        }
        public static void PlotInfectionStatGraphTest(Dictionary<double,double[]> infectedProportions,Func<double[],double> aggregate,string title,string xVar,string yVar,string saveNameData,int size)
        {
            var plot = new Plot();
            Dictionary<double,double> infectedDataProcessed = infectedProportions.ToDictionary(x => x.Key,x => aggregate(x.Value));
            var plt = plot.Add.ScatterLine(infectedProportions.Select(x => x.Key).ToList(), [.. infectedDataProcessed.OrderBy(x => x.Key).Select(x => x.Value)]);
            plt.LegendText = "normal";
            var smoothedData = DataUtilities.SmoothDataAverage(infectedDataProcessed,size).OrderBy(x => x.Key);
            var smhplt = plot.Add.ScatterLine(smoothedData.Select(x => x.Key).ToList(), [.. smoothedData.Select(x => x.Value)]);
            smhplt.LegendText = "smooth";
            SetAxes(ref plot, title,xVar,yVar);
            plot.ShowLegend();
            plot.Save(DataUtilities.ValidFileName(SavePath,saveNameData+$"-{DateTime.Now:yyyyMMddHHmmss}", ".png"), 800, 500);
        }
    }
}