namespace API.Database.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRowVersionToFfmpegTasksTable : DbMigration
    {
        public override void Up()
        {
            DropColumn("dbo.FfmpegTasks", "RowVersion");
        }
        
        public override void Down()
        {
            AddColumn("dbo.FfmpegTasks", "RowVersion", c => c.Binary(nullable: false, fixedLength: true, timestamp: true, storeType: "rowversion"));
        }
    }
}
