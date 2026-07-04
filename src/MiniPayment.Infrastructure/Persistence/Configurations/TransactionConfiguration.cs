using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniPayment.Domain.Entities;

namespace MiniPayment.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.OrderNumber)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(t => t.OrderNumber)
            .IsUnique();

        builder.Property(t => t.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(t => t.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(t => t.CardBin)
            .IsRequired()
            .HasMaxLength(6);

        builder.Property(t => t.CardLast4)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(t => t.CardholderName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(t => t.ResponseCode)
            .IsRequired()
            .HasMaxLength(4);

        builder.Property(t => t.AcquirerReference)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(t => t.Timestamp)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();
    }
}
