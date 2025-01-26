﻿// <auto-generated />
using System;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20250126220115_AddIsAdultOnlyProperty")]
    partial class AddIsAdultOnlyProperty
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Domain.Entities.Cafeteria", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("City")
                        .HasColumnType("int");

                    b.Property<int>("Location")
                        .HasColumnType("int");

                    b.Property<bool>("OffersHotMeals")
                        .HasColumnType("bit");

                    b.HasKey("Id");

                    b.ToTable("Cafeterias");
                });

            modelBuilder.Entity("Domain.Entities.Package", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("CafeteriaId")
                        .HasColumnType("int");

                    b.Property<int>("CafeteriaLocation")
                        .HasColumnType("int");

                    b.Property<int>("City")
                        .HasColumnType("int");

                    b.Property<bool>("IsAdultOnly")
                        .HasColumnType("bit");

                    b.Property<DateTime>("LastReservationDateTime")
                        .HasColumnType("datetime2");

                    b.Property<int>("MealType")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("PickupDateTime")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(18,2)");

                    b.HasKey("Id");

                    b.HasIndex("CafeteriaId");

                    b.ToTable("Packages");
                });

            modelBuilder.Entity("Domain.Entities.Product", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsAlcoholic")
                        .HasColumnType("bit");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("PhotoUrl")
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("Id");

                    b.ToTable("Products");
                });

            modelBuilder.Entity("Domain.Entities.Reservation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsNoShow")
                        .HasColumnType("bit");

                    b.Property<bool>("IsPickedUp")
                        .HasColumnType("bit");

                    b.Property<int>("PackageId")
                        .HasColumnType("int");

                    b.Property<DateTime>("ReservationDateTime")
                        .HasColumnType("datetime2");

                    b.Property<string>("StudentNumber")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.HasKey("Id");

                    b.HasIndex("PackageId")
                        .IsUnique();

                    b.HasIndex("StudentNumber");

                    b.ToTable("Reservations");
                });

            modelBuilder.Entity("Domain.Entities.Student", b =>
                {
                    b.Property<string>("StudentNumber")
                        .HasColumnType("nvarchar(450)");

                    b.Property<DateTime>("DateOfBirth")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("FirstName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<string>("IdentityId")
                        .IsRequired()
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("LastName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("NoShowCount")
                        .HasColumnType("int");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("nvarchar(max)");

                    b.Property<int>("StudyCity")
                        .HasColumnType("int");

                    b.HasKey("StudentNumber");

                    b.HasIndex("Email")
                        .IsUnique();

                    b.HasIndex("IdentityId")
                        .IsUnique();

                    b.ToTable("Students");
                });

            modelBuilder.Entity("PackageProduct", b =>
                {
                    b.Property<int>("PackagesId")
                        .HasColumnType("int");

                    b.Property<int>("ProductsId")
                        .HasColumnType("int");

                    b.HasKey("PackagesId", "ProductsId");

                    b.HasIndex("ProductsId");

                    b.ToTable("PackageProduct");
                });

            modelBuilder.Entity("Domain.Entities.Package", b =>
                {
                    b.HasOne("Domain.Entities.Cafeteria", "Cafeteria")
                        .WithMany("Packages")
                        .HasForeignKey("CafeteriaId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Cafeteria");
                });

            modelBuilder.Entity("Domain.Entities.Reservation", b =>
                {
                    b.HasOne("Domain.Entities.Package", "Package")
                        .WithOne("Reservation")
                        .HasForeignKey("Domain.Entities.Reservation", "PackageId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("Domain.Entities.Student", "Student")
                        .WithMany("Reservations")
                        .HasForeignKey("StudentNumber")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Package");

                    b.Navigation("Student");
                });

            modelBuilder.Entity("PackageProduct", b =>
                {
                    b.HasOne("Domain.Entities.Package", null)
                        .WithMany()
                        .HasForeignKey("PackagesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Domain.Entities.Product", null)
                        .WithMany()
                        .HasForeignKey("ProductsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Cafeteria", b =>
                {
                    b.Navigation("Packages");
                });

            modelBuilder.Entity("Domain.Entities.Package", b =>
                {
                    b.Navigation("Reservation");
                });

            modelBuilder.Entity("Domain.Entities.Student", b =>
                {
                    b.Navigation("Reservations");
                });
#pragma warning restore 612, 618
        }
    }
}
