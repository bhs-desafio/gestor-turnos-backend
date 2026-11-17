using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorTurnos.CrossCutting.Enums
{
    public class CargaEstadosEnum
    {
        public int PorProcesar = 1;
        public int Procesando = 2;
        public int Finalizado   = 3;
    }
}
