using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace IdermaAdministracion.Servicios;

public static class InformePdf
{
    public static void Guardar(string ruta, InformeLaboral informe)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(48);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(11).FontFamily("Times New Roman").FontColor("#1A1A1A").LineHeight(1.45f));
                pagina.Header().Column(columna =>
                {
                    columna.Item().AlignCenter().Text("IDERMA CAPILAR").FontSize(13).Bold().FontColor("#1F4E4E").LetterSpacing(1.2f);
                    columna.Item().AlignCenter().Text("Área de Administración").FontSize(10).FontColor("#5C5C5C");
                    columna.Item().PaddingTop(8).LineHorizontal(0.8f).LineColor("#1F4E4E");
                });
                pagina.Content().PaddingTop(18).Column(columna =>
                {
                    columna.Spacing(10);
                    columna.Item().AlignCenter().Text(informe.Titulo).FontSize(16).Bold();
                    foreach (var seccion in informe.Secciones)
                    {
                        if (!string.IsNullOrWhiteSpace(seccion.Titulo))
                            columna.Item().PaddingTop(10).Text(seccion.Titulo).FontSize(13).Bold().FontColor("#1F4E4E");
                        foreach (var parrafo in seccion.Parrafos)
                            columna.Item().Text(parrafo).Justify();
                    }
                });
                pagina.Footer().AlignRight().Text(texto =>
                {
                    texto.Span("Página ").FontSize(9).FontColor("#5C5C5C");
                    texto.CurrentPageNumber().FontSize(9).FontColor("#5C5C5C");
                    texto.Span(" de ").FontSize(9).FontColor("#5C5C5C");
                    texto.TotalPages().FontSize(9).FontColor("#5C5C5C");
                });
            });
        }).GeneratePdf(ruta);
    }
}
