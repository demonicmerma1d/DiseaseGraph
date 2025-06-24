using System.Numerics;
using QuikGraph;

namespace DiseaseGraph.Extensions
{
    public static class Extensions
    {
        public static T ChooseFrom<T>(this Random random, IEnumerable<T> options)
        {
            if (options == null || !options.Any())
            {
                return default;
            }
            return options.ElementAt(random.Next(options.Count()));
        }
        public static T[] Add<T>(this T[] array, T[] addArray) where T : INumber<T>
        {
            if (array.Length != addArray.Length) throw new("The inputs are of different length");
            T[] sum = array;
            for (int idx = 0; idx < Math.Max(array.Length, addArray.Length); idx++) sum[idx] += addArray[idx]; //could be smarter with comprehension, its fine
            return sum;
        }
        public static string NameOf(this object o)
        {
            return o.GetType().Name;
        }
        public static Edge<T> Mirror<T>(this Edge<T> edge)
        {
            return new Edge<T>(edge.Target, edge.Source);
        }
        public static double Variance(this double[] data)
        {
            var list = data.ToList();
            var average = list.Average();
            if (list.Count == 0) return 0;
            var variance = list.Aggregate((double)0,(total,add) => total + Math.Pow(add-average, 2))/list.Count;
            return variance;
        }
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random rng) //https://stackoverflow.com/a/1665080 
        {
            T[] elements = [.. source];
            // Note i > 0 to avoid final pointless iteration
            for (int i = elements.Length - 1; i > 0; i--)
            {
                // Swap element "i" with a random earlier element it (or itself)
                int swapIndex = rng.Next(i + 1);
                yield return elements[swapIndex];
                elements[swapIndex] = elements[i];
                // we don't actually perform the swap, we can forget about the
                // swapped element because we already returned it.
            }

            // there is one item remaining that was not returned - we return it now
            yield return elements[0]; 
        } 
    }
}