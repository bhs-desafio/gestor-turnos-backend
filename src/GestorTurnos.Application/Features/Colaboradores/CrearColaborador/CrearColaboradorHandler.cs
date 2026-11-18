using AutoMapper;
using GestorTurnos.Application.Common;
using GestorTurnos.CrossCutting.Results;
using GestorTurnos.Domain.Common;
using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace GestorTurnos.Application.Features.Colaboradores.CrearColabarador
{
    public sealed class CrearColaboradorHandler :
            ICommandHandler<CrearColaboradorCommand, CrearColaboradorResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IColaboradorRepository _colaboradorRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<CrearColaboradorHandler> _logger;

        public CrearColaboradorHandler(
            IUnitOfWork unitOfWork,
            IColaboradorRepository colaboradorRepository,
            IMapper mapper,
            ILogger<CrearColaboradorHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _colaboradorRepository = colaboradorRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CrearColaboradorResponse>> Handle(CrearColaboradorCommand request, CancellationToken cancellationToken)
        {
            var colaborador = _mapper.Map<Colaborador>(request);
            colaborador.Id = Guid.NewGuid(); // Generar un nuevo Id para el colaborador
            await _colaboradorRepository.AddAsync(colaborador);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Colaborador creado con éxito. Id: {Id}", colaborador.Id);

            return await Task.FromResult(Result<CrearColaboradorResponse>.Success(new CrearColaboradorResponse(colaborador.Id)));
        }
    }
}
