namespace RentalManager.Helpers;

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message)
    {
    }
}
