using NHibernate_Practice.Infrastructure.BusinessObjects;
using NHibernate_Practice.Infrastructure.Services;

namespace NHibernate_PracticeMvc.Models
{
    public class DeveloperCreateModel
    {
        DeveloperServices developerServices = new DeveloperServices();
        public virtual string? Name { get; set; }
        public virtual string? Status { get; set; }
        public virtual DateTime? CreatedAt { get; set; }

        internal void AddDeveloper(DeveloperCreateModel model)
        {
            var entity = new Developer
            {
                Name = model.Name,
                Status = model.Status,
                CreatedAt = model.CreatedAt
            };
            developerServices.Add(entity);
        }
    }
}
