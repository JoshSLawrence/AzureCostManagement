namespace AzureCostManagement.Exceptions;

[Serializable]
public class InvalidQueryResultException : Exception
{
    public InvalidQueryResultException()
    { }

    public InvalidQueryResultException(string message)
        : base(message)
    { }

    public InvalidQueryResultException(string message, Exception innerException)
        : base(message, innerException)
    { }
}
