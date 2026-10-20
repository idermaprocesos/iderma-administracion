using IdermaAdministracion.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IdermaAdministracion.Servicios;

public static class OrdenServicioPdf
{
    public static void Guardar(string ruta, OrdenServicioDocumento documento)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(doc =>
        {
            doc.Page(pagina =>
            {
                pagina.Size(PageSizes.A4.Landscape());
                pagina.Margin(28);
                pagina.DefaultTextStyle(e => e.FontSize(9).FontFamily("Calibri").FontColor("#1A1A1A"));
                pagina.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("IDERMA CAPILAR").FontSize(14).Bold().FontColor("#1F4E4E").LetterSpacing(1.1f);
                    col.Item().AlignCenter().Text("Orden de servicio · ampliación de contratos").FontSize(10).FontColor("#5C5C5C");
                    col.Item().PaddingTop(6).LineHorizontal(0.8f).LineColor("#1F4E4E");
                });
                pagina.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("ORDEN DE SERVICIO").FontSize(16).Bold().FontColor("#1F4E4E");
                    col.Item().Text(documento.Objeto);
                    col.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                            c.RelativeColumn();
                        });
                        Meta(tabla, "Código", documento.CodigoFormato);
                        Meta(tabla, "Mes / Año", documento.MesAnio);
                        Meta(tabla, "Elaboración", documento.FechaElaboracion);
                        Meta(tabla, "Estado", documento.Estado);
                        Meta(tabla, "Elaborado por", documento.ElaboradoPor);
                        Meta(tabla, "Revisado por", documento.RevisadoPor);
                        Meta(tabla, "Cobertura", documento.PeriodoCobertura);
                        Meta(tabla, "Líneas", documento.Lineas.Count.ToString());
                    });
                    col.Item().PaddingTop(6).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(22);
                            c.RelativeColumn(2.2f);
                            c.RelativeColumn(1.6f);
                            c.RelativeColumn(1.2f);
                            c.ConstantColumn(52);
                            c.ConstantColumn(72);
                            c.ConstantColumn(78);
                            c.ConstantColumn(72);
                            c.ConstantColumn(78);
                        });
                        tabla.Header(h =>
                        {
                            Cabecera(h.Cell(), "#");
                            Cabecera(h.Cell(), "Colaborador");
                            Cabecera(h.Cell(), "Cargo");
                            Cabecera(h.Cell(), "Contrato");
                            Cabecera(h.Cell(), "Meses");
                            Cabecera(h.Cell(), "Nueva vigencia");
                            Cabecera(h.Cell(), "Sin IGV");
                            Cabecera(h.Cell(), "IGV 18%");
                            Cabecera(h.Cell(), "Con IGV");
                        });
                        var n = 1;
                        foreach (var linea in documento.Lineas)
                        {
                            Celda(tabla, n.ToString());
                            Celda(tabla, $"{linea.Nombre}\n{linea.Documento}");
                            Celda(tabla, $"{linea.Cargo}\n{linea.Area}");
                            Celda(tabla, linea.NumeroContrato);
                            Celda(tabla, linea.Meses.ToString());
                            Celda(tabla, linea.NuevaVigencia.ToString("dd/MM/yyyy"));
                            Celda(tabla, linea.SinIgv.ToString("C"));
                            Celda(tabla, linea.Igv.ToString("C"));
                            Celda(tabla, linea.TotalConIgv.ToString("C"));
                            n++;
                        }
                    });
                    var total = documento.Lineas.Sum(l => l.TotalConIgv);
                    var igv = documento.Lineas.Sum(l => l.Igv);
                    col.Item().AlignRight().Text($"IGV: {igv:C}    Total con IGV: {total:C}").FontSize(12).Bold();
                });
                pagina.Footer().AlignRight().Text(t =>
                {
                    t.Span("Página ").FontSize(8).FontColor("#5C5C5C");
                    t.CurrentPageNumber().FontSize(8).FontColor("#5C5C5C");
                    t.Span(" de ").FontSize(8).FontColor("#5C5C5C");
                    t.TotalPages().FontSize(8).FontColor("#5C5C5C");
                });
            });
        }).GeneratePdf(ruta);
    }

    private static void Meta(TableDescriptor tabla, string etiqueta, string valor)
    {
        tabla.Cell().Border(0.4f).BorderColor("#D8D2C6").Padding(4).Text(t =>
        {
            t.Span(etiqueta + "  ").FontSize(8).FontColor("#5C5C5C");
            t.Span(string.IsNullOrWhiteSpace(valor) ? "—" : valor).SemiBold();
        });
    }

    private static void Cabecera(IContainer celda, string texto) =>
        celda.Background("#1F4E4E").Padding(4).AlignMiddle().Text(texto).FontSize(8).FontColor(Colors.White).Bold();

    private static void Celda(TableDescriptor tabla, string texto) =>
        tabla.Cell().BorderBottom(0.4f).BorderColor("#E4E0D8").Padding(3).AlignMiddle().Text(texto).FontSize(8);
}
