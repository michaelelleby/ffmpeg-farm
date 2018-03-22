using System;
using System.Collections.Generic;

namespace Contract
{
    public interface IScreenshotJobRepository
    {
        Guid Add(ScreenshotJobRequest request, ICollection<ScreenshotJob> jobs);
    }
}