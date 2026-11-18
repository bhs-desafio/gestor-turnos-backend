using GestorTurnos.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorTurnos.Infrastructure.Persistence.Configurations;

public class CargaTurnoDetalleConfiguration : IEntityTypeConfiguration<CargaTurnoDetalle>
{
    public void Configure(EntityTypeBuilder<CargaTurnoDetalle> builder)
    {
        builder.ToTable("CargaTurnoDetalle");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("Id").ValueGeneratedNever();

        builder.Property(d => d.Fila).IsRequired();
        builder.Property(d => d.Email).HasMaxLength(100);
        builder.Property(d => d.Exitoso).IsRequired();
        builder.Property(d => d.Error).HasMaxLength(1000);

        builder.HasOne<Colaborador>()
            .WithMany()
            .HasForeignKey(d => d.IdColaborador)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Turno>()
            .WithMany()
            .HasForeignKey(d => d.IdTurno)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
