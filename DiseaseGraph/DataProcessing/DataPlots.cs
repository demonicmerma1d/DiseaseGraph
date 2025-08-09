using DiseaseGraph.Graph;
using DiseaseGraph.Extensions;
using ScottPlot;
using MoreLinq;
namespace DiseaseGraph.DataProcessing
{
    [Flags]
    public enum PlotOptions
    {
        None = 0b_00,
        Raw = 0b_01,
        Smooth = 0b_10,
        Both = Raw | Smooth
    } 
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
        private static void SetAxes(this Plot plot,string title = "",string xAxis = "", string yAxis = "")
        {
            plot.Axes.Title.Label.Text = title;
            plot.Axes.Bottom.Label.Text = xAxis;
            plot.Axes.Left.Label.Text = yAxis;
        }
        private static void Scatter(this Plot plot,List<double> xData,List<double?> yData,string legendLabel)
        {
            List<double> xDataFiltered = [.. Enumerable.Range(0, xData.Count).Where(i => yData[i] != null).Select(i => xData[i])];
            List<double> yDataFiltered = [.. Enumerable.Range(0, yData.Count).Where(i => yData[i] != null).Select(i => (double)yData[i])];
            if (xDataFiltered.Count != yDataFiltered.Count) throw new Exception("The length of xData and yData must match");
            var plt = plot.Add.ScatterLine(xDataFiltered, yDataFiltered);
            plt.LegendText = legendLabel;
        }
        private static void Scatter(this Plot plot,List<double> xData,List<double?> yData)
        {
            List<double> xDataFiltered = [.. Enumerable.Range(0, xData.Count).Where(i => yData[i] != null).Select(i => xData[i])];
            List<double> yDataFiltered = [.. Enumerable.Range(0, yData.Count).Where(i => yData[i] != null).Select(i => (double)yData[i])];
            if (xDataFiltered.Count != yDataFiltered.Count) throw new Exception("The length of xData and yData must match");
            var plt = plot.Add.ScatterLine(xDataFiltered, yDataFiltered);
        }
        public static void Plot(string[] legendLabels,string saveName,string title,string xLabel,string yLabel,List<double> xData, List<List<double?>> yDataLists, bool showLegend = true)
        {
            if (yDataLists.Count == 0) throw new Exception("At least one set of yData must be provided");
            if (showLegend && (yDataLists.Count != legendLabels.Length)) throw new Exception("The count of data and legend labels does not match");
            Plot plot = new();
            plot.SetAxes(title, xLabel, yLabel);
            if (showLegend) for (int i = 0; i < yDataLists.Count; i++) plot.Scatter(xData, yDataLists[i], legendLabels[i]);
            else for (int i = 0; i < yDataLists.Count; i++) plot.Scatter(xData, yDataLists[i]);
            plot.Axes.SetLimitsX(xData.Min(),xData.Max());
            if (showLegend) plot.ShowLegend(Alignment.MiddleRight);
            plot.Save(DataUtilities.ValidFileName(SavePath, saveName, ".png"), 800, 500);
        }
        public static void PlotTotalsGraph(DataGraph graph,NodeState[] nodeStates,string title)
        {
            var yData = DataProcessor.TotalStateMembers(graph).SplitNodeStatesToLists(nodeStates,out List<double> xData);
            Plot([.. nodeStates.Select(LegendName)], graph.FileName("totals",true), title,"Time","NodeCount",xData,[..yData]);
        }
        public static void PlotStateChangeGraph(DataGraph graph,NodeState[] nodeStates,string title, bool all,bool showLegend=true)
        {
            var yData = DataProcessor.GraphStateChangesByTime(graph,all).SplitNodeStatesToLists(nodeStates,out List<double> xData);
            Plot([.. nodeStates.Select(LegendName)], graph.FileName(all ? "net" : "new",true), title,"Time","NodeCount",xData,[..yData],showLegend);
        }
        public static void MultiPlotChangesForState(List<DataGraph> graphList,string[] legendLabels,NodeState nodeState,string title, bool all,bool showLegend=true)
        {
            PlotAggregates(legendLabels, $"StateChangePlot-{(all ? "net" : "new")}", title, "Time", "NodeCount",
             x => x[(int)nodeState], [.. from graph in graphList select DataProcessor.GraphStateChangesByTime(graph,all)],PlotOptions.Smooth,showLegend,10);
        }
        public static void MultiPlotTotalsForState(List<DataGraph> graphList,string[] legendLabels,NodeState nodeState,string title,bool showLegend=true)
        {
            PlotAggregates(legendLabels, $"StateChangePlot-Totals", title, "Time", "NodeCount",
             x => x[(int)nodeState], [.. from graph in graphList select DataProcessor.TotalStateMembers(graph)],PlotOptions.Raw,showLegend);
        }
        public static void PlotAggregates(string[] legendLabels,string saveName,string title,string xLabel,string yLabel,Func<double[],double> aggregate,List<Dictionary<double,double[]>> data,PlotOptions plotOptions,bool showLegend = true,int size = 0)
        {
            if (plotOptions == PlotOptions.None) return;
            List<Dictionary<double, double>> aggData = [];
            if ((plotOptions & PlotOptions.Raw) != 0) //if including raw plots
            {
                aggData.AddRange(data.Select(dataDict => dataDict.ToDictionary(x => x.Key,x => aggregate(x.Value))));
            }
            if ((plotOptions & PlotOptions.Smooth) != 0) //if including smoothed plots
            {
                aggData.AddRange(data.Select(dataDict => DataUtilities.SmoothDataAverage(dataDict.ToDictionary(x => x.Key,x => aggregate(x.Value)),size)));
            }
            IEnumerable<double> allKeys = data.Select(x=> x.Keys).Aggregate(new HashSet<double>(),(current,next) => [.. current.Union(next)]); //get all the xvals
            Dictionary<double, double?[]> processedData = [];
            foreach (var key in allKeys) //calc and format all the yvals for each element in the list into processedData
            {
                double?[] val = [.. from dict in aggData select dict.TryGetValue(key, out double dictVal) ? (double?)dictVal : null];
                processedData[key] = val;
            }
            List<double> orderedKeys = [.. allKeys.OrderBy(x => x)];
            List<List<double?>> orderedDataLists = [.. Enumerable.Range(0,aggData.Count).Select(i => orderedKeys.Select(key => processedData[key][i]).ToList())];
            Plot((plotOptions == PlotOptions.Both) ? [.. Enumerable.Concat(legendLabels, [.. from label in legendLabels select $"{label}-Smooth"])] : legendLabels, saveName, title, xLabel, yLabel, orderedKeys, orderedDataLists,showLegend);
        }
        public static void PlotInfectionStatGraph(Dictionary<double,double[]> infectedProportions,Func<double[],double> aggregate,string title,string xVar,string yVar,string saveNameData,int size=10)
        {
            PlotAggregates(["Total Infected"], saveNameData, title, xVar, yVar, aggregate, [infectedProportions], PlotOptions.Both, size:size);
        }
        public static void MultiPlotInfectionStatGraph(List<Dictionary<double, double[]>> infectedProportionsList, Func<double[], double> aggregate, string title, string xVar, string yVar, string saveNameData, string[] legendLabels,int size=10)
        {
            PlotAggregates(legendLabels, saveNameData, title, xVar, yVar, aggregate, infectedProportionsList, PlotOptions.Both, size:size);
        }
        public static void DegreeDistributionGraph(DataGraph graph) //non scatter plot, needs to be done manually
        {
            Dictionary<int, int> degDist = [];
            foreach (var vertex in graph.NodeData)
            {
                int degree = graph.Graph.OutEdges(vertex).Count();
                degDist.TryAdd(degree, 0);
                degDist[degree] += 1;
            }
            var plot = new Plot();
            plot.Add.Bars((Bar[])[.. from deg in degDist.Keys select new Bar(){Position = deg,Value = degDist[deg]}]);
            plot.SetAxes($"Node degree distribution for {graph.NodeData.Count} nodes", "Degree of Nodes", "Node Count by Degree");
            plot.Save(DataUtilities.ValidFileName(SavePath, graph.FileName("DegDist"), ".png"), 800, 500);
        }
    }
}