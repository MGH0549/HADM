namespace Hadm.Application.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
