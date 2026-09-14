namespace UTH.Library.Application.Abstractions.Identity;

public enum LinkedAccountDeactivationResult
{
    NotLinked,
    AlreadyInactive,
    Deactivated,
    Protected
}

public interface IEmployeeAccountLifecycle
{
    Task<LinkedAccountDeactivationResult> DeactivateAsync(
        Guid? userId,
        Guid? actorUserId,
        CancellationToken cancellationToken);
}
