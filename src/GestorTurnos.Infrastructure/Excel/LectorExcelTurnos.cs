using System.Globalization;
using ClosedXML.Excel;
using GestorTurnos.Application.Abstractions;
using GestorTurnos.CrossCutting.Results;

namespace GestorTurnos.Infrastructure.Excel;

/// <summary>
/// Lee el Excel de turnos con ClosedXML. Usa la primera hoja; la fila 1 es la cabecera y las columnas
/// se ubican por nombre (email, fechaInicio, fechaFin).
/// </summary>
public sealed class LectorExcelTurnos : ILectorExcelTurnos
{
    private const string ColumnaEmail = "email";
    private const string ColumnaFechaInicio = "fechaInicio";
    private const string ColumnaFechaFin = "fechaFin";

    private static readonly string[] FormatosFecha =
    [
        "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy H:mm", "d/M/yyyy H:mm", "dd/MM/yyyy",
        "yyyy-MM-dd HH:mm", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd"
    ];

    public Result<IReadOnlyList<FilaTurnoExcel>> Leer(byte[] archivo)
    {
        XLWorkbook libro;
        try
        {
            libro = new XLWorkbook(new MemoryStream(archivo));
        }
        catch (Exception)
        {
            return Result<IReadOnlyList<FilaTurnoExcel>>.Failure(
                new Error("CargaTurno.ArchivoInvalido", "El archivo no es un .xlsx válido.", ErrorType.Validation));
        }

        using (libro)
        {
            var hoja = libro.Worksheets.FirstOrDefault();
            var rangoUsado = hoja?.RangeUsed();
            if (hoja is null || rangoUsado is null)
                return Result<IReadOnlyList<FilaTurnoExcel>>.Success([]);

            var columnas = hoja.Row(1).CellsUsed()
                .GroupBy(c => Normalizar(c.GetString()))
                .ToDictionary(g => g.Key, g => g.First().Address.ColumnNumber);

            var faltantes = new[] { ColumnaEmail, ColumnaFechaInicio, ColumnaFechaFin }
                .Where(nombre => !columnas.ContainsKey(Normalizar(nombre)))
                .ToList();

            if (faltantes.Count > 0)
                return Result<IReadOnlyList<FilaTurnoExcel>>.Failure(new Error(
                    "CargaTurno.ColumnasFaltantes",
                    $"Faltan columnas obligatorias en la cabecera: {string.Join(", ", faltantes)}.",
                    ErrorType.Validation));

            var colEmail = columnas[Normalizar(ColumnaEmail)];
            var colInicio = columnas[Normalizar(ColumnaFechaInicio)];
            var colFin = columnas[Normalizar(ColumnaFechaFin)];

            var filas = new List<FilaTurnoExcel>();
            var ultimaFila = rangoUsado.LastRow().RowNumber();

            for (var numeroFila = 2; numeroFila <= ultimaFila; numeroFila++)
            {
                var fila = hoja.Row(numeroFila);
                var celdaEmail = fila.Cell(colEmail);
                var celdaInicio = fila.Cell(colInicio);
                var celdaFin = fila.Cell(colFin);

                if (celdaEmail.IsEmpty() && celdaInicio.IsEmpty() && celdaFin.IsEmpty())
                    continue;

                var errores = new List<string>();
                var email = celdaEmail.IsEmpty() ? null : celdaEmail.GetString().Trim();
                var inicio = LeerFecha(celdaInicio, ColumnaFechaInicio, errores);
                var fin = LeerFecha(celdaFin, ColumnaFechaFin, errores);

                filas.Add(new FilaTurnoExcel(
                    numeroFila,
                    string.IsNullOrWhiteSpace(email) ? null : email,
                    inicio,
                    fin,
                    errores.Count > 0 ? string.Join(" ", errores) : null));
            }

            return Result<IReadOnlyList<FilaTurnoExcel>>.Success(filas);
        }
    }

    private static DateTime? LeerFecha(IXLCell celda, string columna, List<string> errores)
    {
        if (celda.IsEmpty())
        {
            errores.Add($"La {columna} es requerida.");
            return null;
        }

        try
        {
            switch (celda.DataType)
            {
                case XLDataType.DateTime:
                    return celda.GetDateTime();
                case XLDataType.Number:
                    return DateTime.FromOADate(celda.GetDouble());
            }
        }
        catch (Exception)
        {
            // Se intenta interpretar como texto más abajo.
        }

        var texto = celda.GetString().Trim();
        if (DateTime.TryParseExact(texto, FormatosFecha, CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            return fecha;

        errores.Add($"La {columna} '{texto}' no tiene un formato de fecha válido (use dd/MM/yyyy HH:mm).");
        return null;
    }

    private static string Normalizar(string texto) =>
        texto.Replace(" ", string.Empty).Replace("_", string.Empty).Trim().ToLowerInvariant();
}
