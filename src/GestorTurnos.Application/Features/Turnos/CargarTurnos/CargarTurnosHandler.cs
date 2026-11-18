using AutoMapper;
using GestorTurnos.Application.Abstractions;
using GestorTurnos.Application.Common;
using GestorTurnos.CrossCutting.Enums;
using GestorTurnos.CrossCutting.Results;
using GestorTurnos.Domain.Common;
using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace GestorTurnos.Application.Features.Turnos.CargarTurnos;

public sealed class CargarTurnosHandler :
        ICommandHandler<CargarTurnosCommand, CargarTurnosResponse>
{
    private const string FormatoFecha = "dd/MM/yyyy HH:mm";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICargaTurnoRepository _cargaTurnoRepository;
    private readonly ITurnoRepository _turnoRepository;
    private readonly IColaboradorRepository _colaboradorRepository;
    private readonly ILectorExcelTurnos _lectorExcel;
    private readonly IMapper _mapper;
    private readonly ILogger<CargarTurnosHandler> _logger;

    public CargarTurnosHandler(
        IUnitOfWork unitOfWork,
        ICargaTurnoRepository cargaTurnoRepository,
        ITurnoRepository turnoRepository,
        IColaboradorRepository colaboradorRepository,
        ILectorExcelTurnos lectorExcel,
        IMapper mapper,
        ILogger<CargarTurnosHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cargaTurnoRepository = cargaTurnoRepository;
        _turnoRepository = turnoRepository;
        _colaboradorRepository = colaboradorRepository;
        _lectorExcel = lectorExcel;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<CargarTurnosResponse>> Handle(CargarTurnosCommand request, CancellationToken cancellationToken)
    {
        var lectura = _lectorExcel.Leer(request.Archivo);
        if (lectura.IsFailure)
            return Result<CargarTurnosResponse>.Failure(lectura.Error);

        var filas = lectura.Value!;
        if (filas.Count == 0)
            return Result<CargarTurnosResponse>.Failure(
                new Error("CargaTurno.SinFilas", "El archivo no contiene filas para procesar.", ErrorType.Validation));

        var carga = new CargaTurno
        {
            Id = Guid.NewGuid(),
            NombreArchivo = request.NombreArchivo,
            Archivo = request.Archivo,
            Fecha = DateTime.Now,
            Estado = CargaEstadosEnum.Procesando
        };

        var colaboradoresPorEmail = await ObtenerColaboradoresPorEmailAsync(filas, cancellationToken);
        var turnosPorColaborador = await ObtenerTurnosExistentesAsync(filas, colaboradoresPorEmail, cancellationToken);

        foreach (var fila in filas.OrderBy(f => f.Fila))
        {
            var detalle = new CargaTurnoDetalle
            {
                Id = Guid.NewGuid(),
                IdCargaTurno = carga.Id,
                Fila = fila.Fila,
                Email = fila.Email?.Trim(),
                HoraInicio = fila.FechaInicio,
                HoraFin = fila.FechaFin
            };

            colaboradoresPorEmail.TryGetValue(detalle.Email ?? string.Empty, out var colaborador);
            detalle.IdColaborador = colaborador?.Id;

            var error = ValidarFila(fila, colaborador, turnosPorColaborador);
            if (error is null)
            {
                var turno = new Turno
                {
                    Id = Guid.NewGuid(),
                    IdColaborador = colaborador!.Id,
                    HoraInicio = fila.FechaInicio!.Value,
                    HoraFin = fila.FechaFin!.Value
                };
                await _turnoRepository.AddAsync(turno, cancellationToken);
                ObtenerTurnos(turnosPorColaborador, colaborador.Id)
                    .Add(new TurnoOcupado(turno.HoraInicio, turno.HoraFin, fila.Fila));

                detalle.Exitoso = true;
                detalle.IdTurno = turno.Id;
            }
            else
            {
                detalle.Exitoso = false;
                detalle.Error = error;
            }

            carga.Detalles.Add(detalle);
        }

        carga.TotalFilas = carga.Detalles.Count;
        carga.FilasExitosas = carga.Detalles.Count(d => d.Exitoso);
        carga.FilasFallidas = carga.TotalFilas - carga.FilasExitosas;
        carga.Estado = CargaEstadosEnum.Finalizado;

        await _cargaTurnoRepository.AddAsync(carga, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Carga de turnos {Id} finalizada. Archivo: {Archivo}. Filas: {Total}, exitosas: {Exitosas}, fallidas: {Fallidas}",
            carga.Id, carga.NombreArchivo, carga.TotalFilas, carga.FilasExitosas, carga.FilasFallidas);

        return Result<CargarTurnosResponse>.Success(new CargarTurnosResponse(
            carga.Id,
            carga.TotalFilas,
            carga.FilasExitosas,
            carga.FilasFallidas,
            _mapper.Map<List<CargarTurnosDetalleResponse>>(carga.Detalles)));
    }

    private static string? ValidarFila(
        FilaTurnoExcel fila,
        Colaborador? colaborador,
        Dictionary<Guid, List<TurnoOcupado>> turnosPorColaborador)
    {
        var errores = new List<string>();

        if (string.IsNullOrWhiteSpace(fila.Email))
            errores.Add("El email es requerido.");
        else if (colaborador is null)
            errores.Add($"No existe un colaborador con el email '{fila.Email.Trim()}'.");

        if (!string.IsNullOrWhiteSpace(fila.ErrorLectura))
            errores.Add(fila.ErrorLectura);

        if (fila.FechaInicio >= fila.FechaFin)
            errores.Add("La fechaInicio debe ser menor que la fechaFin.");

        if (errores.Count > 0)
            return string.Join(" ", errores);

        // Turno que se solapa con otro existente.
        var solapado = ObtenerTurnos(turnosPorColaborador, colaborador!.Id)
            .FirstOrDefault(t => fila.FechaInicio!.Value < t.Fin && fila.FechaFin!.Value > t.Inicio);

        if (solapado is null)
            return null;

        var rango = $"{solapado.Inicio.ToString(FormatoFecha)} - {solapado.Fin.ToString(FormatoFecha)}";
        return solapado.Fila is { } filaOrigen
            ? $"El turno se solapa con el turno de la fila {filaOrigen} del archivo ({rango})."
            : $"El turno se solapa con un turno ya registrado del colaborador ({rango}).";
    }

    private async Task<Dictionary<string, Colaborador>> ObtenerColaboradoresPorEmailAsync(
        IReadOnlyList<FilaTurnoExcel> filas,
        CancellationToken cancellationToken)
    {
        var emails = filas
            .Where(f => !string.IsNullOrWhiteSpace(f.Email))
            .Select(f => f.Email!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (emails.Count == 0)
            return new Dictionary<string, Colaborador>(StringComparer.OrdinalIgnoreCase);

        var colaboradores = await _colaboradorRepository.ObtenerPorEmailsAsync(emails, cancellationToken);

        return colaboradores
            .GroupBy(c => c.Email.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Dictionary<Guid, List<TurnoOcupado>>> ObtenerTurnosExistentesAsync(
        IReadOnlyList<FilaTurnoExcel> filas,
        Dictionary<string, Colaborador> colaboradoresPorEmail,
        CancellationToken cancellationToken)
    {
        var resultado = new Dictionary<Guid, List<TurnoOcupado>>();

        var conFechas = filas
            .Where(f => f.FechaInicio.HasValue && f.FechaFin.HasValue)
            .ToList();

        if (colaboradoresPorEmail.Count == 0 || conFechas.Count == 0)
            return resultado;

        var ids = colaboradoresPorEmail.Values.Select(c => c.Id).Distinct().ToList();
        var desde = conFechas.Min(f => f.FechaInicio!.Value);
        var hasta = conFechas.Max(f => f.FechaFin!.Value);

        var existentes = await _turnoRepository.ObtenerPorColaboradoresEnRangoAsync(ids, desde, hasta, cancellationToken);

        foreach (var turno in existentes)
            ObtenerTurnos(resultado, turno.IdColaborador).Add(new TurnoOcupado(turno.HoraInicio, turno.HoraFin, null));

        return resultado;
    }

    private static List<TurnoOcupado> ObtenerTurnos(Dictionary<Guid, List<TurnoOcupado>> turnos, Guid idColaborador)
    {
        if (!turnos.TryGetValue(idColaborador, out var lista))
        {
            lista = [];
            turnos[idColaborador] = lista;
        }
        return lista;
    }

    private sealed record TurnoOcupado(DateTime Inicio, DateTime Fin, int? Fila);
}
