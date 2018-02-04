using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IOldAudioJobRepository : IOldJobRepository
    {
        Guid Add(AudioJobRequest request, ICollection<AudioTranscodingJob> jobs);
    }
}