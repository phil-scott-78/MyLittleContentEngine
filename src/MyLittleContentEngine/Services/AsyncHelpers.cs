namespace MyLittleContentEngine.Services;

internal static class AsyncHelpers
{
    private static readonly TaskFactory TaskFactory = new(CancellationToken.None,
        TaskCreationOptions.None,
        TaskContinuationOptions.None,
        TaskScheduler.Default);

    public static TResult RunSync<TResult>(Func<Task<TResult>> func,
        CancellationToken cancellationToken = default(CancellationToken))
        => TaskFactory
            .StartNew(func, cancellationToken)
            .Unwrap()
            .GetAwaiter()
            .GetResult();
}