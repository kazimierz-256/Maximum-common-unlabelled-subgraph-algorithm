using System;
using System.Collections.Generic;
using MathNet.Symbolics;
using Expr = MathNet.Symbolics.Expression;

namespace MathParser
{
    public static class Parse
    {
        public static Func<int, int, double> ParseInput(string input)
        {
            var e = Expr.Symbol("edges");
            var v = Expr.Symbol("vertices");

            return (vertices, edges) =>
            {
                var symbols = new Dictionary<string, FloatingPoint>
                           {{ "vertices", vertices },
                            { "edges", edges }};

                return Evaluate.Evaluate(symbols, Infix.ParseOrUndefined(input)).RealValue;
            };

        }
    }
}
