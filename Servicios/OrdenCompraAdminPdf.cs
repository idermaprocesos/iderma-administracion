using IdermaAdministracion.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IdermaAdministracion.Servicios;

public static class OrdenCompraAdminPdf
{
    public static void Guardar(string ruta, OrdenCompraAdminDocumento documento)
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
                    col.Item().AlignCenter().Text("Orden de compra").FontSize(10).FontColor("#5C5C5C");
                    col.Item().PaddingTop(6).LineHorizontal(0.8f).LineColor("#1F4E4E");
                });
                pagina.Content().PaddingTop(12).Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("ORDEN DE COMPRA").FontSize(16).Bold().FontColor("#1F4E4E");
                    col.Item().Text(string.IsNullOrWhiteSpace(documento.Proveedor) ? "Sin proveedor indicado" : $"Proveedor: {documento.Proveedor}");
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
                            c.RelativeColumn(3);
                            c.ConstantColumn(60);
                            c.ConstantColumn(50);
                            c.ConstantColumn(90);
                            c.ConstantColumn(80);
                            c.ConstantColumn(80);
                            c.ConstantColumn(90);
                            c.ConstantColumn(80);
                        });
                        tabla.Header(h =>
                        {
                            foreach (var titulo in new[] { "#", "Descripción", "Cant.", "Und.", "Tipo", "Sin IGV", "IGV 18%", "Con IGV", "P. unit." })
                                h.Cell().Background("#1F4E4E").Padding(4).Text(titulo).FontSize(8).FontColor(Colors.White).Bold();
                        });
                        var n = 1;
                        foreach (var linea in documento.Lineas)
                        {
                            foreach (var texto in new[]
                                     {
                                         n.ToString(), linea.Descripcion, linea.Cantidad.ToString("0.##"), linea.Unidad,
                                         linea.TipoPrecio, linea.SinIgv.ToString("C"), linea.Igv.ToString("C"),
                                         linea.TotalConIgv.ToString("C"), linea.PrecioConIgv.ToString("C"),
                                     })
                                tabla.Cell().BorderBottom(0.4f).BorderColor("#E4E0D8").Padding(3).Text(texto).FontSize(8);
                            n++;
                        }
                    });
                    col.Item().AlignRight().Text($"Total con IGV: {documento.Lineas.Sum(l => l.TotalConIgv):C}").FontSize(12).Bold();
                });
                pagina.Footer().AlignRight().Text(t =>
                {
                    t.Span("Página ").FontSize(8).FontColor("#5C5C5C");
                    t.CurrentPageNumber().FontSize(8);
                    t.Span(" de ").FontSize(8);
                    t.TotalPages().FontSize(8);
                });
            });
        }).GeneratePdf(ruta);
    }

    private static void Meta(TableDescriptor tabla, string etiqueta, string valor) =>
        tabla.Cell().Border(0.4f).BorderColor("#D8D2C6").Padding(4).Text(t =>
        {
            t.Span(etiqueta + "  ").FontSize(8).FontColor("#5C5C5C");
            t.Span(string.IsNullOrWhiteSpace(valor) ? "—" : valor).SemiBold();
        });
}
