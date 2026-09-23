namespace StockFlow.Domain.Exceptions;

/// <summary>
/// Raised when an operation would break a business invariant.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    /// <param name="message">Description of the broken invariant.</param>
    public DomainException(string message) : base(message)
    {
    }
}
