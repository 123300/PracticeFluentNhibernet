using FluentNHibernate;
using FluentNHibernate.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHibernate_Practice.Infrastructure.Conventions
{
    public class CustomForeignKeyConvention : ForeignKeyConvention
    {
        protected override string GetKeyName(Member property, System.Type type)
        {
            if (property == null)
            {
                return type.Name;
            }

            return property.Name;
        }
    }
}
