﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RunnersPal.Core.Repository;

#nullable disable

namespace RunnersPal.Core.Migrations
{
    [DbContext(typeof(SqliteDataContext))]
    [Migration("20241219115650_InitialModel")]
    partial class InitialModel
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("RunnersPal.Core.Models.Route", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("Creator")
                        .HasColumnType("INTEGER");

                    b.Property<decimal>("Distance")
                        .HasColumnType("TEXT");

                    b.Property<int>("DistanceUnits")
                        .HasColumnType("INTEGER");

                    b.Property<string>("MapPoints")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Notes")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ReplacesRouteId")
                        .HasColumnType("INTEGER");

                    b.Property<char>("RouteType")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("Creator");

                    b.HasIndex("ReplacesRouteId");

                    b.ToTable("Route");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.RunLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Comment")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Date")
                        .HasColumnType("TEXT");

                    b.Property<char>("LogState")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ReplacesRunLogId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("RouteId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("TimeTaken")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("UserAccountId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ReplacesRunLogId");

                    b.HasIndex("RouteId");

                    b.HasIndex("UserAccountId");

                    b.ToTable("RunLog");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.SiteSettings", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Domain")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Identifier")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("SettingValue")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("SiteSettings");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.UserAccount", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("DisplayName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("DistanceUnits")
                        .HasColumnType("INTEGER");

                    b.Property<string>("EmailAddress")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastActivityDate")
                        .HasColumnType("TEXT");

                    b.Property<string>("OriginalHostAddress")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<char>("UserType")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("UserAccount");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.UserAccountAuthentication", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("CredentialId")
                        .HasColumnType("BLOB");

                    b.Property<byte[]>("PublicKey")
                        .HasColumnType("BLOB");

                    b.Property<uint?>("SignatureCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserAccountId")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("UserHandle")
                        .HasColumnType("BLOB");

                    b.HasKey("Id");

                    b.HasIndex("UserAccountId");

                    b.ToTable("UserAccountAuthentication");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.UserPref", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("UserAccountId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("ValidTo")
                        .HasColumnType("TEXT");

                    b.Property<double?>("Weight")
                        .HasColumnType("REAL");

                    b.Property<string>("WeightUnits")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("UserAccountId");

                    b.ToTable("UserPref");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.Route", b =>
                {
                    b.HasOne("RunnersPal.Core.Models.UserAccount", "CreatorAccount")
                        .WithMany()
                        .HasForeignKey("Creator")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("RunnersPal.Core.Models.Route", "ReplacesRoute")
                        .WithMany()
                        .HasForeignKey("ReplacesRouteId");

                    b.Navigation("CreatorAccount");

                    b.Navigation("ReplacesRoute");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.RunLog", b =>
                {
                    b.HasOne("RunnersPal.Core.Models.Route", "ReplacesRunLog")
                        .WithMany()
                        .HasForeignKey("ReplacesRunLogId");

                    b.HasOne("RunnersPal.Core.Models.Route", "Route")
                        .WithMany()
                        .HasForeignKey("RouteId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("RunnersPal.Core.Models.UserAccount", "UserAccount")
                        .WithMany()
                        .HasForeignKey("UserAccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ReplacesRunLog");

                    b.Navigation("Route");

                    b.Navigation("UserAccount");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.UserAccountAuthentication", b =>
                {
                    b.HasOne("RunnersPal.Core.Models.UserAccount", "UserAccount")
                        .WithMany()
                        .HasForeignKey("UserAccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserAccount");
                });

            modelBuilder.Entity("RunnersPal.Core.Models.UserPref", b =>
                {
                    b.HasOne("RunnersPal.Core.Models.UserAccount", "UserAccount")
                        .WithMany()
                        .HasForeignKey("UserAccountId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserAccount");
                });
#pragma warning restore 612, 618
        }
    }
}
