using GestorTurnos.CrossCutting.Enums;
using GestorTurnos.Domain.Common;

namespace GestorTurnos.Domain.Models
{

    public class CargaTurno: AggregateRoot<Guid>
    {
        public string NombreArchivo { get; private set; }
        public byte[] Archivo { get; private set; }
        public DateTime Fecha { get; private set; }
        public CargaEstadosEnum Estado { get; private set; }

        public IEnumerable<CargaTurnoDetalle> Detalles { get; private set; } = new List<CargaTurnoDetalle>();
    }
}
