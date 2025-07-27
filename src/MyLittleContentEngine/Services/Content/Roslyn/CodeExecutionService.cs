using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace MyLittleContentEngine.Services.Content.Roslyn;

/// <summary>
/// Handles executing sample code.
/// </summary>
/// <param name="logger"></param>
internal class CodeExecutionService(ILogger<CodeExecutionService> logger)
{
    internal Dictionary<string, string> ExecuteMethod(Assembly assembly, IMethodSymbol methodSymbol)
    {
        logger.LogTrace("Executing {name}", methodSymbol.Name);
        var typeName = methodSymbol.ContainingType.ToDisplayString();
        var type = assembly.GetType(typeName);
        if (type == null)
        {
            throw new Exception($"Type not found: {typeName}");
        }

        var methodName = methodSymbol.Name;
        var method = type.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance) ?? throw new Exception($"Method not found: {methodName}");
        object? instance = null;
        if (!method.IsStatic)
        {
            instance = Activator.CreateInstance(type);
        }

        var consoleOutput = new StringWriter();
        var originalConsoleOut = Console.Out;
        Console.SetOut(consoleOutput);
        Dictionary<string, string> results = new();
        object? returnValue;
        try
        {
            var invokeResult = method.Invoke(instance, []);
            if (invokeResult is Task task)
            {
                // await task.ConfigureAwait(false);
                var taskType = task.GetType();
                if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    // Get Task<T>.Result via reflection
                    returnValue = taskType.GetProperty("Result")?.GetValue(task);
                }
                else
                {
                    // Task or VoidTaskResult
                    returnValue = null;
                }
            }
            else
            {
                returnValue = invokeResult;
            }
        }
        finally
        {
            Console.SetOut(originalConsoleOut);
        }

        switch (returnValue)
        {
            case IEnumerable<(string Key, string Value)> keyValuePairs:
                foreach (var keyValuePair in keyValuePairs)
                {
                    results.Add(keyValuePair.Key, keyValuePair.Value);
                }

                break;
            case null:
                results.Add(string.Empty, "(null)");
                break;
            default:
                results.Add(string.Empty, returnValue.ToString() ?? "(null)");
                break;
        }

        return results;
    }
}