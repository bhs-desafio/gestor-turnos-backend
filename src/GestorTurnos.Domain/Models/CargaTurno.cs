using GestorTurnos.CrossCutting.Enums;
using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{

    public class CargaTurno: AggregateRoot<Guid>
    {
        public string NombreArchivo { get; set; }
        public byte[] Archivo { get; set; }
        public DateTime Fecha { get; set; }
        public CargaEstadosEnum Estado { get; set; }

        public IEnumerable<CargaTurnoDetalle> Detalles { get; set; } = new List<CargaTurnoDetalle>();
    }
}
