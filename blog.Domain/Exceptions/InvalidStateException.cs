namespace blog.Domain.Exceptions
{
    public class InvalidStateException : DomainException
    {
        public InvalidStateException(string resource, string currentState, string expectedState)
            : base(409, "INVALID_STATE", $"{resource} is in an invalid state", new { Resource = resource, CurrentState = currentState, ExpectedState = expectedState })
        { }
    }
}
