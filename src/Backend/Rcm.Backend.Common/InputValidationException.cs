using System;

namespace Rcm.Backend.Common;

public class InputValidationException : Exception
{
    public string Path { get; } = String.Empty;
    public string ErrorMessage { get; } = String.Empty;

    public InputValidationException(string path, string error)
        : base($"{path}: {error}")
    {
        (Path, ErrorMessage) = (path, error);
    }

    public InputValidationException()
    {
    }

    public InputValidationException(string message) : base(message)
    {
    }

    public InputValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}