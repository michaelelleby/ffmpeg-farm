using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Contract;

namespace API.Repository
{
    public class ScrubbingRepository : JobRepository, IAudioJobRepository
    {
        private readonly IHelper _helper;
        
        public ScrubbingRepository(IHelper helper) : base(helper)
        {
            if (helper == null) throw new ArgumentNullException(nameof(helper));
            _helper = helper;
        }

        public Guid Add(AudioJobRequest request, ICollection<AudioTranscodingJob> jobs)
        {
            throw new NotImplementedException();
        }
    }
}
