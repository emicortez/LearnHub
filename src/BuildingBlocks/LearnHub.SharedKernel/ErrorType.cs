namespace LearnHub.SharedKernel;

/// <summary>
/// The kind of an expected business failure. Callers translate it to their own protocol
/// (for example an HTTP status code) without inspecting the error code.
/// </summary>
public enum ErrorType
{
    /// <summary>The input breaks a rule of a value or an operation.</summary>
    Validation,

    /// <summary>The requested resource does not exist.</summary>
    NotFound,

    /// <summary>The operation is not allowed in the current state of the resource.</summary>
    Conflict,
}
