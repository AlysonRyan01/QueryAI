namespace QueryAI.Application.Abstractions.Persistence.Exceptions;

public sealed class ConcurrencyConflictException(string message, Exception innerException)
    : Exception(message, innerException);
