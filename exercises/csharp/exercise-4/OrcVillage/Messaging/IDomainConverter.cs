using System;

namespace OrcVillage.Messaging
{
    public interface IDto
    {
    }

    public interface IDomainConverter<TDomain>
    {
        Type TargetDtoType { get; }
        
        TDomain ToDomain(IDto dto);

        IDto FromDomain(TDomain domain);
    }
}