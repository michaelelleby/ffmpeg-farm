using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IAudioJobRepository : IJobRepository
    {
        Guid Add(AudioJobRequest request, ICollection<AudioTranscodingJob> jobs);
        AudioTranscodingJob GetNextTranscodingJob();
        AudioJobRequestDto Get(Guid id);
        IEnumerable<AudioJobRequestDto> Get();
    }
}