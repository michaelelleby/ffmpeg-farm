using System;
using API.Database;
using Contract;

namespace API.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly FfmpegFarmContext _context;

        public UnitOfWork(FfmpegFarmContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            _context = context;

            Jobs = new JobRepository(context);
            Tasks = new TaskRepository(context);
            Clients = new ClientRepository(context);
            AudioRequests = new AudioRequestRepository(context);
        }

        public IJobRepository Jobs { get; }
        public ITaskRepository Tasks { get; }
        public IClientRepository Clients { get; }
        public IAudioRequestRepository AudioRequests { get; }

        public void Complete()
        {
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}