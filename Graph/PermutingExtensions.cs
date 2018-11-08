using System;
using System.Collections.Generic;
using System.Text;

namespace GraphDataStructure
{
    public static class PermutingExtensions
    {
        public static Graph Permute(this Graph g, int permutingSeed = 10)
        {
            var permuted = GraphFactory.GeneratePermuted(g, permutingSeed);
            g = permuted;
            return g;
        }
        public static Graph Permute(this Graph g, Func<int, double> valuation)
        {
            var permuted = GraphFactory.GeneratePermuted(g, valuation);
            g = permuted;
            return g;
        }
    }
}
