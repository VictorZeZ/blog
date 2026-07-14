namespace blog.Domain.Exceptions
{
    public class EmailNotConfirmedException : DomainException
    {
        public EmailNotConfirmedException(string email)
            : base(403, "EMAIL_NOT_CONFIRMED", "Email address has not been confirmed", new { Email = email })
        { }
    }
}
