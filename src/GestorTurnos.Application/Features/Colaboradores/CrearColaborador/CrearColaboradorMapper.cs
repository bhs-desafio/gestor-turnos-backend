using AutoMapper;
using GestorTurnos.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorTurnos.Application.Features.Colaboradores.CrearColabarador
{
    public sealed class CrearColaboradorMapper:Profile
    {
        public CrearColaboradorMapper()
        {
            CreateMap<CrearColaboradorCommand, Colaborador>();
        }
    }
}
