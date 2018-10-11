using System;
using System.Collections.Generic;

namespace SubgraphIsomorphismExactAlgorithm
{
    public static class Primes
    {
        private static List<int> primes = new List<int>();
        public static int GetNthPrime(int n)
        {
            while (primes.Count <= n)
            {
                if (!staticGenerator.GetEnumerator().MoveNext()) throw new Exception("Error while generating next prime number");
                primes.Add(staticGenerator.GetEnumerator().Current);
            }

            return primes[n];
        }
        private static IEnumerable<int> staticGenerator = Enumerate();
        public static IEnumerable<int> Enumerate()
        {
            var primes = new List<int>();
            var primesSquared = new List<int>();
            int considering = 3;
            yield return 2;
            while (true)
            {
                // perform the increase and add to prime numbers
                yield return considering;
                primes.Add(considering);
                primesSquared.Add(considering * considering);

                // compute increase
                var shouldIncrease = true;
                while (shouldIncrease)
                {
                    considering += 2;
                    shouldIncrease = false;

                    for (int i = 0; i < primes.Count && primesSquared[i] <= considering; i++)
                    {
                        if ((considering) % primes[i] == 0)
                        {
                            shouldIncrease = true;
                            break;
                        }
                    }
                }
            }

        }
    }
}
