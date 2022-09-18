using FluentNHibernate.Mapping;
using NHibernate;
using NHibernate_Practice.Infrastructure.Entities;

namespace NHibernate_Practice.Infrastructure.Mappings
{
    public class DeveloperMap : ClassMap<Developer>
    {
        public DeveloperMap()
        {
            Id(x => x.Id);
            Map(x => x.Name);
            Map(x => x.Status);
            Map(x => x.CreatedAt);
            Table("Developer");
        }
    }
}
