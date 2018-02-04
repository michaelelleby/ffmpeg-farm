using System;

namespace Contract
{
    public interface IUnitOfWork : IDisposable
    {
        IJobRepository Jobs { get; }
        ITaskRepository Tasks { get; }
        IClientRepository Clients { get; }
        IAudioRequestRepository AudioRequests { get; }
        void Complete();
    }
}