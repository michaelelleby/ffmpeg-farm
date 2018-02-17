namespace API.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ChangedAudioSourceFilenameInFfmpegMuxRequestToNullable : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.FfmpegMuxRequest", "AudioSourceFilename", c => c.String());
        }
        
        public override void Down()
        {
            AlterColumn("dbo.FfmpegMuxRequest", "AudioSourceFilename", c => c.String(nullable: false));
        }
    }
}
