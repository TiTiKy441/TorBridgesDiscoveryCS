namespace TorBridgesDiscoveryCS
{
    internal static class Utils
    {

        public readonly static Random Random = new(Convert.ToInt32(DateTime.Now.ToString("FFFFFFF")));

        public static void Shuffle<T>(this Random rng, List<T> array)
        {
            int n = array.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[k], array[n]) = (array[n], array[k]);
            }
        }
    }
}
