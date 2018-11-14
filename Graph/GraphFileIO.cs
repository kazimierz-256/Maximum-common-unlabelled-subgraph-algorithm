using GraphDataStructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Application
{
    public static class GraphFileIO
    {
        public static bool Read(string uri, out Graph g)
        {
            g = null;
            string[] lines = File.ReadAllLines(uri);
            var boolList = new List<bool>();

            foreach (var line in lines)
            {
                foreach (var character in line)
                {
                    if (char.IsDigit(character))
                    {
                        var value = char.GetNumericValue(character);
                        if (value == 1d)
                        {
                            boolList.Add(true);
                        }
                        else if (value == 0d)
                        {
                            boolList.Add(false);
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            var v = (int)Math.Sqrt(boolList.Count);

            if (v * v == boolList.Count)
            {
                var vertices = new HashSet<int>(Enumerable.Range(0, v));
                var neighbours = new Dictionary<int, HashSet<int>>();

                for (int i = 0; i < v; i++)
                {
                    for (int j = 0; j < v; j++)
                    {
                        if (boolList[i * v + j])
                        {
                            if (!boolList[j * v + i])
                            {
                                throw new Exception("The graph is not undirected.");
                            }
                            if (!neighbours.ContainsKey(i))
                            {
                                neighbours.Add(i, new HashSet<int>());
                            }

                            neighbours[i].Add(j);
                        }
                    }
                }

                g = new Graph(neighbours, vertices, neighbours.Sum(neighbourhood => neighbourhood.Value.Count) / 2);
            }
            else
            {
                return false;
            }

            return true;
        }

        public static void Write(this Graph g, string uri)
        {
            var vertices = g.Vertices.ToArray();

            var lines = new string[vertices.Length];

            var builder = new StringBuilder();
            for (int i = 0; i < vertices.Length; i++)
            {
                for (int j = 0; j < vertices.Length; j++)
                {
                    if (g.AreVerticesConnected(vertices[i], vertices[j]))
                    {
                        builder.Append("1");
                    }
                    else
                    {
                        builder.Append("0");
                    }
                    if (j < vertices.Length - 1)
                    {
                        builder.Append(",");
                    }
                }

                lines[i] = builder.ToString();
                builder.Clear();
            }

            File.WriteAllLines(uri, lines);
        }
    }
}
