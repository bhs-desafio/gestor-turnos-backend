using GestorTurnos.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorTurnos.Application.Features.Colaboradores.CrearColabarador;

public sealed record CrearColaboradorCommand(string Nombre,string Email, string Pais) 
            : ICommand<CrearColaboradorResponse>;