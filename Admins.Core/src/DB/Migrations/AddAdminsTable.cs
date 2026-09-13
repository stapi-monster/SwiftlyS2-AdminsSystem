using FluentMigrator;

namespace Admins.Core.Database.Migrations;

[Migration(20270214103913)]
public class Admins_AddAdminsTable : Migration
{
    public override void Up()
    {
        if (!Schema.Table("as_admins").Exists())
        {
            Create.Table("as_admins")
            .WithColumn("id").AsInt64().PrimaryKey().Identity().NotNullable()
            .WithColumn("steamid").AsInt64().NotNullable()
            .WithColumn("name").AsString(64).NotNullable()
            .WithColumn("flags").AsString(255).NotNullable()
            .WithColumn("immunity").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("end").AsInt64().NotNullable().WithDefaultValue(0);
        }
    }

    public override void Down()
    {
        if (Schema.Table("as_admins").Exists())
        {
            Delete.Table("as_admins");
        }
    }
}