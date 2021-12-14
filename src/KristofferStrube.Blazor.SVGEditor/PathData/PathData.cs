﻿using System.Globalization;
using System.Text.RegularExpressions;

namespace KristofferStrube.Blazor.SVGEditor
{
    public static class PathData
    {
        public static List<IPathInstruction> Parse(string input)
        {
            var strippedInput = input.Replace(",", " ").Replace("-", " -");
            List<string> instructions = new() { "M", "m", "Z", "z", "L", "l", "H", "h", "V", "v", "C", "c", "S", "s", "Q", "q", "T", "t", "A", "a" };
            var standardizedInput = instructions.Aggregate(strippedInput, (accu, curr) => accu.Replace(curr, $",{curr} ")).TrimStart(' ');
            // This part makes numbers that have implicit 0 in front of a dot into it's real number like .142 into 0.142. Related to https://github.com/KristofferStrube/Blazor.SVGEditor/issues/1
            var zeroFixed = Regex.Replace(standardizedInput, @"(?:0|.[1-9][0-9]*)(:\.[0-9]{1,2})?", " 0$0");
            // This part looks for any number of spaces and replaces them with a single space.
            var removesDoubleSpaces = Regex.Replace(zeroFixed, @"\s+", " ");
            var splitInstructionSequences = removesDoubleSpaces.Split(",");
            var list = Enumerable.Range(1, splitInstructionSequences.Length - 1).Aggregate<int, List<IPathInstruction>>(
                new List<IPathInstruction>(),
                (list, curr) =>
                    {
                        var previous = curr == 1 ? null : list.Last();
                        var seq = splitInstructionSequences[curr].TrimEnd(' ');
                        var instruction = seq.Substring(0, 1);
                        if (curr == 1 && instruction is not ("M" or "m"))
                            throw new ArgumentException($"The first sequence is not a move (\"m\" or \"M\") in {strippedInput}");
                        var parameters = new List<double>();
                        if (seq != "Z" && seq != "z")
                        {
                            parameters = seq.Substring(2, seq.Length - 2).Split(" ").Select(p => p.ParseAsDouble()).ToList();
                        }
                        switch (instruction)
                        {
                            case "L" or "l":
                                if (parameters.Count % 2 != 0 && parameters.Count >= 2)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count / 2).ToList().ForEach(i =>
                                {
                                    list.Add(new LineInstruction(parameters[i * 2], parameters[i * 2 + 1], instruction == "l", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "M" or "m":
                                if (parameters.Count % 2 != 0 && parameters.Count >= 2)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count / 2).ToList().ForEach(i =>
                                {
                                    list.Add(new MoveInstruction(parameters[i * 2], parameters[i * 2 + 1], instruction == "m", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "H" or "h":
                                if (parameters.Count == 0)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count).ToList().ForEach(i =>
                                {
                                    list.Add(new HorizontalLineInstruction(parameters[i], instruction == "h", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "V" or "v":
                                if (parameters.Count == 0)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count).ToList().ForEach(i =>
                                {
                                    list.Add(new VerticalLineInstruction(parameters[i], instruction == "v", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "Z" or "z":
                                if (parameters.Count != 0)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                list.Add(new ClosePathInstruction(instruction == "z", previous));
                                previous = list.Last();
                                break;
                            case "C" or "c":
                                if (parameters.Count % 6 != 0 && parameters.Count >= 6)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count / 6).ToList().ForEach(i =>
                                {
                                    list.Add(new CubicBézierCurveInstruction(parameters[i * 6], parameters[i * 6 + 1], parameters[i * 6 + 2], parameters[i * 6 + 3], parameters[i * 6 + 4], parameters[i * 6 + 5], instruction == "c", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "S" or "s":
                                if (parameters.Count % 4 != 0 && parameters.Count >= 4)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count / 4).ToList().ForEach(i =>
                                {
                                    list.Add(new ShorthandCubicBézierCurveInstruction(parameters[i * 4], parameters[i * 4 + 1], parameters[i * 4 + 2], parameters[i * 4 + 3], instruction == "s", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "Q" or "q":
                                if (parameters.Count % 4 != 0 && parameters.Count >= 4)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count / 4).ToList().ForEach(i =>
                                {
                                    list.Add(new QuadraticBézierCurveInstruction(parameters[i * 4], parameters[i * 4 + 1], parameters[i * 4 + 2], parameters[i * 4 + 3], instruction == "q", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "T" or "t":
                                if (parameters.Count % 2 != 0 && parameters.Count >= 2)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count / 2).ToList().ForEach(i =>
                                {
                                    list.Add(new ShorthandQuadraticBézierCurveInstruction(parameters[i * 2], parameters[i * 2 + 1], instruction == "t", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            case "A" or "a":
                                if (parameters.Count % 7 != 0 && parameters.Count >= 7)
                                    throw new ArgumentException($"Wrong number of parameters for '{instruction}' at number {curr} sequence in {strippedInput}");
                                Enumerable.Range(0, parameters.Count / 7).ToList().ForEach(i =>
                                {
                                    list.Add(new EllipticalArcInstruction(parameters[i * 7], parameters[i * 7 + 1], parameters[i * 7 + 2], parameters[i * 7 + 3] == 1, parameters[i * 7 + 4] == 1, parameters[i * 7 + 5], parameters[i * 7 + 6], instruction == "a", previous) { ExplicitSymbol = i == 0 });
                                    previous = list.Last();
                                });
                                break;
                            default:
                                throw new ArgumentException($"Non supported sequence initializer: {instruction}");
                        }
                        return list;
                    });
            if (list.Count >= 2)
            {
                for (int i = 0; i < list.Count - 1; i++)
                {
                    list[i].NextInstruction = list[i + 1];
                }

            }
            return list;
        }

        public static string AsString(this List<IPathInstruction> pathData) => string.Join(" ", pathData.Select(p => p.ToString()));

        public static string AsString(this double d) => Math.Round(d, 9).ToString(CultureInfo.InvariantCulture);

        public static double ParseAsDouble(this string s) => double.Parse(s, CultureInfo.InvariantCulture);
    }
}
