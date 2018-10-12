using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphDataStructure
{
    public class GraphFactory
    {
        public static Graph GenerateRandom(int n, double density, int generatingSeed) => throw new NotImplementedException();
        public static Graph GeneratePermuted(Graph g, int permutingSeed)
        {

            // permute the vertices and make another graph
            var ascendingIntegers = Enumerable.Range(0, n).ToArray();
            Permute(permutingSeed, ref ascendingIntegers);

        }

        private void Permute(int seed, ref int[] vertices)
        {
            var random = new Random(seed);
            var randomValues = Enumerable.Range(0, vertices.Length).Select(i => random.Next()).ToArray();
            //new int[vertices.Length];
            //for (int i = 0; i < vertices.Length; i++)
            //{
            //    randomValues[i] = random.Next();
            //}

            Array.Sort(randomValues, vertices);
        }
    }
}
