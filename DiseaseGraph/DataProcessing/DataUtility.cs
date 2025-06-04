

namespace DiseaseGraph.DataProcessing
{
    public static class DataUtilities
    {
        public static string ValidFileName(string path, string fileName,string extension)
        {
            string name = fileName;
            int i = 0;
            while (Path.Exists(Path.Combine(path,fileName+extension)))
            { 
                fileName = $"{i++}-{name}";
            }
            return Path.Combine(path,fileName+extension);
        }
        public static Dictionary<double,double[]> SmoothData(Dictionary<double,double[]> data,double paramRangeFromMidpoint) // rolling average of data for smoothing
        {
            ArgumentOutOfRangeException.ThrowIfNegative(paramRangeFromMidpoint);
            double[] smoothFunc(double time,double timeRangeFromMidpoint)
            {
                List<KeyValuePair<double,double[]>> localPoints = [..data.Where(kvp => Math.Abs(kvp.Key - time) <= timeRangeFromMidpoint)];
                return [.. Enumerable.Range(0,localPoints[0].Value.Length).Select(i => localPoints.Select(a => a.Value.Skip(i).First()).Average())];
            }
            Dictionary<double, double[]> smoothedData = [];
            foreach (double time in data.Keys)
            {
                smoothedData.Add(time, smoothFunc(time, paramRangeFromMidpoint));
            }
            return smoothedData;
        }
    }
} 