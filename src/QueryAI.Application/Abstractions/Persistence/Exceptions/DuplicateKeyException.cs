namespace QueryAI.Application.Abstractions.Persistence.Exceptions;

public sealed class DuplicateKeyException(string message, Exception innerException)
    : Exception(message, innerException);
