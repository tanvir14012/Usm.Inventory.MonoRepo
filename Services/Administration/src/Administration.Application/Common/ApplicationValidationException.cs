namespace Administration.Application.Common;

public sealed class ApplicationValidationException(string message) : Exception(message)
{
}
