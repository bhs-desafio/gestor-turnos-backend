using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GestorTurnos.Infrastructure.Persistence.Repositories
{
    public class ColaboradorRepository: Repository<Colaborador, Guid>, IColaboradorRepository
    {

        public ColaboradorRepository(GestorTurnosDbContext dbContext) : base(dbContext)
        {
        }
    }
}
