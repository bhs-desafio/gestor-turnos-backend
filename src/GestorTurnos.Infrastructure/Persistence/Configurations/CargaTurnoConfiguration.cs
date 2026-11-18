using GestorTurnos.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorTurnos.Infrastructure.Persistence.Configurations;

public class CargaTurnoConfiguration : IEntityTypeConfiguration<CargaTurno>
{
    public void Configure(EntityTypeBuilder<CargaTurno> builder)
    {
        builder.ToTable("CargaTurno");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("Id").ValueGeneratedNever();

        builder.Property(c => c.NombreArchivo).HasMaxLength(255).IsRequired();
        builder.Property(c => c.Archivo).IsRequired();
        builder.Property(c => c.Fecha).IsRequired();
        builder.Property(c => c.Estado).HasConversion<int>().IsRequired();

        builder.HasMany(c => c.Detalles)
            .WithOne()
            .HasForeignKey(d => d.IdCargaTurno)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
