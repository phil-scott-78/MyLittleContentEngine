using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using MyLittleContentEngine.Services.Content.CodeAnalysis.Configuration;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Execution;

/// <summary>
/// Implementation of ICodeExecutionService that handles executing code in an isolated context
/// </summary>
internal class CodeExecutionService : ICodeExecutionService
{
    private readonly ILogger<CodeExecutionService> _logger;
    private readonly ExecutionOptions _options;

    public CodeExecutionService(ILogger<CodeExecutionService> logger, CodeAnalysisOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Execution ?? new ExecutionOptions();
    }

    public Task<ExecutionResult> ExecuteAsync(string code, System.Reflection.Assembly assembly)
    {
        // This would require dynamic compilation of the code string
        // For now, this is not implemented
        throw new NotImplementedException("Direct code execution is not yet implemented");
    }

    public async Task<ExecutionResult> ExecuteMethodAsync(System.Reflection.Assembly assembly, IMethodSymbol methodSymbol)
    {
        _logger.LogTrace("Executing method {MethodName}", methodSymbol.Name);

        var stopwatch = Stopwatch.StartNew();
        var typeName = methodSymbol.ContainingType.ToDisplayString();
        var type = assembly.GetType(typeName);
        
        if (type == null)
        {
            return ExecutionResult.CreateFailure($"Type not found: {typeName}");
        }

        var methodName = methodSymbol.Name;
        var method = type.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance);
            
        if (method == null)
        {
            return ExecutionResult.CreateFailure($"Method not found: {methodName}");
        }

        return await ExecuteMethodInternalAsync(method, type, stopwatch);
    }

    public async Task<ExecutionResult> ExecuteMethodAsync(string xmlDocId, System.Reflection.Assembly assembly)
    {
        // Parse the XML doc ID to find the method
        // Format: M:Namespace.Type.Method(Parameters)
        if (!xmlDocId.StartsWith("M:"))
        {
            return ExecutionResult.CreateFailure($"Invalid XML doc ID for method: {xmlDocId}");
        }

        var methodPath = xmlDocId.Substring(2); // Remove "M:"
        var lastDotIndex = methodPath.LastIndexOf('.');
        if (lastDotIndex < 0)
        {
            return ExecutionResult.CreateFailure($"Invalid method path: {methodPath}");
        }

        var typeName = methodPath.Substring(0, lastDotIndex);
        var methodName = methodPath.Substring(lastDotIndex + 1);
        
        // Remove parameter list if present
        var parenIndex = methodName.IndexOf('(');
        if (parenIndex >= 0)
        {
            methodName = methodName.Substring(0, parenIndex);
        }

        var type = assembly.GetType(typeName);
        if (type == null)
        {
            return ExecutionResult.CreateFailure($"Type not found: {typeName}");
        }

        var method = type.GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance);
            
        if (method == null)
        {
            return ExecutionResult.CreateFailure($"Method not found: {methodName} in type {typeName}");
        }

        var stopwatch = Stopwatch.StartNew();
        return await ExecuteMethodInternalAsync(method, type, stopwatch);
    }

    private async Task<ExecutionResult> ExecuteMethodInternalAsync(MethodInfo method, Type type, Stopwatch stopwatch)
    {
        object? instance = null;
        if (!method.IsStatic)
        {
            try
            {
                instance = Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                return ExecutionResult.CreateFailure(
                    $"Failed to create instance of type {type.Name}: {ex.Message}", ex);
            }
        }

        // Capture console output
        var consoleOutput = new StringWriter();
        var consoleError = new StringWriter();
        var originalOut = Console.Out;
        var originalError = Console.Error;

        Console.SetOut(consoleOutput);
        if (_options.CaptureErrorOutput)
        {
            Console.SetError(consoleError);
        }

        try
        {
            // Set up cancellation for timeout
            using var cts = new CancellationTokenSource(_options.TimeoutMs);
            
            var executeTask = Task.Run(async () =>
            {
                var result = method.Invoke(instance, null);
                
                // Handle async methods
                if (result is Task task)
                {
                    await task;
                    
                    // Get result from Task<T>
                    var taskType = task.GetType();
                    if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
                    {
                        result = taskType.GetProperty("Result")?.GetValue(task);
                    }
                    else
                    {
                        result = null; // Task with no result
                    }
                }

                return result;
            }, cts.Token);

            var executionResult = await executeTask;
            stopwatch.Stop();

            // Process the result
            var output = consoleOutput.ToString();
            var resultOutput = FormatResult(executionResult);
            
            if (!string.IsNullOrEmpty(resultOutput))
            {
                output = string.IsNullOrEmpty(output) ? resultOutput : $"{output}\n{resultOutput}";
            }

            // Truncate output if needed
            if (output.Length > _options.MaxOutputSize)
            {
                output = output.Substring(0, _options.MaxOutputSize) + "\n... (output truncated)";
            }

            var result = ExecutionResult.CreateSuccess(output, stopwatch.Elapsed);
            result.Metadata["MethodName"] = method.Name;
            result.Metadata["TypeName"] = type.FullName ?? type.Name;
            
            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return ExecutionResult.CreateFailure(
                $"Method execution timed out after {_options.TimeoutMs}ms",
                new TimeoutException($"Execution exceeded timeout of {_options.TimeoutMs}ms"));
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMessage = ex.InnerException != null 
                ? $"{ex.Message}: {ex.InnerException.Message}"
                : ex.Message;
                
            return ExecutionResult.CreateFailure(errorMessage, ex);
        }
        finally
        {
            Console.SetOut(originalOut);
            if (_options.CaptureErrorOutput)
            {
                Console.SetError(originalError);
            }
        }
    }

    private string FormatResult(object? result)
    {
        switch (result)
        {
            case null:
                return string.Empty;
                
            case IEnumerable<(string Key, string Value)> keyValuePairs:
                var kvps = keyValuePairs.ToList();
                if (!kvps.Any())
                {
                    return string.Empty;
                }
                return string.Join("\n", kvps.Select(kvp => 
                    string.IsNullOrEmpty(kvp.Key) ? kvp.Value : $"{kvp.Key}: {kvp.Value}"));
                
            default:
                return result.ToString() ?? string.Empty;
        }
    }

    // Legacy method for backward compatibility
    internal Dictionary<string, string> ExecuteMethod(System.Reflection.Assembly assembly, IMethodSymbol methodSymbol)
    {
        var result = ExecuteMethodAsync(assembly, methodSymbol).GetAwaiter().GetResult();
        
        var output = new Dictionary<string, string>();
        if (result.Success)
        {
            // Parse the output back into dictionary format for legacy compatibility
            var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = line.Substring(0, colonIndex).Trim();
                    var value = line.Substring(colonIndex + 1).Trim();
                    output[key] = value;
                }
                else
                {
                    output[string.Empty] = line;
                }
            }
        }
        else
        {
            output["Error"] = result.ErrorOutput;
        }
        
        return output;
    }
}