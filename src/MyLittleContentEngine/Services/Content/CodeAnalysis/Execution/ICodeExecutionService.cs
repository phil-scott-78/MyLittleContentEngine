using Microsoft.CodeAnalysis;

namespace MyLittleContentEngine.Services.Content.CodeAnalysis.Execution;

/// <summary>
/// Service responsible for executing code in an isolated context
/// </summary>
public interface ICodeExecutionService
{
    /// <summary>
    /// Executes a code string in the context of a loaded assembly
    /// </summary>
    /// <param name="code">The code to execute</param>
    /// <param name="assembly">The assembly context for execution</param>
    /// <returns>Execution result with output and errors</returns>
    Task<ExecutionResult> ExecuteAsync(string code, System.Reflection.Assembly assembly);

    /// <summary>
    /// Executes a specific method from an assembly
    /// </summary>
    /// <param name="assembly">The assembly containing the method</param>
    /// <param name="methodSymbol">The method symbol to execute</param>
    /// <returns>Execution result with output and errors</returns>
    Task<ExecutionResult> ExecuteMethodAsync(System.Reflection.Assembly assembly, IMethodSymbol methodSymbol);

    /// <summary>
    /// Executes a method by its XML documentation ID
    /// </summary>
    /// <param name="xmlDocId">The XML documentation ID of the method</param>
    /// <param name="assembly">The assembly containing the method</param>
    /// <returns>Execution result with output and errors</returns>
    Task<ExecutionResult> ExecuteMethodAsync(string xmlDocId, System.Reflection.Assembly assembly);
}

/// <summary>
/// Result of code execution
/// </summary>
public record ExecutionResult
{
    /// <summary>
    /// Whether the execution completed successfully
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Standard output from the execution
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// Error output from the execution
    /// </summary>
    public required string ErrorOutput { get; init; }

    /// <summary>
    /// Exception details if execution failed
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Execution duration
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Additional metadata about the execution
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Creates a successful execution result
    /// </summary>
    public static ExecutionResult CreateSuccess(string output, TimeSpan? duration = null) => new()
    {
        Success = true,
        StandardOutput = output,
        ErrorOutput = string.Empty,
        Duration = duration
    };

    /// <summary>
    /// Creates a failed execution result
    /// </summary>
    public static ExecutionResult CreateFailure(string error, Exception? exception = null) => new()
    {
        Success = false,
        StandardOutput = string.Empty,
        ErrorOutput = error,
        Exception = exception
    };
}