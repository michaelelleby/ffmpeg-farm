namespace Contract
{
    public class VideoSize
    {
        public VideoSize()
        {
            
        }

        public VideoSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; set; }

        public int Height { get; set; }

        public double AspectRatio => Width / Height;
    }
}