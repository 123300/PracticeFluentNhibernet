using FluentNHibernate.Cfg.Db;
using FluentNHibernate.Cfg;
using NHibernate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NHibernate_Practice.Infrastructure.Entities;
using NHibernate.Tool.hbm2ddl;
using System.Reflection;
using NHibernate_Practice.Infrastructure.Conventions;

namespace NHibernate_Practice.Infrastructure.HibernetHelpers
{
    public static class FluentNHibernateHelper
    {
        private static ISessionFactory _sessionFactory;

        public static ISessionFactory Instance
        {
            get
            {
                if (_sessionFactory == null)
                {
                    _sessionFactory = CreateSessionFactory();
                }

                return _sessionFactory;
            }
        }

        private static ISessionFactory CreateSessionFactory()
        {
            return Fluently.Configure()
                        .Database(MsSqlConfiguration.MsSql2012
                            .ConnectionString("Server=DESKTOP-H4F0994; Database=NHibernet;Trusted_Connection= true;"))
                        .Mappings(m =>
                        {
                            m.FluentMappings.Conventions.AddFromAssemblyOf<CustomForeignKeyConvention>();
                            m.FluentMappings.AddFromAssemblyOf<NHibernate_Practice.Infrastructure.Entities.Developer>();
                        })
                        .BuildSessionFactory();
        }
    }
}
