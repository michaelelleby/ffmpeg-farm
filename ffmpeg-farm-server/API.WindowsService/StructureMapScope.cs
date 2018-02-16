using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;
using StructureMap;

namespace API.WindowsService
{
    public class StructureMapScope : IDependencyScope
    {
        private readonly IContainer _container;

        public StructureMapScope(IContainer container)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        object IDependencyScope.GetService(Type serviceType)
        {
            if (serviceType == null)
                return null;

            if (serviceType.IsAbstract || serviceType.IsInterface)
                return _container.TryGetInstance(serviceType);

            return _container.GetInstance(serviceType);
        }

        IEnumerable<object> IDependencyScope.GetServices(Type serviceType)
        {
            return _container.GetAllInstances(serviceType).Cast<object>();
        }

        void IDisposable.Dispose()
        {
            _container.Dispose();
        }
    }
}