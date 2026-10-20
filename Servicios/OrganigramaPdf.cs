using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace IdermaAdministracion.Servicios;

public static class OrganigramaPdf
{
    private const string Tinta = "#1F4E4E";
    private const string Texto = "#1A1A1A";
    private const string Suave = "#5C5C5C";
    private const float AnchoCaja = 148f;
    private const float Separacion = 8f;

    public static void Guardar(string ruta)
    {
        OrganigramaDatos.CargarDesdePersonal();
        QuestPDF.Settings.License = LicenseType.Community;

        var ancho = Medir(OrganigramaDatos.Raiz) + 96f;
        Document.Create(documento =>
        {
            documento.Page(pagina =>
            {
                pagina.ContinuousSize(ancho);
                pagina.Margin(24);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(8).FontFamily("Calibri").FontColor(Texto));
                pagina.Content().Column(columna =>
                {
                    columna.Spacing(0);
                    columna.Item().AlignCenter().Text("IDERMA CAPILAR").FontSize(14).Bold().FontColor(Tinta);
                    columna.Item().AlignCenter().PaddingBottom(10).Text("Organigrama").FontSize(11).FontColor(Suave);
                    Pintar(columna, OrganigramaDatos.Raiz);
                });
            });
        }).GeneratePdf(ruta);
    }

    private static void Pintar(ColumnDescriptor columna, NodoOrganigrama nodo)
    {
        var ancho = Medir(nodo);
        columna.Item().AlignCenter().Width(ancho).Column(bloque =>
        {
            Caja(bloque, nodo);
            Cola(bloque, nodo);
        });
    }

    private static void Cola(ColumnDescriptor columna, NodoOrganigrama nodo)
    {
        if (nodo.Hijos.Count == 0)
            return;

        if (nodo.Hijos.Count == 1)
        {
            Trazo(columna, 12, nodo.Hijos[0].Asesoria);
            Pintar(columna, nodo.Hijos[0]);
            return;
        }

        Trazo(columna, 12, punteada: false);
        Conector(columna, nodo.Hijos);
        columna.Item().Row(fila =>
        {
            foreach (var hijo in nodo.Hijos)
                CajaDeNivel(fila, hijo);
        });
        columna.Item().Row(fila =>
        {
            foreach (var hijo in nodo.Hijos)
                ColaDeNivel(fila, hijo);
        });
    }

    private static void CajaDeNivel(RowDescriptor fila, NodoOrganigrama hijo)
    {
        var celda = fila.RelativeItem(Medir(hijo) + Separacion).PaddingHorizontal(Separacion / 2);
        if (!hijo.Agrupa)
        {
            celda.Element(e => CajaAjustada(e, hijo));
            return;
        }

        celda.AlignCenter().AlignTop().MaxWidth(AnchoFila(hijo.Hijos)).Column(columna =>
        {
            Conector(columna, hijo.Hijos);
            columna.Item().Row(sub =>
            {
                foreach (var jefe in hijo.Hijos)
                    sub.RelativeItem().PaddingHorizontal(Separacion / 2).Element(e => CajaAjustada(e, jefe));
            });
        });
    }

    private static void ColaDeNivel(RowDescriptor fila, NodoOrganigrama hijo)
    {
        var celda = fila.RelativeItem(Medir(hijo) + Separacion).PaddingHorizontal(Separacion / 2);
        if (!hijo.Agrupa)
        {
            celda.Element(e => e.Column(c => Cola(c, hijo)));
            return;
        }

        celda.Column(columna =>
        {
            columna.Item().AlignCenter().MaxWidth(AnchoFila(hijo.Hijos)).Element(e => Puente(e, hijo.Hijos));
            Conector(columna, hijo.Compartidos);
            columna.Item().Row(sub =>
            {
                foreach (var area in hijo.Compartidos)
                    sub.RelativeItem().PaddingHorizontal(Separacion / 2).Element(e => CajaAjustada(e, area));
            });
            columna.Item().Row(sub =>
            {
                foreach (var area in hijo.Compartidos)
                    sub.RelativeItem().PaddingHorizontal(Separacion / 2).Element(e => e.Column(c => Cola(c, area)));
            });
        });
    }

    private static void CajaAjustada(IContainer contenedor, NodoOrganigrama nodo) =>
        contenedor.AlignCenter().AlignTop().Width(AnchoCaja).Element(e => CajaEn(e, nodo));

    private static float Medir(NodoOrganigrama nodo)
    {
        if (nodo.Agrupa)
            return Math.Max(AnchoFila(nodo.Hijos), AnchoFila(nodo.Compartidos));
        if (nodo.Hijos.Count == 0)
            return AnchoCaja + 12f;
        if (nodo.Hijos.Count == 1)
            return Math.Max(AnchoCaja, Medir(nodo.Hijos[0]));
        return Math.Max(AnchoCaja, AnchoFila(nodo.Hijos));
    }

    private static float AnchoFila(IReadOnlyList<NodoOrganigrama> nodos) =>
        nodos.Count == 0 ? 0 : nodos.Sum(n => Medir(n) + Separacion);

    private static void Puente(IContainer contenedor, IReadOnlyList<NodoOrganigrama> jefes)
    {
        contenedor.Column(columna =>
        {
            Conector(columna, jefes, barraAbajo: true);
            Trazo(columna, 12, punteada: false);
        });
    }

    private static void Conector(ColumnDescriptor columna, IReadOnlyList<NodoOrganigrama> hijos, bool barraAbajo = false)
    {
        columna.Item().Row(fila =>
        {
            for (var i = 0; i < hijos.Count; i++)
            {
                var primero = i == 0;
                var ultimo = i == hijos.Count - 1;
                var peso = Medir(hijos[i]) + Separacion;
                fila.RelativeItem(peso).Row(mitad =>
                {
                    mitad.RelativeItem().Element(celda => Celda(celda, barra: !primero, caida: true, punteada: hijos[i].Asesoria, barraAbajo));
                    mitad.RelativeItem().Element(celda => Celda(celda, barra: !ultimo, caida: false, punteada: false, barraAbajo));
                });
            }
        });
    }

    private static void Celda(IContainer celda, bool barra, bool caida, bool punteada, bool barraAbajo = false)
    {
        celda.Height(14).Layers(capas =>
        {
            capas.PrimaryLayer();
            if (barra)
            {
                var linea = barraAbajo ? capas.Layer().AlignBottom() : capas.Layer().AlignTop();
                linea.Height(1.25f).Background(Tinta);
            }
            if (!caida)
                return;

            if (!punteada)
            {
                capas.Layer().AlignRight().Width(1.25f).Background(Tinta);
                return;
            }

            capas.Layer().AlignRight().Width(1.25f).Column(columna =>
            {
                columna.Item().Height(3).Background(Tinta);
                columna.Item().Height(2);
                columna.Item().Height(3).Background(Tinta);
                columna.Item().Height(2);
                columna.Item().Height(3).Background(Tinta);
            });
        });
    }

    private static void Caja(ColumnDescriptor columna, NodoOrganigrama nodo) =>
        columna.Item().AlignCenter().Width(AnchoCaja).Element(e => CajaEn(e, nodo));

    private static void CajaEn(IContainer contenedor, NodoOrganigrama nodo)
    {
        contenedor.Border(1).BorderColor(Tinta).Background("#F4F7F7").Padding(6).AlignTop().Column(caja =>
        {
            caja.Item().AlignCenter().Text(nodo.Titulo).Bold().FontSize(9).FontColor(Tinta);
            foreach (var persona in nodo.Personas)
            {
                caja.Item().PaddingTop(3).AlignCenter().Text(persona.Nombre).FontSize(8);
                if (!string.IsNullOrWhiteSpace(persona.Cargo))
                    caja.Item().AlignCenter().Text(persona.Cargo).FontSize(7).FontColor(Suave);
            }
        });
    }

    private static void Trazo(ColumnDescriptor columna, float alto, bool punteada)
    {
        if (!punteada)
        {
            columna.Item().AlignCenter().Width(1.25f).Height(alto).Background(Tinta);
            return;
        }

        columna.Item().AlignCenter().Width(1.25f).Column(columnaTrazo =>
        {
            var restante = alto;
            while (restante > 0)
            {
                var tramo = Math.Min(3, restante);
                columnaTrazo.Item().Height(tramo).Background(Tinta);
                restante -= tramo;
                if (restante <= 0)
                    break;

                var hueco = Math.Min(2, restante);
                columnaTrazo.Item().Height(hueco);
                restante -= hueco;
            }
        });
    }
}
