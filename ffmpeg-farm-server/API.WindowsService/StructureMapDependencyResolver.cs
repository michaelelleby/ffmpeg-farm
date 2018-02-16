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
            _container = container ?? throw new ArgumentNullException(nameof(container));
        }

        IDependencyScope IDependencyResolver.BeginScope()
        {
            var childContainer = _container.GetNestedContainer();
            return new StructureMapScope(childContainer);
        }
    }
}