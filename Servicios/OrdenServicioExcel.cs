using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using IdermaAdministracion.Models;

namespace IdermaAdministracion.Servicios;

public static class OrdenServicioExcel
{
    public static void Guardar(string ruta, OrdenServicioDocumento documento)
    {
        using var libro = SpreadsheetDocument.Create(ruta, SpreadsheetDocumentType.Workbook);
        var parte = libro.AddWorkbookPart();
        parte.Workbook = new Workbook();
        var hoja = parte.AddNewPart<WorksheetPart>();
        var datos = new SheetData();
        hoja.Worksheet = new Worksheet(datos);
        parte.Workbook.AppendChild(new Sheets()).Append(new Sheet
        {
            Id = parte.GetIdOfPart(hoja),
            SheetId = 1,
            Name = "Orden de servicio",
        });

        Fila(datos, 1, "IDERMA CAPILAR — ORDEN DE SERVICIO");
        Fila(datos, 2, documento.Objeto);
        Fila(datos, 3,
            "Código", documento.CodigoFormato,
            "Mes / Año", documento.MesAnio,
            "Elaboración", documento.FechaElaboracion,
            "Estado", documento.Estado);
        Fila(datos, 4,
            "Elaborado por", documento.ElaboradoPor,
            "Revisado por", documento.RevisadoPor,
            "Cobertura", documento.PeriodoCobertura);
        Fila(datos, 6,
            "#", "Colaborador", "Documento", "Cargo", "Área", "Contrato", "Tipo", "Modalidad",
            "Vence", "Meses", "Nueva vigencia", "Mensual", "Sin IGV", "IGV 18%", "Con IGV");

        var fila = 7u;
        var n = 1;
        foreach (var linea in documento.Lineas)
        {
            Fila(datos, fila++,
                n.ToString(),
                linea.Nombre,
                linea.Documento,
                linea.Cargo,
                linea.Area,
                linea.NumeroContrato,
                linea.TipoContrato,
                linea.Modalidad,
                linea.FechaFinActual?.ToString("dd/MM/yyyy") ?? "",
                linea.Meses.ToString(),
                linea.NuevaVigencia.ToString("dd/MM/yyyy"),
                linea.SueldoMensual.ToString("0.00"),
                linea.SinIgv.ToString("0.00"),
                linea.Igv.ToString("0.00"),
                linea.TotalConIgv.ToString("0.00"));
            n++;
        }

        Fila(datos, fila + 1, "Total con IGV", documento.Lineas.Sum(l => l.TotalConIgv).ToString("0.00"));
        parte.Workbook.Save();
    }

    private static void Fila(SheetData datos, uint indice, params string[] celdas)
    {
        var fila = new Row { RowIndex = indice };
        for (var i = 0; i < celdas.Length; i++)
        {
            fila.Append(new Cell
            {
                CellReference = Columna(i) + indice,
                DataType = CellValues.String,
                CellValue = new CellValue(celdas[i] ?? ""),
            });
        }

        datos.Append(fila);
    }

    private static string Columna(int indice)
    {
        var nombre = "";
        var n = indice;
        do
        {
            nombre = (char)('A' + n % 26) + nombre;
            n = n / 26 - 1;
        }
        while (n >= 0);

        return nombre;
    }
}
