using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PurrplingCore.Toolkit.Collections;

public sealed class Hierarchy<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<T>> _relations = [];

    public IReadOnlyCollection<T> this[T key] => _relations[key];

    public int ParentCount => _relations.Count;

    public bool AddChild(T parent, T child)
    {
        // 1. Check self-reference (A -> A)
        if (EqualityComparer<T>.Default.Equals(parent, child))
        {
            throw new InvalidOperationException($"Node '{parent}' cannot be a child of itself.");
        }

        // 2. Check for cycles (Is there already a path Child -> ... -> Parent?)
        if (HasPath(start: child, end: parent))
        {
            throw new InvalidOperationException($"Circular reference detected: Adding '{parent} -> {child}' creates a cycle.");
        }

        // 3. Bezpečné vložení
        if (!_relations.TryGetValue(parent, out var children))
        {
            children = [];
            _relations[parent] = children;
        }

        return children.Add(child);
    }

    public void RemoveChild(T parent, T child)
    {
        if (_relations.TryGetValue(parent, out var children))
        {
            children.Remove(child);
            if (children.Count == 0)
            {
                _relations.Remove(parent);
            }
        }
    }

    public bool ContainsNode(T node)
    {
        return _relations.ContainsKey(node);
    }

    public bool ContainsChild(T parent, T child)
    {
        return _relations.TryGetValue(parent, out var children) && children.Contains(child);
    }

    public IReadOnlyCollection<T> GetChildren(T node)
    {
        if (_relations.TryGetValue(node, out var set))
        {
            return set;
        }
        throw new KeyNotFoundException($"Node '{node}' does not exist in the hierarchy.");
    }


    public ReadOnlySpan<T> GetParrents(T node)
    {
        HashSet<T> parents = [];
        foreach (var kvp in _relations)
        {
            if (kvp.Value.Contains(node))
            {
                parents.Add(kvp.Key);
            }
        }
        return new ReadOnlySpan<T>([.. parents]);
    }

    public bool TryGetChildren(T node, [MaybeNullWhen(false)] out IReadOnlyCollection<T> children)
    {
        if (_relations.TryGetValue(node, out var set))
        {
            children = set;
            return true;
        }

        children = null;
        return false;
    }

    private bool HasPath(T start, T end)
    {
        // Rychlá optimalizace: Pokud 'start' nemá žádné děti, cesta neexistuje.
        if (!_relations.ContainsKey(start)) return false;

        var stack = new Stack<T>();
        stack.Push(start);

        var visited = new HashSet<T>();

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            // Pokud jsme uzel už viděli v této větvi, přeskočíme ho
            if (!visited.Add(current)) continue;

            if (EqualityComparer<T>.Default.Equals(current, end)) return true;

            if (_relations.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    // Rychlý check před vložením na stack
                    if (EqualityComparer<T>.Default.Equals(child, end)) return true;
                    stack.Push(child);
                }
            }
        }

        return false;
    }

    public void Clear()
    {
        _relations.Clear();
    }
}
