using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorTurnos.Application.Features.Colaboradores.CrearColabarador
{
    public sealed class CrearColaboradorValidator : AbstractValidator<CrearColaboradorCommand>
    {
        public CrearColaboradorValidator()
        {
            RuleFor(x => x.Nombre).NotEmpty().WithMessage("El nombre es requerido");
            RuleFor(x => x.Email).NotEmpty().WithMessage("El email es requerido")
                     .EmailAddress().WithMessage("El email no es válido");
            RuleFor(x => x.Pais).NotEmpty().WithMessage("El país es requerido");
        }
    }
}
