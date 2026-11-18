using GestorTurnos.Application.Abstractions;
using GestorTurnos.Application.Features.Turnos.CargarTurnos;
using GestorTurnos.CrossCutting.Enums;
using GestorTurnos.CrossCutting.Results;
using GestorTurnos.Domain.Common;
using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace GestorTurnos.Application.Tests.Features.Turnos.CargarTurnos;

/// <summary>
/// Pruebas del caso de uso "Cargar turnos desde Excel" dentro de la capa Application:
/// MediatR -> ValidationBehavior (validator real) -> Handler -> AutoMapper (perfil real) -> Repositorios / UnitOfWork.
/// La lectura del Excel y la persistencia se sustituyen con NSubstitute.
/// </summary>
public sealed class CargarTurnosUseCaseTests : IDisposable
{
    private const string NombreArchivo = "turnos.xlsx";
    private static readonly byte[] Archivo = [1, 2, 3];

    private static readonly Colaborador Juan = new() { Id = Guid.NewGuid(), Nombre = "Juan", Email = "juan@correo.com", Pais = "Perú" };
    private static readonly Colaborador Ana = new() { Id = Guid.NewGuid(), Nombre = "Ana", Email = "ana@correo.com", Pais = "Chile" };

    private static readonly DateTime Dia = new(2026, 11, 20);

    private readonly ILectorExcelTurnos _lectorExcel = Substitute.For<ILectorExcelTurnos>();
    private readonly IColaboradorRepository _colaboradorRepository = Substitute.For<IColaboradorRepository>();
    private readonly ITurnoRepository _turnoRepository = Substitute.For<ITurnoRepository>();
    private readonly ICargaTurnoRepository _cargaTurnoRepository = Substitute.For<ICargaTurnoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly ISender _sender;

    private readonly List<Turno> _turnosAgregados = [];
    private CargaTurno? _cargaAgregada;

    public CargarTurnosUseCaseTests()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddScoped(_ => _lectorExcel);
        services.AddScoped(_ => _colaboradorRepository);
        services.AddScoped(_ => _turnoRepository);
        services.AddScoped(_ => _cargaTurnoRepository);
        services.AddScoped(_ => _unitOfWork);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        _scope = _provider.CreateScope();
        _sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        _colaboradorRepository
            .ObtenerPorEmailsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(call => new[] { Juan, Ana }
                .Where(c => call.Arg<IReadOnlyCollection<string>>().Contains(c.Email, StringComparer.OrdinalIgnoreCase))
                .ToList());

        TurnosExistentes();

        _turnoRepository
            .When(r => r.AddAsync(Arg.Any<Turno>(), Arg.Any<CancellationToken>()))
            .Do(call => _turnosAgregados.Add(call.Arg<Turno>()));
        _cargaTurnoRepository
            .When(r => r.AddAsync(Arg.Any<CargaTurno>(), Arg.Any<CancellationToken>()))
            .Do(call => _cargaAgregada = call.Arg<CargaTurno>());

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    // ---------------------------------------------------------------- OK

    [Fact]
    public async Task CargarTurnos_FilaValida_RegistraTurnoYDetalleExitoso()
    {
        FilasDelArchivo(Fila(2, Juan.Email, 8, 16));

        var result = await Cargar();

        Assert.True(result.IsSuccess);
        var turno = Assert.Single(_turnosAgregados);
        Assert.Equal(Juan.Id, turno.IdColaborador);
        Assert.Equal(Dia.AddHours(8), turno.HoraInicio);
        Assert.Equal(Dia.AddHours(16), turno.HoraFin);

        var detalle = Assert.Single(result.Value!.Detalles);
        Assert.True(detalle.Exitoso);
        Assert.Null(detalle.Error);
        Assert.Equal(turno.Id, detalle.IdTurno);
        Assert.Equal(2, detalle.Fila);
    }

    [Fact]
    public async Task CargarTurnos_ArchivoValido_GuardaLaCargaFinalizadaConSusDetallesEnUnSoloSaveChanges()
    {
        FilasDelArchivo(Fila(2, Juan.Email, 8, 16), Fila(3, "noexiste@correo.com", 8, 16));

        var result = await Cargar();

        Assert.NotNull(_cargaAgregada);
        Assert.Equal(result.Value!.IdCarga, _cargaAgregada!.Id);
        Assert.Equal(NombreArchivo, _cargaAgregada.NombreArchivo);
        Assert.Equal(Archivo, _cargaAgregada.Archivo);
        Assert.Equal(CargaEstadosEnum.Finalizado, _cargaAgregada.Estado);
        Assert.Equal(2, _cargaAgregada.TotalFilas);
        Assert.Equal(1, _cargaAgregada.FilasExitosas);
        Assert.Equal(1, _cargaAgregada.FilasFallidas);
        Assert.Equal(2, _cargaAgregada.Detalles.Count);
        Assert.All(_cargaAgregada.Detalles, d => Assert.Equal(_cargaAgregada.Id, d.IdCargaTurno));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CargarTurnos_EmailConOtrasMayusculas_EncuentraAlColaborador()
    {
        FilasDelArchivo(Fila(2, "  JUAN@Correo.com ", 8, 16));

        var result = await Cargar();

        Assert.True(result.Value!.Detalles.Single().Exitoso);
        Assert.Equal(Juan.Id, _turnosAgregados.Single().IdColaborador);
    }

    [Fact]
    public async Task CargarTurnos_TurnosContiguosDelMismoColaborador_AmbosSeRegistran()
    {
        FilasDelArchivo(Fila(2, Juan.Email, 8, 12), Fila(3, Juan.Email, 12, 16));

        var result = await Cargar();

        Assert.All(result.Value!.Detalles, d => Assert.True(d.Exitoso));
        Assert.Equal(2, _turnosAgregados.Count);
    }

    [Fact]
    public async Task CargarTurnos_MismoHorarioDistintoColaborador_AmbosSeRegistran()
    {
        FilasDelArchivo(Fila(2, Juan.Email, 8, 16), Fila(3, Ana.Email, 8, 16));

        var result = await Cargar();

        Assert.All(result.Value!.Detalles, d => Assert.True(d.Exitoso));
        Assert.Equal(2, _turnosAgregados.Count);
    }

    // ---------------------------------------------------------------- Filas con error

    [Fact]
    public async Task CargarTurnos_ColaboradorNoExiste_DetalleFallaConMotivo()
    {
        FilasDelArchivo(Fila(2, "noexiste@correo.com", 8, 16));

        var result = await Cargar();

        var detalle = Assert.Single(result.Value!.Detalles);
        Assert.False(detalle.Exitoso);
        Assert.Contains("No existe un colaborador con el email 'noexiste@correo.com'", detalle.Error);
        Assert.Empty(_turnosAgregados);
    }

    [Fact]
    public async Task CargarTurnos_EmailVacio_DetalleFallaConMotivo()
    {
        FilasDelArchivo(Fila(2, null, 8, 16));

        var result = await Cargar();

        Assert.Contains("El email es requerido", result.Value!.Detalles.Single().Error);
        Assert.Empty(_turnosAgregados);
    }

    [Fact]
    public async Task CargarTurnos_FechaInicioMayorOIgualQueFechaFin_DetalleFallaConMotivo()
    {
        FilasDelArchivo(Fila(2, Juan.Email, 16, 8), Fila(3, Ana.Email, 8, 8));

        var result = await Cargar();

        Assert.All(result.Value!.Detalles, d =>
        {
            Assert.False(d.Exitoso);
            Assert.Contains("La fechaInicio debe ser menor que la fechaFin", d.Error);
        });
        Assert.Empty(_turnosAgregados);
    }

    [Fact]
    public async Task CargarTurnos_ErrorDeLecturaDeLaFila_DetalleFallaConElMotivoDelLector()
    {
        FilasDelArchivo(new FilaTurnoExcel(2, Juan.Email, null, Dia.AddHours(16), "La fechaInicio 'abc' no tiene un formato de fecha válido."));

        var result = await Cargar();

        var detalle = result.Value!.Detalles.Single();
        Assert.False(detalle.Exitoso);
        Assert.Contains("La fechaInicio 'abc' no tiene un formato de fecha válido.", detalle.Error);
        Assert.Empty(_turnosAgregados);
    }

    [Fact]
    public async Task CargarTurnos_SolapaConTurnoYaRegistrado_DetalleFallaConMotivo()
    {
        TurnosExistentes(new Turno { Id = Guid.NewGuid(), IdColaborador = Juan.Id, HoraInicio = Dia.AddHours(10), HoraFin = Dia.AddHours(14) });
        FilasDelArchivo(Fila(2, Juan.Email, 8, 12));

        var result = await Cargar();

        var detalle = result.Value!.Detalles.Single();
        Assert.False(detalle.Exitoso);
        Assert.Contains("se solapa con un turno ya registrado", detalle.Error);
        Assert.Empty(_turnosAgregados);
    }

    [Fact]
    public async Task CargarTurnos_SolapaConOtraFilaDelArchivo_SoloLaPrimeraSeRegistra()
    {
        FilasDelArchivo(Fila(2, Juan.Email, 8, 16), Fila(3, Juan.Email, 15, 20));

        var result = await Cargar();

        var detalles = result.Value!.Detalles;
        Assert.True(detalles[0].Exitoso);
        Assert.False(detalles[1].Exitoso);
        Assert.Contains("se solapa con el turno de la fila 2", detalles[1].Error);
        Assert.Single(_turnosAgregados);
        Assert.Equal(1, result.Value.FilasExitosas);
        Assert.Equal(1, result.Value.FilasFallidas);
    }

    [Fact]
    public async Task CargarTurnos_FilaFallidaNoBloqueaElSolapeDeLasSiguientes()
    {
        // La fila 2 falla (colaborador inexistente), por lo que no ocupa horario para la fila 3.
        FilasDelArchivo(Fila(2, "noexiste@correo.com", 8, 16), Fila(3, Juan.Email, 8, 16));

        var result = await Cargar();

        Assert.False(result.Value!.Detalles[0].Exitoso);
        Assert.True(result.Value.Detalles[1].Exitoso);
    }

    // ---------------------------------------------------------------- Error (archivo)

    [Theory]
    [InlineData("turnos.csv", "El archivo debe tener extensión .xlsx")]
    [InlineData("", "El nombre del archivo es requerido")]
    public async Task CargarTurnos_NombreDeArchivoInvalido_RetornaErrorDeValidacion(string nombre, string mensaje)
    {
        var result = await _sender.Send(new CargarTurnosCommand(nombre, Archivo));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains(mensaje, result.Error.Message);
        _lectorExcel.DidNotReceiveWithAnyArgs().Leer(default!);
    }

    [Fact]
    public async Task CargarTurnos_ArchivoVacio_RetornaErrorDeValidacion()
    {
        var result = await _sender.Send(new CargarTurnosCommand(NombreArchivo, []));

        Assert.True(result.IsFailure);
        Assert.Contains("El archivo está vacío", result.Error.Message);
    }

    [Fact]
    public async Task CargarTurnos_LectorFalla_RetornaElErrorYNoPersiste()
    {
        var error = new Error("CargaTurno.ColumnasFaltantes", "Faltan columnas obligatorias en la cabecera: email.", ErrorType.Validation);
        _lectorExcel.Leer(Arg.Any<byte[]>()).Returns(Result<IReadOnlyList<FilaTurnoExcel>>.Failure(error));

        var result = await Cargar();

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        await _cargaTurnoRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task CargarTurnos_ArchivoSinFilas_RetornaErrorDeValidacion()
    {
        FilasDelArchivo();

        var result = await Cargar();

        Assert.True(result.IsFailure);
        Assert.Equal("CargaTurno.SinFilas", result.Error.Code);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    // ---------------------------------------------------------------- Helpers

    private Task<Result<CargarTurnosResponse>> Cargar() =>
        _sender.Send(new CargarTurnosCommand(NombreArchivo, Archivo));

    private static FilaTurnoExcel Fila(int numero, string? email, int horaInicio, int horaFin) =>
        new(numero, email, Dia.AddHours(horaInicio), Dia.AddHours(horaFin), null);

    private void FilasDelArchivo(params FilaTurnoExcel[] filas) =>
        _lectorExcel.Leer(Arg.Any<byte[]>()).Returns(Result<IReadOnlyList<FilaTurnoExcel>>.Success(filas));

    private void TurnosExistentes(params Turno[] turnos) =>
        _turnoRepository
            .ObtenerPorColaboradoresEnRangoAsync(
                Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(turnos);
}
