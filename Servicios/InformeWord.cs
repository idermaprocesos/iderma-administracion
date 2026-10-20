using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace IdermaAdministracion.Servicios;

public static class InformeWord
{
    public static void Guardar(string ruta, InformeLaboral informe)
    {
        using var archivo = File.Create(ruta);
        using var documento = WordprocessingDocument.Create(archivo, WordprocessingDocumentType.Document);
        var parte = documento.AddMainDocumentPart();
        parte.Document = new Document(new Body());
        var cuerpo = parte.Document.Body!;

        cuerpo.Append(Linea("Iderma Capilar", 22, true, "2E6B6B"));
        cuerpo.Append(Linea("Administración · Informe de situación laboral", 20, false, "5C5C5C"));
        cuerpo.Append(Linea(informe.Titulo, 32, true, "1A1A1A"));

        foreach (var seccion in informe.Secciones)
        {
            cuerpo.Append(Linea(seccion.Titulo, 26, true, "1F4E4E"));
            foreach (var parrafo in seccion.Parrafos)
                cuerpo.Append(Linea(parrafo, 22, false, "1A1A1A"));
        }

        parte.Document.Save();
    }

    private static Paragraph Linea(string texto, int mediosPuntos, bool negrita, string color)
    {
        var propiedades = new ParagraphProperties(
            new SpacingBetweenLines { Before = "80", After = "160", Line = "360", LineRule = LineSpacingRuleValues.Auto },
            new Justification { Val = negrita ? JustificationValues.Left : JustificationValues.Both });
        if (!negrita)
            propiedades.Append(new Indentation { FirstLine = "420" });
        var letra = new RunProperties(
            new RunFonts { Ascii = "Times New Roman", HighAnsi = "Times New Roman" },
            new FontSize { Val = mediosPuntos.ToString() },
            new Color { Val = color });
        if (negrita)
            letra.Append(new Bold());

        return new Paragraph(
            propiedades,
            new Run(letra, new Text(texto) { Space = SpaceProcessingModeValues.Preserve }));
    }
}
