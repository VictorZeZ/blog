using FluentValidation;

namespace blog.Application.Users.Queries.GetMySessions
{
    public class GetMySessionsQueryValidator : AbstractValidator<GetMySessionsQuery>
    {
        public GetMySessionsQueryValidator()
        {
            RuleFor(x => x.ActorId)
                .NotEmpty();
        }
    }
}