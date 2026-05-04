using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NuReaper.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DependencyGraph",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RootPackage = table.Column<string>(type: "TEXT", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalThreatLevel = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DependencyGraph", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VersionRange",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MinVersion = table.Column<string>(type: "TEXT", nullable: true),
                    MaxVersion = table.Column<string>(type: "TEXT", nullable: true),
                    IsMinInclusive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsMaxInclusive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionRange", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cycle",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    DependencyGraphId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cycle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cycle_DependencyGraph_DependencyGraphId",
                        column: x => x.DependencyGraphId,
                        principalTable: "DependencyGraph",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GraphEdge",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FromId = table.Column<string>(type: "TEXT", nullable: false),
                    ToId = table.Column<string>(type: "TEXT", nullable: false),
                    DependencyName = table.Column<string>(type: "TEXT", nullable: false),
                    DependencyVersion = table.Column<string>(type: "TEXT", nullable: false),
                    TargetFramework = table.Column<string>(type: "TEXT", nullable: true),
                    DependencyGraphId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphEdge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GraphEdge_DependencyGraph_DependencyGraphId",
                        column: x => x.DependencyGraphId,
                        principalTable: "DependencyGraph",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GraphNode",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Depth = table.Column<int>(type: "INTEGER", nullable: false),
                    DependencyGraphId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GraphNode", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GraphNode_DependencyGraph_DependencyGraphId",
                        column: x => x.DependencyGraphId,
                        principalTable: "DependencyGraph",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PackageName = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    Author = table.Column<string>(type: "TEXT", nullable: false),
                    Sha256Hash = table.Column<string>(type: "TEXT", nullable: false),
                    Downloads = table.Column<long>(type: "INTEGER", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LastScanDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastScanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DependencyGraphId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_DependencyGraph_DependencyGraphId",
                        column: x => x.DependencyGraphId,
                        principalTable: "DependencyGraph",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PackageDependencies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PackageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    TargetFramework = table.Column<string>(type: "TEXT", nullable: true),
                    VersionRangeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    IsTransitive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageDependencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageDependencies_VersionRange_VersionRangeId",
                        column: x => x.VersionRangeId,
                        principalTable: "VersionRange",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Scans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PackageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    ScanDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ThreatLevel = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scans_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cycle_DependencyGraphId",
                table: "Cycle",
                column: "DependencyGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphEdge_DependencyGraphId",
                table: "GraphEdge",
                column: "DependencyGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_GraphNode_DependencyGraphId",
                table: "GraphNode",
                column: "DependencyGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageDependencies_VersionRangeId",
                table: "PackageDependencies",
                column: "VersionRangeId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_DependencyGraphId",
                table: "Packages",
                column: "DependencyGraphId");

            migrationBuilder.CreateIndex(
                name: "IX_Scans_PackageId",
                table: "Scans",
                column: "PackageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cycle");

            migrationBuilder.DropTable(
                name: "GraphEdge");

            migrationBuilder.DropTable(
                name: "GraphNode");

            migrationBuilder.DropTable(
                name: "PackageDependencies");

            migrationBuilder.DropTable(
                name: "Scans");

            migrationBuilder.DropTable(
                name: "VersionRange");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "DependencyGraph");
        }
    }
}
