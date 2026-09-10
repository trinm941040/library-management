namespace UTH.Library.Application.Abstractions;

public interface IRequestContext
{
    Guid? UserId { get; }
    string CorrelationId { get; }
}
