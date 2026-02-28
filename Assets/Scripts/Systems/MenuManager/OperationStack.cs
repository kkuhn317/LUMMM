using System.Collections.Generic;
using UnityEngine;

public static class OperationStack
{
    private static readonly Stack<ICancelableOperation> stack = new();

    public static void Push(ICancelableOperation op)
    {
        if (op == null) return;
        stack.Push(op);
    }

    public static void Pop(ICancelableOperation op)
    {
        if (op == null) return;

        var temp = new Stack<ICancelableOperation>();
        while (stack.Count > 0)
        {
            var top = stack.Pop();
            if (ReferenceEquals(top, op)) break;
            temp.Push(top);
        }
        while (temp.Count > 0) stack.Push(temp.Pop());
    }

    public static ICancelableOperation Peek()
    {
        while (stack.Count > 0)
        {
            var obj = stack.Peek() as Object;
            if (obj == null) stack.Pop();
            else break;
        }
        return stack.Count > 0 ? stack.Peek() : null;
    }
}