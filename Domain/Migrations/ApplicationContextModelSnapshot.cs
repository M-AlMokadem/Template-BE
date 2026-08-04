using System;
using Domain.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Domain.Migrations
{
    [DbContext(typeof(ApplicationContext))]
    partial class ApplicationContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Domain.Models.ApplicationRole", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<Guid?>("CreatedBy")
                    .HasColumnType("uuid");

                b.Property<DateTime>("CreatedOn")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Description")
                    .HasColumnType("text");

                b.Property<bool>("IsActive")
                    .HasColumnType("boolean");

                b.Property<bool>("IsDeleted")
                    .HasColumnType("boolean");

                b.Property<Guid?>("ModifiedBy")
                    .HasColumnType("uuid");

                b.Property<DateTime?>("ModifiedOn")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Name")
                    .IsRequired()
                    .HasColumnType("text");

                b.HasKey("Id");

                b.ToTable("ApplicationRoles");
            });

            modelBuilder.Entity("Domain.Models.ApplicationUser", b =>
            {
                b.Property<Guid>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("uuid");

                b.Property<Guid?>("CreatedBy")
                    .HasColumnType("uuid");

                b.Property<DateTime>("CreatedOn")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("Email")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<bool>("IsActive")
                    .HasColumnType("boolean");

                b.Property<bool>("IsDeleted")
                    .HasColumnType("boolean");

                b.Property<Guid?>("ModifiedBy")
                    .HasColumnType("uuid");

                b.Property<DateTime?>("ModifiedOn")
                    .HasColumnType("timestamp with time zone");

                b.Property<string>("PasswordHash")
                    .IsRequired()
                    .HasColumnType("text");

                b.Property<string>("UserName")
                    .IsRequired()
                    .HasColumnType("text");

                b.HasKey("Id");

                b.ToTable("ApplicationUsers");
            });
#pragma warning restore 612, 618
        }
    }
}
