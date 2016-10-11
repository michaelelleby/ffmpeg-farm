using System;
using System.Collections.Generic;
using Contract.Dto;

namespace Contract
{
    public interface IAudioJobRepository : IJobRepository
    {
        Guid Add(AudioJobRequest request, ICollection<AudioTranscodingJob> jobs);
        AudioJobRequestDto Get(Guid id);
        IEnumerable<AudioJobRequestDto> Get();
    }
}