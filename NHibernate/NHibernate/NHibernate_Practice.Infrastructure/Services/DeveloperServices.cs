using NHibernate_Practice.Infrastructure.BusinessObjects;
using DeveloperEO = NHibernate_Practice.Infrastructure.Entities.Developer;
using NHibernate_Practice.Infrastructure.HibernetHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate_Practice.Infrastructure.Services
{
    public class DeveloperServices
    {
        private DeveloperEO MapEntity(Developer developer)
        {
            var entity = new DeveloperEO()
            {
                Name = developer.Name,
                Status = developer.Status,
                CreatedAt = developer.CreatedAt
            };
            return entity;
        }
        public void Add(Developer developer)
        {
            var entityEO = MapEntity(developer);
            var factory = FluentNHibernateHelper.Instance;
            var session = factory.OpenSession();
            session.SaveOrUpdate(entityEO);
        }
    }
}
