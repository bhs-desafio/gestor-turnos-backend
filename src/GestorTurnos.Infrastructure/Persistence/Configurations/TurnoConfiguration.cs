using GestorTurnos.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestorTurnos.Infrastructure.Persistence.Configurations;

public class TurnoConfiguration : IEntityTypeConfiguration<Turno>
{
    public void Configure(EntityTypeBuilder<Turno> builder)
    {
        builder.ToTable("Turno");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasColumnName("Id").ValueGeneratedNever();

        builder.Property(t => t.IdColaborador).IsRequired();
        builder.Property(t => t.HoraInicio).IsRequired();
        builder.Property(t => t.HoraFin).IsRequired();

        builder.HasOne<Colaborador>()
            .WithMany()
            .HasForeignKey(t => t.IdColaborador)
            .OnDelete(DeleteBehavior.Restrict);

        // Acelera la búsqueda de solapamientos por colaborador y rango.
        builder.HasIndex(t => new { t.IdColaborador, t.HoraInicio, t.HoraFin });
    }
}
