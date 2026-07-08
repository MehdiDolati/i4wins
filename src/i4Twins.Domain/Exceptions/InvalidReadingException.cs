namespace i4Twins.Domain.Exceptions;

public class InvalidReadingException : Exception
{
    public InvalidReadingException(string message) : base(message) { }
    public InvalidReadingException(string message, Exception inner) : base(message, inner) { }
}