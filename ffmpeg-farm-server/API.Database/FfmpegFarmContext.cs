using System.Data.Common;

namespace API.Database
{
    using System.Data.Entity;

    public partial class FfmpegFarmContext : DbContext
    {
        public FfmpegFarmContext() : base("name=FfmpegFarmContext")
        {
            Configuration.LazyLoadingEnabled = false;
        }

        public FfmpegFarmContext(DbConnection connection) : base(connection, true)
        {
            Configuration.LazyLoadingEnabled = false;
        }

        public virtual DbSet<Clients> Clients { get; set; }
        public virtual DbSet<FfmpegAudioRequest> AudioRequests { get; set; }
        public virtual DbSet<FfmpegAudioRequestTargets> AudioRequestTargets { get; set; }
        public virtual DbSet<FfmpegHardSubtitlesRequest> HardSubtitlesRequest { get; set; }
        public virtual DbSet<FfmpegJobs> Jobs { get; set; }
        public virtual DbSet<FfmpegMuxRequest> MuxRequests { get; set; }
        public virtual DbSet<FfmpegTasks> Tasks { get; set; }
        public virtual DbSet<FfmpegVideoJobs> VideoJobs { get; set; }
        public virtual DbSet<FfmpegVideoMergeJobs> VideoMergeJobs { get; set; }
        public virtual DbSet<FfmpegVideoParts> VideoParts { get; set; }
        public virtual DbSet<FfmpegVideoRequest> VideoRequest { get; set; }
        public virtual DbSet<FfmpegVideoRequestTargets> VideoRequestTargets { get; set; }
        public virtual DbSet<Log> Log { get; set; }
        public virtual DbSet<Mp4boxJobs> Mp4boxJobs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FfmpegAudioRequest>()
                .Property(e => e.OutputFolder)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegAudioRequestTargets>()
                .Property(e => e.Codec)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegAudioRequestTargets>()
                .Property(e => e.Format)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegJobs>()
                .HasMany(e => e.FfmpegTasks)
                .WithRequired(e => e.Job)
                .HasForeignKey(e => e.FfmpegJobs_id)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FfmpegTasks>()
                .Property(e => e.HeartbeatMachineName)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegVideoJobs>()
                .Property(e => e.State)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegVideoMergeJobs>()
                .Property(e => e.State)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegVideoRequest>()
                .Property(e => e.Inpoint)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegVideoRequestTargets>()
                .Property(e => e.H264Profile)
                .IsUnicode(false);

            modelBuilder.Entity<FfmpegVideoRequestTargets>()
                .Property(e => e.H264Level)
                .IsUnicode(false);

            modelBuilder.Entity<Mp4boxJobs>()
                .Property(e => e.State)
                .IsUnicode(false);
        }
    }
}
