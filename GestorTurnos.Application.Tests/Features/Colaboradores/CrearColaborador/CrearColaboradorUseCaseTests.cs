using GestorTurnos.Application.Features.Colaboradores.CrearColabarador;
using GestorTurnos.CrossCutting.Results;
using GestorTurnos.Domain.Common;
using GestorTurnos.Domain.Models;
using GestorTurnos.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestorTurnos.Application.Tests.Features.Colaboradores.CrearColaborador;

/// <summary>
/// Pruebas del caso de uso "Crear colaborador" de punta a punta dentro de la capa Application:
/// MediatR -> ValidationBehavior (validator real) -> Handler -> AutoMapper (perfil real) -> Repositorio / UnitOfWork.
/// Solo la persistencia (repositorio y unidad de trabajo) se sustituye con NSubstitute.
/// </summary>
public sealed class CrearColaboradorUseCaseTests : IDisposable
{
    private const string NombreValido = "Juan Pérez";
    private const string EmailValido = "juan.perez@correo.com";
    private const string PaisValido = "Perú";

    private readonly IColaboradorRepository _colaboradorRepository = Substitute.For<IColaboradorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly ISender _sender;

    private Colaborador? _colaboradorAgregado;

    public CrearColaboradorUseCaseTests()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddScoped(_ => _colaboradorRepository);
        services.AddScoped(_ => _unitOfWork);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        // Igual que en una petición HTTP: el caso de uso se resuelve dentro de un scope.
        _scope = _provider.CreateScope();
        _sender = _scope.ServiceProvider.GetRequiredService<ISender>();

        // Captura la entidad enviada al repositorio para verificar el mapeo.
        _colaboradorRepository
            .When(r => r.AddAsync(Arg.Any<Colaborador>(), Arg.Any<CancellationToken>()))
            .Do(call => _colaboradorAgregado = call.Arg<Colaborador>());

        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    // ---------------------------------------------------------------- OK

    [Fact]
    public async Task CrearColaborador_ComandoValido_RetornaSuccess()
    {
        var result = await _sender.Send(new CrearColaboradorCommand(NombreValido, EmailValido, PaisValido));

        Assert.True(result.IsSuccess);
        Assert.Equal(Error.None, result.Error);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public async Task CrearColaborador_ComandoValido_MapeaElComandoALaEntidadYLaAgregaAlRepositorio()
    {
        await _sender.Send(new CrearColaboradorCommand(NombreValido, EmailValido, PaisValido));

        await _colaboradorRepository.Received(1).AddAsync(Arg.Any<Colaborador>(), Arg.Any<CancellationToken>());
        Assert.NotNull(_colaboradorAgregado);
        Assert.Equal(NombreValido, _colaboradorAgregado!.Nombre);
        Assert.Equal(EmailValido, _colaboradorAgregado.Email);
        Assert.Equal(PaisValido, _colaboradorAgregado.Pais);
    }

    [Fact]
    public async Task CrearColaborador_ComandoValido_GuardaCambiosDespuesDeAgregar()
    {
        await _sender.Send(new CrearColaboradorCommand(NombreValido, EmailValido, PaisValido));

        Received.InOrder(() =>
        {
            _colaboradorRepository.AddAsync(Arg.Any<Colaborador>(), Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task CrearColaborador_ComandoValido_RetornaElIdAsignadoAlPersistir()
    {
        // Simula que la persistencia asigna el Id al guardar (como lo haría la base de datos).
        var idGenerado = Guid.NewGuid();
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                AsignarId(_colaboradorAgregado!, idGenerado);
                return 1;
            });

        var result = await _sender.Send(new CrearColaboradorCommand(NombreValido, EmailValido, PaisValido));

        Assert.True(result.IsSuccess);
        Assert.Equal(idGenerado, result.Value!.Id);
    }

    // ---------------------------------------------------------------- Error (validación)

    public static TheoryData<string?, string?, string?, string> ComandosInvalidos => new()
    {
        { "",           EmailValido,        PaisValido, "El nombre es requerido" },
        { null,         EmailValido,        PaisValido, "El nombre es requerido" },
        { " ",          EmailValido,        PaisValido, "El nombre es requerido" },
        { NombreValido, "",                 PaisValido, "El email es requerido" },
        { NombreValido, null,               PaisValido, "El email es requerido" },
        { NombreValido, "correo-invalido",  PaisValido, "El email no es válido" },
        { NombreValido, "juan.correo.com",  PaisValido, "El email no es válido" },
        { NombreValido, EmailValido,        "",         "El país es requerido" },
        { NombreValido, EmailValido,        null,       "El país es requerido" },
    };

    [Theory]
    [MemberData(nameof(ComandosInvalidos))]
    public async Task CrearColaborador_ComandoInvalido_RetornaErrorDeValidacion(
        string? nombre, string? email, string? pais, string mensajeEsperado)
    {
        var result = await _sender.Send(new CrearColaboradorCommand(nombre!, email!, pais!));

        Assert.True(result.IsFailure);
        Assert.Null(result.Value);
        Assert.Equal("Validation.Failed", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains(mensajeEsperado, result.Error.Message);
    }

    [Theory]
    [MemberData(nameof(ComandosInvalidos))]
    public async Task CrearColaborador_ComandoInvalido_NoLlegaAlHandlerNiPersiste(
        string? nombre, string? email, string? pais, string _)
    {
        await _sender.Send(new CrearColaboradorCommand(nombre!, email!, pais!));

        await _colaboradorRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task CrearColaborador_TodosLosCamposVacios_RetornaTodosLosMensajesEnUnSoloError()
    {
        var result = await _sender.Send(new CrearColaboradorCommand("", "", ""));

        Assert.True(result.IsFailure);
        Assert.Contains("El nombre es requerido", result.Error.Message);
        Assert.Contains("El email es requerido", result.Error.Message);
        Assert.Contains("El país es requerido", result.Error.Message);
    }

    // ---------------------------------------------------------------- Excepción

    [Fact]
    public async Task CrearColaborador_RepositorioLanzaExcepcion_PropagaLaExcepcionYNoGuardaCambios()
    {
        _colaboradorRepository
            .AddAsync(Arg.Any<Colaborador>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Error en repositorio"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sender.Send(new CrearColaboradorCommand(NombreValido, EmailValido, PaisValido)));

        Assert.Equal("Error en repositorio", ex.Message);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task CrearColaborador_UnitOfWorkLanzaExcepcion_PropagaLaExcepcion()
    {
        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Error al guardar"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sender.Send(new CrearColaboradorCommand(NombreValido, EmailValido, PaisValido)));

        Assert.Equal("Error al guardar", ex.Message);
        await _colaboradorRepository.Received(1).AddAsync(Arg.Any<Colaborador>(), Arg.Any<CancellationToken>());
    }

    // ---------------------------------------------------------------- Helpers

    private static void AsignarId(Colaborador colaborador, Guid id) =>
        typeof(Colaborador).GetProperty(nameof(Colaborador.Id))!.SetValue(colaborador, id);
}
