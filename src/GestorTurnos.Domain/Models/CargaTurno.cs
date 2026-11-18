using GestorTurnos.CrossCutting.Enums;
using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{

    public class CargaTurno: AggregateRoot<Guid>
    {
        public string NombreArchivo { get; set; } = string.Empty;
        public byte[] Archivo { get; set; } = [];
        public DateTime Fecha { get; set; }
        public CargaEstadosEnum Estado { get; set; }
        public int TotalFilas { get; set; }
        public int FilasExitosas { get; set; }
        public int FilasFallidas { get; set; }

        public ICollection<CargaTurnoDetalle> Detalles { get; set; } = new List<CargaTurnoDetalle>();
    }
}
