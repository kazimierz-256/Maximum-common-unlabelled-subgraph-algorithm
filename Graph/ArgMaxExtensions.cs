using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphDataStructure
{
    public static class ArgMaxExtensions
    {
        public static List<T> ArgMaxMultiple<T, V>(this IEnumerable<T> enumerable, Func<T, V> valuation)
            where V : IComparable
        {
            var enumerator = enumerable.GetEnumerator();
            enumerator.MoveNext();
            var bestObjects = new List<T>() { enumerator.Current };
            var bestValuation = valuation(bestObjects[0]);
            while (enumerator.MoveNext())
            {
                var localValuation = valuation(enumerator.Current);
                var comparison = localValuation.CompareTo(bestValuation);
                if (comparison > 0)
                {
                    bestValuation = localValuation;
                    bestObjects.Clear();
                    bestObjects.Add(enumerator.Current);
                }
                else if (comparison == 0)
                {
                    bestObjects.Add(enumerator.Current);
                }
            }

            return bestObjects;
        }

        public static IEnumerable<T> ArgMaxMultiple<T, V>(this IEnumerable<T> enumerable, params Func<T, V>[] valuations)
            where V : IComparable
        {
            var survivors = enumerable.ArgMaxMultiple(valuations[0]);
            for (int i = 1; i < valuations.Length; i += 1)
                survivors = survivors.ArgMaxMultiple(valuations[i]);
            return survivors;
        }

        public static T ArgMax<T, V>(this IEnumerable<T> enumerable, params Func<T, V>[] valuations)
            where V : IComparable
        {
            if (valuations.Length == 1)
            {
                return enumerable.ArgMax(valuations[0]);
            }
            else
            {
                var survivors = enumerable.ArgMaxMultiple(valuations[0]);
                for (int i = 1; i < valuations.Length - 1; i += 1)
                    survivors = survivors.ArgMaxMultiple(valuations[i]);
                return survivors.ArgMax(valuations[valuations.Length - 1]);
            }
        }

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
