using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace IdermaAdministracion.Pages;

public sealed partial class AnalisisPage : Page
{
    public AnalisisPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        Pintar();
    }

    private void Pintar()
    {
        var personas = RepositorioEjemplo.Colaboradores.ToList();
        var activos = personas.Where(p => p.EstaActivo).ToList();
        var porVencer = activos.Count(p => p.Contrato.AlertaVencimiento.Length > 0);
        var firmasPendientes = activos.Sum(p => p.Contrato.Documentos.Count(d => !d.Firmado));
        var abiertos = RepositorioEjemplo.PuestosAbiertos;
        var postulacionesAbiertas = RepositorioEjemplo.Postulaciones
            .Where(p => abiertos.Any(puesto => string.Equals(puesto.Nombre, p.Puesto, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var enProceso = postulacionesAbiertas.Count(p => !p.Cerrada);

        PintarIndicadores(
        [
            ("Personal activo", activos.Count.ToString(), $"{personas.Count(p => p.Rol == RolClinica.Trabajador)} trabajadores · {personas.Count(p => p.Rol == RolClinica.Doctor)} doctores"),
            ("Por vencer o vencidos", porVencer.ToString(), "Contratos activos con alerta"),
            ("Firmas pendientes", firmasPendientes.ToString(), "Documentos sin marcar en personal activo"),
            ("Postulantes en proceso", enProceso.ToString(), $"{abiertos.Count} puestos abiertos"),
        ]);

        PintarBarras(PorArea, activos
            .GroupBy(p => string.IsNullOrWhiteSpace(p.Contrato.Area) ? "Sin área" : p.Contrato.Area)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(g => g.Item2)
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList(), Trazo("AccentFillColorDefaultBrush"));

        PintarBarras(PorPlazo,
        [
            ("Sin plazo fijo", activos.Count(p => !p.Contrato.TienePlazo)),
            ("3 meses", activos.Count(p => p.Contrato.PlazoMeses == 3)),
            ("6 meses", activos.Count(p => p.Contrato.PlazoMeses == 6)),
            ("12 meses", activos.Count(p => p.Contrato.PlazoMeses == 12)),
        ], Trazo("AccentFillColorDefaultBrush"));

        PintarBarras(PorFirmas, DocumentosPorFirmar.Catalogo
            .Select(doc => (
                doc.Nombre,
                activos.Count(p => p.Contrato.Documentos.Any(d => d.Codigo == doc.Codigo && d.Firmado))))
            .ToList(), Trazo("SystemFillColorSuccessBrush"));

        var postulaciones = postulacionesAbiertas;
        PintarBarras(PorPostulacion,
        [
            ("Por contactar", postulaciones.Count(p => p.Pasos.Count == 0)),
            ("Contacto", postulaciones.Count(p => p.IndiceProceso == 0 && p.Pasos.Count > 0 && !p.Contratado && !p.TieneCierreNegativo)),
            ("Reunión", postulaciones.Count(p => p.IndiceProceso == 1 && !p.Contratado && !p.TieneCierreNegativo)),
            ("Conocimientos", postulaciones.Count(p => p.IndiceProceso == 2 && !p.Contratado && !p.TieneCierreNegativo)),
            ("Primer día", postulaciones.Count(p => p.IndiceProceso == 3 && !p.Contratado && !p.TieneCierreNegativo)),
            ("No continuó", postulaciones.Count(p => p.TieneCierreNegativo)),
            ("Contratado", postulaciones.Count(p => p.Contratado)),
        ], Trazo("AccentFillColorDefaultBrush"));

        var permisos = PermisosDatos.Todos;
        PintarBarras(PorPermiso,
        [
            ("Salud", permisos.Count(p => p.Motivo == MotivoAusencia.Salud)),
            ("Fallecimiento de familiar", permisos.Count(p => p.Motivo == MotivoAusencia.FallecimientoFamiliar)),
            ("Otros", permisos.Count(p => p.Motivo == MotivoAusencia.Otros)),
            ("Por horas", permisos.Count(p => p.Tipo == TipoAusencia.Horas)),
            ("Por días", permisos.Count(p => p.Tipo == TipoAusencia.Dias)),
        ], Trazo("SystemFillColorCautionBrush"));

        PintarBarras(PorPago,
        [
            ("Planilla", activos.Count(p => p.Contrato.Modalidad == ModalidadPago.Planilla)),
            ("Recibo por honorarios", activos.Count(p => p.Contrato.Modalidad == ModalidadPago.ReciboPorHonorarios)),
            ("Vigentes", activos.Count(p => p.Contrato.AlertaVencimiento.Length == 0)),
            ("Por vencer", activos.Count(p => p.Contrato.ProximoAVencer)),
            ("Vencidos", activos.Count(p => p.Contrato.ContratoVencido)),
        ], Trazo("AccentFillColorDefaultBrush"));
    }

    private void PintarIndicadores(IReadOnlyList<(string Titulo, string Valor, string Detalle)> tarjetas)
    {
        Indicadores.Children.Clear();
        Indicadores.ColumnDefinitions.Clear();
        for (var i = 0; i < tarjetas.Count; i++)
        {
            Indicadores.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var cuerpo = new StackPanel { Spacing = 4 };
            cuerpo.Children.Add(new TextBlock { Text = tarjetas[i].Titulo, Opacity = 0.7, TextWrapping = TextWrapping.Wrap });
            cuerpo.Children.Add(new TextBlock { Text = tarjetas[i].Valor, FontSize = 28, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
            cuerpo.Children.Add(new TextBlock { Text = tarjetas[i].Detalle, Opacity = 0.75, TextWrapping = TextWrapping.Wrap });
            var tarjeta = new Border
            {
                Style = (Style)Application.Current.Resources["TarjetaEstilo"],
                Child = cuerpo,
            };
            Grid.SetColumn(tarjeta, i);
            Indicadores.Children.Add(tarjeta);
        }
    }

    private static void PintarBarras(Panel destino, IReadOnlyList<(string Etiqueta, int Valor)> filas, Brush color)
    {
        destino.Children.Clear();
        var maximo = Math.Max(1, filas.Count == 0 ? 1 : filas.Max(f => f.Valor));
        var pista = Trazo("SubtleFillColorSecondaryBrush");
        if (filas.Count == 0)
        {
            destino.Children.Add(new TextBlock { Text = "Sin datos todavía.", Opacity = 0.7 });
            return;
        }

        foreach (var fila in filas)
        {
            var bloque = new Grid { ColumnSpacing = 10 };
            bloque.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(148) });
            bloque.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            bloque.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });

            bloque.Children.Add(new TextBlock
            {
                Text = fila.Etiqueta,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            });

            var medida = new Grid { Height = 14, VerticalAlignment = VerticalAlignment.Center };
            var lleno = Math.Max(fila.Valor, 0);
            var resto = Math.Max(maximo - lleno, 0);
            medida.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(lleno == 0 ? 0 : lleno, GridUnitType.Star) });
            medida.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(resto == 0 && lleno == 0 ? 1 : resto, GridUnitType.Star) });
            var fondo = new Border
            {
                Background = pista,
                CornerRadius = new CornerRadius(7),
            };
            Grid.SetColumnSpan(fondo, 2);
            var relleno = new Border
            {
                Background = color,
                CornerRadius = new CornerRadius(7),
            };
            medida.Children.Add(fondo);
            medida.Children.Add(relleno);
            Grid.SetColumn(medida, 1);
            bloque.Children.Add(medida);
            var numero = new TextBlock
            {
                Text = fila.Valor.ToString(),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(numero, 2);
            bloque.Children.Add(numero);
            destino.Children.Add(bloque);
        }
    }

    private static Brush Trazo(string recurso) =>
        Application.Current.Resources[recurso] as Brush
        ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
}
