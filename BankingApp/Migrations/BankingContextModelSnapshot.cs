using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using BankingApp.Models;
using BankingApp.Enums;

namespace BankingApp.Migrations
{
    [DbContext(typeof(BankingContext))]
    partial class BankingContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.1")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("BankingApp.Model.TransactionInfo", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<double>("Amount");

                    b.Property<DateTime>("TransactionDate");

                    b.Property<int>("TransactionType");

                    b.Property<Guid>("UserAccountId");

                    b.HasKey("Id");

                    b.HasIndex("UserAccountId");

                    b.ToTable("Transactions");
                });

            modelBuilder.Entity("BankingApp.Model.UserAccount", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("AccountNumber")
                        .IsRequired()
                        .HasMaxLength(20);

                    b.Property<DateTime>("CreateDate");

                    b.HasKey("Id");

                    b.ToTable("UserAccounts");
                });

            modelBuilder.Entity("BankingApp.Model.TransactionInfo", b =>
                {
                    b.HasOne("BankingApp.Model.UserAccount", "UserAccount")
                        .WithMany("Transactions")
                        .HasForeignKey("UserAccountId")
                        .OnDelete(DeleteBehavior.Cascade);
                });
        }
    }
}
