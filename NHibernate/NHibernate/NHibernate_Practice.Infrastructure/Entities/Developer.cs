using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate_Practice.Infrastructure.Entities
{
    public class Developer
    {
        public virtual int Id { get; set; }
        public virtual string? Name { get; set; }
        public virtual string? Status { get; set; }
        public virtual DateTime? CreatedAt { get; set; }
    }
}
