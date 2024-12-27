using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;

#nullable disable

namespace RunnersPal.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            We're moving to ef core migrations from an existing db, so we create an initial migration,
            comment this Up method out, then ef will create the migration table and populate it.
            From that point on, migrations will apply as normal.
            But for testing, we do still need the migration so do it when in debug mode.
            */
#if DEBUG
            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Domain = table.Column<string>(type: "TEXT", nullable: false),
                    Identifier = table.Column<string>(type: "TEXT", nullable: false),
                    SettingValue = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAccount",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastActivityDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EmailAddress = table.Column<string>(type: "TEXT", nullable: true),
                    OriginalHostAddress = table.Column<string>(type: "TEXT", nullable: false),
                    UserType = table.Column<char>(type: "TEXT", nullable: false),
                    DistanceUnits = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccount", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Route",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Distance = table.Column<decimal>(type: "TEXT", nullable: false),
                    DistanceUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    Creator = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RouteType = table.Column<char>(type: "TEXT", nullable: false),
                    MapPoints = table.Column<string>(type: "TEXT", nullable: true),
                    ReplacesRouteId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Route", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Route_Route_ReplacesRouteId",
                        column: x => x.ReplacesRouteId,
                        principalTable: "Route",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Route_UserAccount_Creator",
                        column: x => x.Creator,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserAccountAuthentication",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "BLOB", nullable: true),
                    PublicKey = table.Column<byte[]>(type: "BLOB", nullable: true),
                    UserHandle = table.Column<byte[]>(type: "BLOB", nullable: true),
                    SignatureCount = table.Column<uint>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAccountAuthentication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAccountAuthentication_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserPref",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Weight = table.Column<double>(type: "REAL", nullable: true),
                    WeightUnits = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPref", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPref_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RouteId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimeTaken = table.Column<string>(type: "TEXT", nullable: false),
                    UserAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: true),
                    LogState = table.Column<char>(type: "TEXT", nullable: false),
                    ReplacesRunLogId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RunLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunLog_Route_ReplacesRunLogId",
                        column: x => x.ReplacesRunLogId,
                        principalTable: "Route",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RunLog_Route_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Route",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RunLog_UserAccount_UserAccountId",
                        column: x => x.UserAccountId,
                        principalTable: "UserAccount",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Route_Creator",
                table: "Route",
                column: "Creator");

            migrationBuilder.CreateIndex(
                name: "IX_Route_ReplacesRouteId",
                table: "Route",
                column: "ReplacesRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RunLog_ReplacesRunLogId",
                table: "RunLog",
                column: "ReplacesRunLogId");

            migrationBuilder.CreateIndex(
                name: "IX_RunLog_RouteId",
                table: "RunLog",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_RunLog_UserAccountId",
                table: "RunLog",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAccountAuthentication_UserAccountId",
                table: "UserAccountAuthentication",
                column: "UserAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPref_UserAccountId",
                table: "UserPref",
                column: "UserAccountId");
            
            // create system user & system routes
            migrationBuilder.Sql(@"
insert into UserAccount(DisplayName, CreatedDate, LastActivityDate, EmailAddress, OriginalHostAddress, UserType, DistanceUnits)
select 'Admin', datetime('now'), datetime('now'), 'admin@runnerspal.com', '', 'A', 0
where not exists(select 1 from UserAccount where DisplayName = 'Admin' and UserType = 'A');");
            migrationBuilder.Sql(@"
insert into Route(Name, Distance, DistanceUnits, Creator, CreatedDate, RouteType)
select name, distance, 2, u.Id, datetime('now'), 'Z'
from (
select '1 Kilometer' as name, 1000 as distance
union select '1 Mile', 1609.344
union select '3 Miles', 3218.688
union select '5 Kilometers', 5000
union select '5 Miles', 8046.72
union select '10 Kilometers', 10000
union select '10 Miles', 16093.44
union select 'Half-marathon', 21097.5
union select 'Marathon', 42195
) distances
, UserAccount u
where u.UserType = 'A'
order by distance");
#endif
            // update the map points from the bing maps points to openstreetmap points
            migrationBuilder.Sql("update Route set MapPoints = replace(replace(MapPoints, 'latitude', 'lat'), 'longitude', 'lng') where MapPoints is not null");
            // update all route distances to be in meters
            migrationBuilder.Sql("update Route set Distance = Distance * 1000, DistanceUnits = 2 where DistanceUnits = 1");
            migrationBuilder.Sql("update Route set Distance = Distance * 1.609344 * 1000, DistanceUnits = 2 where DistanceUnits = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunLog");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropTable(
                name: "UserAccountAuthentication");

            migrationBuilder.DropTable(
                name: "UserPref");

            migrationBuilder.DropTable(
                name: "Route");

            migrationBuilder.DropTable(
                name: "UserAccount");
        }
    }
}
