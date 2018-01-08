namespace Contract
{
    public interface IProvideExamples
    {
        object GetExamples();
    }

    public class JobRequestExamples : IProvideExamples
    {
        public object GetExamples()
        {
            return new JobRequest
            {
                VideoSourceFilename = @"c:\temp\input.mxf",
                AudioSourceFilename = @"\\server\audio.wav",
                DestinationFilename = @"e:\temp\output.mp4",
                Targets = new[]
                {
                    new DestinationFormat
                    {
                        VideoBitrate = 2000,
                        Width = 1280,
                        Height = 720,
                        AudioBitrate = 128
                    },
                    new DestinationFormat
                    {
                        VideoBitrate = 1000,
                        AudioBitrate = 64,
                        Width = 1024,
                        Height = 640
                    }
                }
            };
        }
    }
}