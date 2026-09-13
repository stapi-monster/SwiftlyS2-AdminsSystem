using FluentMigrator;

namespace Admins.Core.Database.Migrations;

[Migration(20270214103912)]
public class Admins_AddGroupsTable : Migration
{
    public override void Up()
    {
        if (!Schema.Table("as_groups").Exists())
        {
            Create.Table("as_groups")
            .WithColumn("id").AsInt64().PrimaryKey().Identity().NotNullable()
            .WithColumn("name").AsString(100).NotNullable()
            .WithColumn("flags").AsString(255).NotNullable()
            .WithColumn("immunity").AsInt32().NotNullable().WithDefaultValue(0);
        }
    }

    public override void Down()
    {
        if (Schema.Table("as_groups").Exists())
        {
            Delete.Table("as_groups");
        }
    }
}