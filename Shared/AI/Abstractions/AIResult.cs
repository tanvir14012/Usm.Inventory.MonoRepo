namespace Shared.AI.Abstractions;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Represents an error or problem that occurred during an AI operation.
/// </summary>
public class AIError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIError"/> class.
    /// </summary>
    public AIError(string message, string? code = null, Exception? innerException = null, string? details = null)
    {
        Message = message;
        Code = code ?? "UNKNOWN";
        InnerException = innerException;
        Details = details;
    }

    /// <summary>Gets the error message.</summary>
    public string Message { get; }

    /// <summary>Gets the error code.</summary>
    public string Code { get; }

    /// <summary>Gets the inner exception if available.</summary>
    public Exception? InnerException { get; }

    /// <summary>Gets additional error details.</summary>
    public string? Details { get; }

    /// <summary>
    /// Creates an error for authentication failure.
    /// </summary>
    public static AIError AuthenticationFailed(string message = "Authentication failed") =>
        new(message, "AUTH_FAILED");

    /// <summary>
    /// Creates an error for invalid configuration.
    /// </summary>
    public static AIError InvalidConfiguration(string message) =>
        new(message, "INVALID_CONFIG");

    /// <summary>
    /// Creates an error for provider unavailable.
    /// </summary>
    public static AIError ProviderUnavailable(string message) =>
        new(message, "PROVIDER_UNAVAILABLE");

    /// <summary>
    /// Creates an error for rate limiting.
    /// </summary>
    public static AIError RateLimited(string message = "Rate limit exceeded") =>
        new(message, "RATE_LIMIT");

    /// <summary>
    /// Creates an error for timeout.
    /// </summary>
    public static AIError Timeout(string message = "Operation timed out") =>
        new(message, "TIMEOUT");

    /// <summary>
    /// Creates an error for invalid input.
    /// </summary>
    public static AIError InvalidInput(string message) =>
        new(message, "INVALID_INPUT");

    /// <summary>
    /// Creates an error for unsupported operation.
    /// </summary>
    public static AIError NotSupported(string message) =>
        new(message, "NOT_SUPPORTED");
}

/// <summary>
/// Represents the result of an AI operation that may succeed or fail.
/// Provides a monadic interface for error handling.
/// </summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public abstract record AIResult<TValue>
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AIResult<TValue> Success(TValue value) => new SuccessResult(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static AIResult<TValue> Failure(AIError error) => new FailureResult(error);

    /// <summary>
    /// Creates a failed result from a message.
    /// </summary>
    public static AIResult<TValue> Failure(string message) => 
        new FailureResult(new AIError(message));

    /// <summary>
    /// Matches the result and applies appropriate function.
    /// </summary>
    public abstract TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<AIError, TResult> onFailure);

    /// <summary>
    /// Applies a function if successful.
    /// </summary>
    public abstract Task<TResult> MatchAsync<TResult>(
        Func<TValue, Task<TResult>> onSuccess,
        Func<AIError, Task<TResult>> onFailure);

    /// <summary>
    /// Maps the success value.
    /// </summary>
    public abstract AIResult<TNext> Map<TNext>(Func<TValue, TNext> map);

    /// <summary>
    /// Binds with another result-returning function.
    /// </summary>
    public abstract AIResult<TNext> Bind<TNext>(Func<TValue, AIResult<TNext>> bind);

    /// <summary>
    /// Gets whether the result is successful.
    /// </summary>
    public abstract bool IsSuccess { get; }

    /// <summary>
    /// Gets the value or throws an exception if failed.
    /// </summary>
    public abstract TValue GetValueOrThrow();

    /// <summary>
    /// Gets the value or a default if failed.
    /// </summary>
    public abstract TValue? GetValueOrDefault(TValue? @default = default);

    /// <summary>
    /// Gets the error if failed.
    /// </summary>
    public abstract AIError? GetErrorOrNull();

    private sealed record SuccessResult(TValue Value) : AIResult<TValue>
    {
        public override TResult Match<TResult>(
            Func<TValue, TResult> onSuccess,
            Func<AIError, TResult> onFailure) =>
            onSuccess(Value);

        public override async Task<TResult> MatchAsync<TResult>(
            Func<TValue, Task<TResult>> onSuccess,
            Func<AIError, Task<TResult>> onFailure) =>
            await onSuccess(Value);

        public override AIResult<TNext> Map<TNext>(Func<TValue, TNext> map) =>
            new SuccessResult(map(Value));

        public override AIResult<TNext> Bind<TNext>(Func<TValue, AIResult<TNext>> bind) =>
            bind(Value);

        public override bool IsSuccess => true;

        public override TValue GetValueOrThrow() => Value;

        public override TValue? GetValueOrDefault(TValue? @default = default) => Value;

        public override AIError? GetErrorOrNull() => null;
    }

    private sealed record FailureResult(AIError Error) : AIResult<TValue>
    {
        public override TResult Match<TResult>(
            Func<TValue, TResult> onSuccess,
            Func<AIError, TResult> onFailure) =>
            onFailure(Error);

        public override async Task<TResult> MatchAsync<TResult>(
            Func<TValue, Task<TResult>> onSuccess,
            Func<AIError, Task<TResult>> onFailure) =>
            await onFailure(Error);

        public override AIResult<TNext> Map<TNext>(Func<TValue, TNext> map) =>
            new FailureResult(Error);

        public override AIResult<TNext> Bind<TNext>(Func<TValue, AIResult<TNext>> bind) =>
            new FailureResult(Error);

        public override bool IsSuccess => false;

        public override TValue GetValueOrThrow() =>
            throw new InvalidOperationException($"Operation failed: {Error.Message}", Error.InnerException);

        public override TValue? GetValueOrDefault(TValue? @default = default) => @default;

        public override AIError? GetErrorOrNull() => Error;
    }
}

/// <summary>
/// Non-generic result type for operations that don't return a value.
/// </summary>
public abstract record AIResult
{
    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static AIResult Success() => new SuccessResult();

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static AIResult Failure(AIError error) => new FailureResult(error);

    /// <summary>
    /// Creates a failed result from a message.
    /// </summary>
    public static AIResult Failure(string message) =>
        new FailureResult(new AIError(message));

    /// <summary>
    /// Gets whether the result is successful.
    /// </summary>
    public abstract bool IsSuccess { get; }

    /// <summary>
    /// Gets the error if failed.
    /// </summary>
    public abstract AIError? GetErrorOrNull();

    /// <summary>
    /// Throws if the result is failed.
    /// </summary>
    public abstract void ThrowIfFailed();

    private sealed record SuccessResult : AIResult
    {
        public override bool IsSuccess => true;
        public override AIError? GetErrorOrNull() => null;
        public override void ThrowIfFailed() { }
    }

    private sealed record FailureResult(AIError Error) : AIResult
    {
        public override bool IsSuccess => false;
        public override AIError? GetErrorOrNull() => Error;
        public override void ThrowIfFailed() =>
            throw new InvalidOperationException($"Operation failed: {Error.Message}", Error.InnerException);
    }
}
