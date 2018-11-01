using System;
using System.Collections.Generic;
using System.Text;

namespace GraphDataStructure
{
    public static class ArgMaxExtensions
    {
        public static T ArgMax<T, V>(this IEnumerable<T> enumerable, Func<T, V> valuation)
            where V : IComparable
        {
            var enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();
            var bestObject = enumerator.Current;
            var bestValuation = valuation(bestObject);
            while (enumerator.MoveNext())
            {
                var localValuation = valuation(enumerator.Current);
                if (localValuation.CompareTo(bestValuation) > 0)
                {
                    bestValuation = localValuation;
                    bestObject = enumerator.Current;
                }
            }

            return bestObject;
        }

        public static T ArgMax<T, V>(this IEnumerable<T> enumerable, IEnumerable<V> valuation)
            where V : IComparable
        {
            var TEnumerator = enumerable.GetEnumerator();
            var VEnumerator = valuation.GetEnumerator();
            TEnumerator.MoveNext();
            VEnumerator.MoveNext();
            var bestObject = TEnumerator.Current;
            var bestValuation = VEnumerator.Current;
            while (TEnumerator.MoveNext() && VEnumerator.MoveNext())
            {
                if (VEnumerator.Current.CompareTo(bestValuation) > 0)
                {
                    bestValuation = VEnumerator.Current;
                    bestObject = TEnumerator.Current;
                }
            }

            return bestObject;
        }
    }
}
