namespace API.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddVersionColumnToFfmpegTasksTable : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.FfmpegTasks", "VersionColumn", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.FfmpegTasks", "VersionColumn");
        }
    }
}
