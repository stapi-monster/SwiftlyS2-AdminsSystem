using FluentMigrator;

namespace Admins.Comms.Database.Migrations;

[Migration(20270214103917)]
public class Admins_AddSanctionsTable : Migration
{
    public override void Up()
    {
        if (!Schema.Table("as_mutes").Exists())
        {
            Create.Table("as_mutes")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("steamid").AsInt64().NotNullable()
            .WithColumn("name").AsString(64).NotNullable()
            .WithColumn("ip").AsString(45).NotNullable()
            .WithColumn("sanction_type").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("type").AsInt32().NotNullable().WithDefaultValue(0)
            .WithColumn("ends").AsInt64().NotNullable().WithDefaultValue(0)
            .WithColumn("duration").AsInt64().NotNullable().WithDefaultValue(0)
            .WithColumn("reason").AsString(255).NotNullable()
            .WithColumn("admin_steamid").AsInt64().NotNullable()
            .WithColumn("admin_name").AsString(64).NotNullable()
            .WithColumn("server").AsString(64).NotNullable()
            .WithColumn("global_sanction").AsBoolean().NotNullable().WithDefaultValue(false)
            .WithColumn("created").AsInt64().NotNullable().WithDefaultValue(0)
            .WithColumn("updated").AsInt64().NotNullable().WithDefaultValue(0);
        }
    }

    public override void Down()
    {
        if (Schema.Table("as_mutes").Exists())
        {
            Delete.Table("as_mutes");
        }
    }
}