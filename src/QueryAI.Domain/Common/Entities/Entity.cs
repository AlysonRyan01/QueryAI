namespace QueryAI.Domain.Common.Entities;

public abstract class Entity
{
    protected Entity()
    {
        Id = Guid.CreateVersion7();
    }

    public Guid Id { get; protected set; }
}
