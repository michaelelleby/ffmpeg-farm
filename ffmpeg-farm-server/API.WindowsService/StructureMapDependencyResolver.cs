using System;
using System.Web.Http.Dependencies;
using StructureMap;

namespace API.WindowsService
{
    public class StructureMapDependencyResolver : StructureMapScope, IDependencyResolver
    {
        private readonly IContainer _container;

        public StructureMapDependencyResolver(IContainer container) : base(container)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));

            _container = container;
        }

        IDependencyScope IDependencyResolver.BeginScope()
        {
            var childContainer = _container.GetNestedContainer();
            return new StructureMapScope(childContainer);
        }
    }
}