namespace ECommerce.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
