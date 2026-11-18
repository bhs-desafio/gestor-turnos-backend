using GestorTurnos.CrossCutting.Results;

namespace GestorTurnos.Application.Abstractions;

/// <summary>
/// Fila leída del Excel de turnos. Las fechas son null si la celda está vacía o no es una fecha válida;
/// en ese caso <see cref="ErrorLectura"/> describe el problema.
/// </summary>
public sealed record FilaTurnoExcel(
    int Fila,
    string? Email,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    string? ErrorLectura);

/// <summary>
/// Lee un archivo .xlsx con las columnas email, fechaInicio y fechaFin (cabecera en la primera fila).
/// </summary>
public interface ILectorExcelTurnos
{
    /// <summary>
    /// Devuelve las filas con datos, o un error de validación si el archivo no es un .xlsx válido
    /// o le faltan columnas obligatorias.
    /// </summary>
    Result<IReadOnlyList<FilaTurnoExcel>> Leer(byte[] archivo);
}
