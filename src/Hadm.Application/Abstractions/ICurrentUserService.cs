namespace Hadm.Application.Abstractions;

public interface ICurrentUserService
{
    string? UserId { get; }
}
