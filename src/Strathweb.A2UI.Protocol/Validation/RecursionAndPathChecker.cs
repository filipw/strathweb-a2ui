using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Strathweb.A2UI.Validation;

/// <summary>
/// Walks the raw payload looking for nesting that runs too deep and data-binding paths that are not
/// well formed.
/// </summary>
internal static class RecursionAndPathChecker
{
    /// <summary>
    /// Accepts a JSON Pointer, and also the relative form a template scope uses. Ported from the
    /// reference implementation so that the same paths are accepted and rejected.
    /// </summary>
    private static readonly Regex PathPattern = new(
        @"^(?:(?:\/(?:[^~\/]|~[01])*)*|(?:[^~\/]|~[01])+(?:\/(?:[^~\/]|~[01])*)*)$",
        RegexOptions.CultureInvariant);

    internal static void Check(JsonNode? payload, A2UIValidationOptions options, List<A2UIValidationError> errors)
    {
        var stack = new Stack<Frame>();
        stack.Push(new Frame(payload, 0, 0, string.Empty));

        var reportedDepth = false;
        var reportedFunctionDepth = false;

        while (stack.Count > 0)
        {
            var frame = stack.Pop();

            if (frame.Depth > options.MaxDepth)
            {
                if (!reportedDepth)
                {
                    reportedDepth = true;
                    errors.Add(new A2UIValidationError(
                        A2UIValidationErrorCategory.Recursion,
                        A2UIErrorCodes.RecursionLimit,
                        frame.Path,
                        $"Global recursion limit exceeded: Depth > {options.MaxDepth}"));
                }

                continue;
            }

            switch (frame.Node)
            {
                case JsonArray array:
                    for (var i = array.Count - 1; i >= 0; i--)
                    {
                        stack.Push(new Frame(array[i], frame.Depth + 1, frame.FunctionDepth, $"{frame.Path}[{i}]"));
                    }

                    break;

                case JsonObject obj:
                    PushChildren(obj, frame, options, stack, errors, ref reportedFunctionDepth);
                    break;
            }
        }
    }

    private static void PushChildren(
        JsonObject obj,
        Frame frame,
        A2UIValidationOptions options,
        Stack<Frame> stack,
        List<A2UIValidationError> errors,
        ref bool reportedFunctionDepth)
    {
        if (obj["path"] is JsonValue pathValue && pathValue.TryGetValue<string>(out var path) &&
            !PathPattern.IsMatch(path))
        {
            errors.Add(new A2UIValidationError(
                A2UIValidationErrorCategory.Validation,
                A2UIErrorCodes.InvalidPointer,
                $"{frame.Path}.path",
                $"Invalid path syntax: '{path}'"));
        }

        // Two spellings of a nested call: the v0.8 wrapper object, and the v0.9 call/args pair.
        var wrapsFunctionCall = obj["functionCall"] is JsonObject;
        var isFunctionCall = obj.ContainsKey("call") && obj.ContainsKey("args");

        if (wrapsFunctionCall || isFunctionCall)
        {
            if (frame.FunctionDepth >= options.MaxFunctionCallDepth)
            {
                if (!reportedFunctionDepth)
                {
                    reportedFunctionDepth = true;
                    errors.Add(new A2UIValidationError(
                        A2UIValidationErrorCategory.Recursion,
                        A2UIErrorCodes.FunctionCallRecursionLimit,
                        frame.Path,
                        $"Recursion limit exceeded: functionCall depth > {options.MaxFunctionCallDepth}"));
                }

                return;
            }

            if (wrapsFunctionCall)
            {
                stack.Push(new Frame(
                    obj["functionCall"],
                    frame.Depth + 1,
                    frame.FunctionDepth + 1,
                    $"{frame.Path}.functionCall"));
                return;
            }

            foreach (var pair in obj)
            {
                // Only the arguments deepen the call chain; the call's own metadata does not.
                var functionDepth = pair.Key == "args" ? frame.FunctionDepth + 1 : frame.FunctionDepth;
                stack.Push(new Frame(pair.Value, frame.Depth + 1, functionDepth, $"{frame.Path}.{pair.Key}"));
            }

            return;
        }

        foreach (var pair in obj)
        {
            stack.Push(new Frame(pair.Value, frame.Depth + 1, frame.FunctionDepth, $"{frame.Path}.{pair.Key}"));
        }
    }

    private readonly struct Frame(JsonNode? node, int depth, int functionDepth, string path)
    {
        internal JsonNode? Node { get; } = node;

        internal int Depth { get; } = depth;

        internal int FunctionDepth { get; } = functionDepth;

        internal string Path { get; } = path;
    }
}
