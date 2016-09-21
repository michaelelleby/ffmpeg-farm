using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http.Dependencies;
using StructureMap;

namespace API.WindowsService
{
    public class StructureMapScope : IDependencyScope
    {
        private readonly IContainer container;

        public StructureMapScope(IContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            this.container = container;
        }

        object IDependencyScope.GetService(Type serviceType)
        {
            if (serviceType == null)
                return null;

            if (serviceType.IsAbstract || serviceType.IsInterface)
                return container.TryGetInstance(serviceType);

            return container.GetInstance(serviceType);
        }

        IEnumerable<object> IDependencyScope.GetServices(Type serviceType)
        {
            return container.GetAllInstances(serviceType).Cast<object>();
        }

        void IDisposable.Dispose()
        {
            container.Dispose();
        }
    }
}