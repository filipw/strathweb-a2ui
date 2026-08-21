using Strathweb.A2UI.Components;

namespace Strathweb.A2UI.Validation;

/// <summary>
/// Checks that a set of components hangs together: unique ids, a root, no dangling references, no
/// cycles, nothing unreachable.
/// </summary>
internal static class ComponentGraphChecker
{
    internal static void Check(
        IReadOnlyList<A2UIComponent> components,
        string path,
        A2UIValidationOptions options,
        bool allowMissingRoot,
        List<A2UIValidationError> errors)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (var i = 0; i < components.Count; i++)
        {
            var id = components[i].Id;

            // An empty id is a *missing* id. Feeding it to the duplicate set would make two
            // unnamed components report each other as duplicates and hide the real problem.
            if (id.Length == 0)
            {
                errors.Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Integrity,
                    A2UIErrorCodes.MissingId,
                    $"{path}.{i}.id",
                    "A component must have a non-empty id."));
                continue;
            }

            if (!ids.Add(id))
            {
                errors.Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Integrity,
                    A2UIErrorCodes.DuplicateId,
                    $"{path}.{i}.id",
                    $"Duplicate component ID: {id}"));
            }
        }

        var references = new Dictionary<string, List<A2UIComponentReference>>(StringComparer.Ordinal);
        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];
            if (component.Id.Length == 0)
            {
                continue;
            }

            references[component.Id] = ComponentReferenceReader.Read(component, options.Catalog);
        }

        if (!allowMissingRoot && !ids.Contains(options.RootComponentId))
        {
            errors.Add(new A2UIValidationError(
                A2UIValidationErrorCategory.Integrity,
                A2UIErrorCodes.MissingRoot,
                path,
                $"Missing root component: No component has id='{options.RootComponentId}'"));
        }

        if (!options.AllowDanglingReferences)
        {
            CheckDangling(components, references, ids, path, errors);
        }

        CheckTopology(references, ids, path, options, allowMissingRoot, errors);
    }

    private static void CheckDangling(
        IReadOnlyList<A2UIComponent> components,
        Dictionary<string, List<A2UIComponentReference>> references,
        HashSet<string> ids,
        string path,
        List<A2UIValidationError> errors)
    {
        for (var i = 0; i < components.Count; i++)
        {
            var component = components[i];
            if (!references.TryGetValue(component.Id, out var componentReferences))
            {
                continue;
            }

            foreach (var reference in componentReferences)
            {
                if (!ids.Contains(reference.ComponentId))
                {
                    errors.Add(new A2UIValidationError(
                        A2UIValidationErrorCategory.Integrity,
                        A2UIErrorCodes.UnresolvedReference,
                        $"{path}.{i}.{reference.PropertyPath}",
                        $"Component '{component.Id}' references non-existent component " +
                        $"'{reference.ComponentId}' in field '{reference.PropertyPath}'"));
                }
            }
        }
    }

    private static void CheckTopology(
        Dictionary<string, List<A2UIComponentReference>> references,
        HashSet<string> ids,
        string path,
        A2UIValidationOptions options,
        bool allowMissingRoot,
        List<A2UIValidationError> errors)
    {
        var edges = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var selfReferenced = false;

        foreach (var pair in references)
        {
            var targets = new List<string>(pair.Value.Count);
            foreach (var reference in pair.Value)
            {
                if (string.Equals(reference.ComponentId, pair.Key, StringComparison.Ordinal))
                {
                    selfReferenced = true;
                    errors.Add(new A2UIValidationError(
                        A2UIValidationErrorCategory.Recursion,
                        A2UIErrorCodes.SelfReference,
                        $"{path}.{reference.PropertyPath}",
                        $"Self-reference detected: Component '{pair.Key}' references itself in " +
                        $"field '{reference.PropertyPath}'"));
                    continue;
                }

                targets.Add(reference.ComponentId);
            }

            edges[pair.Key] = targets;
        }

        if (selfReferenced)
        {
            // The graph already has a proven cycle; walking it would only restate that.
            return;
        }

        var visited = new HashSet<string>(StringComparer.Ordinal);

        if (allowMissingRoot)
        {
            var roots = new List<string>(edges.Keys);
            roots.Sort(StringComparer.Ordinal);
            foreach (var start in roots)
            {
                if (!visited.Contains(start) && !Walk(start, edges, visited, path, options, errors))
                {
                    return;
                }
            }

            return;
        }

        if (ids.Contains(options.RootComponentId) &&
            !Walk(options.RootComponentId, edges, visited, path, options, errors))
        {
            return;
        }

        if (options.AllowOrphanComponents)
        {
            return;
        }

        var orphans = new List<string>();
        foreach (var id in ids)
        {
            if (!visited.Contains(id))
            {
                orphans.Add(id);
            }
        }

        orphans.Sort(StringComparer.Ordinal);
        foreach (var orphan in orphans)
        {
            errors.Add(new A2UIValidationError(
                A2UIValidationErrorCategory.Integrity,
                A2UIErrorCodes.OrphanComponent,
                path,
                $"Component '{orphan}' is not reachable from '{options.RootComponentId}'"));
        }
    }

    /// <summary>
    /// Depth-first search over an explicit stack. Recursion here would overflow on generated or
    /// hostile input long before any depth limit could report it.
    /// </summary>
    /// <returns><see langword="false"/> when the graph is unsound and traversal must stop.</returns>
    private static bool Walk(
        string start,
        Dictionary<string, List<string>> edges,
        HashSet<string> visited,
        string path,
        A2UIValidationOptions options,
        List<A2UIValidationError> errors)
    {
        var stack = new List<Frame> { new(start, 0) };
        var onStack = new HashSet<string>(StringComparer.Ordinal) { start };
        visited.Add(start);

        while (stack.Count > 0)
        {
            var frame = stack[stack.Count - 1];
            var neighbours = edges.TryGetValue(frame.Id, out var list) ? list : null;

            if (neighbours is null || frame.Index >= neighbours.Count)
            {
                onStack.Remove(frame.Id);
                stack.RemoveAt(stack.Count - 1);
                continue;
            }

            var neighbour = neighbours[frame.Index];
            stack[stack.Count - 1] = frame.Advanced();

            if (onStack.Contains(neighbour))
            {
                errors.Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Recursion,
                    A2UIErrorCodes.CircularReference,
                    path,
                    $"Circular reference detected involving component '{neighbour}'"));
                return false;
            }

            if (visited.Contains(neighbour))
            {
                continue;
            }

            if (frame.Depth + 1 > options.MaxDepth)
            {
                errors.Add(new A2UIValidationError(
                    A2UIValidationErrorCategory.Recursion,
                    A2UIErrorCodes.RecursionLimit,
                    path,
                    $"Global recursion limit exceeded: logical depth > {options.MaxDepth}"));
                return false;
            }

            visited.Add(neighbour);
            onStack.Add(neighbour);
            stack.Add(new Frame(neighbour, frame.Depth + 1));
        }

        return true;
    }

    private readonly struct Frame(string id, int depth, int index = 0)
    {
        internal string Id { get; } = id;

        internal int Depth { get; } = depth;

        internal int Index { get; } = index;

        internal Frame Advanced() => new(Id, Depth, Index + 1);
    }
}
