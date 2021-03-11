using DryIoc;
using NzbDrone.Core.Datastore.Migration.Framework;

namespace NzbDrone.Core.Datastore.Extensions
{
    public static class CompositionExtensions
    {
        public static IContainer AddDatabase(this IContainer container)
        {
            container.RegisterDelegate<IDbFactory, IMainDatabase>(f => new MainDatabase(f.Create()), Reuse.Singleton);
            container.RegisterDelegate<IDbFactory, ILogDatabase>(f => new LogDatabase(f.Create(MigrationType.Log)), Reuse.Singleton);
            container.RegisterDelegate<IDbFactory, ICacheDatabase>(f => new CacheDatabase(f.Create(MigrationType.Cache)), Reuse.Singleton);

            return container;
        }

        public static IContainer AddDummyDatabase(this IContainer container)
        {
            container.RegisterInstance<IMainDatabase>(new MainDatabase(null));
            container.RegisterInstance<ILogDatabase>(new LogDatabase(null));
            container.RegisterInstance<ICacheDatabase>(new CacheDatabase(null));

            return container;
        }
    }
}
