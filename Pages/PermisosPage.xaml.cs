using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class PermisosPage : Page
{
    private bool _clinica;
    private Colaborador? _persona;
    private string? _permisoId;

    public PermisosPage()
    {
        InitializeComponent();
        FechaDesde.Date = DateTimeOffset.Now;
        FechaHasta.Date = DateTimeOffset.Now;
        HoraInicio.Time = new TimeSpan(9, 0, 0);
        HoraFin.Time = new TimeSpan(12, 0, 0);
        Loaded += (_, _) =>
        {
            PintarListas();
            Tipo_SelectionChanged(Tipo, null!);
        };
    }

    private void Grupo_Click(object sender, RoutedEventArgs e)
    {
        _clinica = sender is Button { Tag: string etiqueta } && etiqueta == "clinica";
        _persona = null;
        Detalle.Visibility = Visibility.Collapsed;
        PintarListas();
    }

    private void PintarListas()
    {
        Listas.Children.Clear();
        var areas = _clinica ? PermisosDatos.AreasClinica : PermisosDatos.AreasTrabajadores;
        foreach (var area in areas)
        {
            var personas = RepositorioEjemplo.Colaboradores
                .Where(c => string.Equals(c.Contrato.Area, area, StringComparison.OrdinalIgnoreCase))
                .OrderBy(c => c.Apellidos)
                .ThenBy(c => c.Nombres)
                .ToList();
            if (personas.Count == 0)
                continue;

            var bloque = new StackPanel { Spacing = 8 };
            bloque.Children.Add(new TextBlock
            {
                Text = area,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            });
            if (personas.Count == 0)
            {
                bloque.Children.Add(new TextBlock { Opacity = 0.7, Text = "Sin personas en esta área." });
            }
            else
            {
                foreach (var persona in personas)
                {
                    var boton = new Button
                    {
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        HorizontalContentAlignment = HorizontalAlignment.Left,
                        Content = $"{persona.NombreCompleto} · {persona.Contrato.Cargo}",
                    };
                    var elegida = persona;
                    boton.Click += (_, _) => Elegir(elegida);
                    bloque.Children.Add(boton);
                }
            }

            Listas.Children.Add(new Border
            {
                Style = (Style)Application.Current.Resources["TarjetaEstilo"],
                Child = bloque,
            });
        }
    }

    private void Elegir(Colaborador persona)
    {
        _persona = persona;
        _permisoId = null;
        Detalle.Visibility = Visibility.Visible;
        NombrePersona.Text = persona.NombreCompleto;
        CargoPersona.Text = $"{persona.Contrato.Cargo} · {persona.Contrato.Area}";
        LimpiarFormulario();
        PintarHistorial();
    }

    private void Tipo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BloqueHoras is null || FechaHasta is null)
            return;

        var porHoras = Tipo.SelectedIndex == 0;
        BloqueHoras.Visibility = porHoras ? Visibility.Visible : Visibility.Collapsed;
        FechaHasta.Visibility = porHoras ? Visibility.Collapsed : Visibility.Visible;
        FechaDesde.Header = porHoras ? "Fecha" : "Desde";
    }

    private void Motivo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (OtroMotivo is null)
            return;

        OtroMotivo.Visibility = Motivo.SelectedIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Nuevo_Click(object sender, RoutedEventArgs e)
    {
        _permisoId = null;
        LimpiarFormulario();
    }

    private void Guardar_Click(object sender, RoutedEventArgs e)
    {
        if (_persona is null)
            return;

        if (string.IsNullOrWhiteSpace(Justificacion.Text))
        {
            MostrarAviso("Falta la justificación", "Escribe por qué se pide el permiso.", InfoBarSeverity.Warning);
            return;
        }

        if (Motivo.SelectedIndex == 2 && string.IsNullOrWhiteSpace(OtroMotivo.Text))
        {
            MostrarAviso("Falta el motivo", "Describe el motivo en Otros.", InfoBarSeverity.Warning);
            return;
        }

        if (FechaDesde.Date is not DateTimeOffset desde)
        {
            MostrarAviso("Falta la fecha", "Indica desde cuándo es el permiso.", InfoBarSeverity.Warning);
            return;
        }

        var porHoras = Tipo.SelectedIndex == 0;
        var hasta = porHoras ? desde : FechaHasta.Date ?? desde;
        if (!porHoras && hasta.Date < desde.Date)
        {
            MostrarAviso("Fechas inválidas", "La fecha final no puede ser anterior al inicio.", InfoBarSeverity.Warning);
            return;
        }

        if (porHoras && HoraFin.Time <= HoraInicio.Time)
        {
            MostrarAviso("Horario inválido", "La hora de salida tiene que ser posterior a la de inicio.", InfoBarSeverity.Warning);
            return;
        }

        PermisosDatos.Guardar(new PermisoAusencia
        {
            Id = _permisoId ?? $"per-{Guid.NewGuid():N}"[..12],
            ColaboradorId = _persona.Id,
            Tipo = porHoras ? TipoAusencia.Horas : TipoAusencia.Dias,
            Desde = desde.Date,
            Hasta = hasta.Date,
            HoraInicio = porHoras ? HoraInicio.Time : null,
            HoraFin = porHoras ? HoraFin.Time : null,
            Motivo = Motivo.SelectedIndex switch
            {
                1 => MotivoAusencia.FallecimientoFamiliar,
                2 => MotivoAusencia.Otros,
                _ => MotivoAusencia.Salud,
            },
            OtroMotivo = Motivo.SelectedIndex == 2 ? OtroMotivo.Text.Trim() : "",
            Justificacion = Justificacion.Text.Trim(),
            AceptadoJefe = AceptadoJefe.IsChecked == true,
            AceptadoAdministracion = AceptadoAdministracion.IsChecked == true,
            AceptadoRecursosHumanos = AceptadoRecursosHumanos.IsChecked == true,
            AceptadoGerencia = AceptadoGerencia.IsChecked == true,
        });

        _permisoId = null;
        LimpiarFormulario();
        PintarHistorial();
        MostrarAviso("Permiso guardado", "Quedó en el registro de esta persona.", InfoBarSeverity.Success);
    }

    private void PintarHistorial()
    {
        Historial.Children.Clear();
        if (_persona is null)
            return;

        var permisos = PermisosDatos.De(_persona.Id);
        if (permisos.Count == 0)
        {
            Historial.Children.Add(new TextBlock { Opacity = 0.75, Text = "Todavía no hay permisos de esta persona." });
            return;
        }

        foreach (var permiso in permisos)
        {
            var editar = new Button { Content = "Editar", HorizontalAlignment = HorizontalAlignment.Left };
            var actual = permiso;
            editar.Click += (_, _) => Cargar(actual);
            var quitar = new Button { Content = "Quitar", HorizontalAlignment = HorizontalAlignment.Left };
            quitar.Click += (_, _) =>
            {
                PermisosDatos.Eliminar(actual.Id);
                if (_permisoId == actual.Id)
                {
                    _permisoId = null;
                    LimpiarFormulario();
                }

                PintarHistorial();
            };

            var cuerpo = new StackPanel { Spacing = 4 };
            cuerpo.Children.Add(new TextBlock
            {
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Text = $"{permiso.TipoTexto} · {permiso.MotivoTexto}",
                TextWrapping = TextWrapping.Wrap,
            });
            cuerpo.Children.Add(new TextBlock { Opacity = 0.8, Text = permiso.CuandoTexto, TextWrapping = TextWrapping.Wrap });
            cuerpo.Children.Add(new TextBlock { Text = permiso.Justificacion, TextWrapping = TextWrapping.Wrap });
            cuerpo.Children.Add(new TextBlock { Opacity = 0.75, Text = permiso.AprobacionesTexto, TextWrapping = TextWrapping.Wrap });
            var acciones = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            acciones.Children.Add(editar);
            acciones.Children.Add(quitar);
            cuerpo.Children.Add(acciones);
            Historial.Children.Add(new Border
            {
                Style = (Style)Application.Current.Resources["TarjetaEstilo"],
                Child = cuerpo,
            });
        }
    }

    private void Cargar(PermisoAusencia permiso)
    {
        _permisoId = permiso.Id;
        Tipo.SelectedIndex = permiso.Tipo == TipoAusencia.Horas ? 0 : 1;
        FechaDesde.Date = new DateTimeOffset(permiso.Desde);
        FechaHasta.Date = new DateTimeOffset(permiso.Hasta);
        if (permiso.HoraInicio is TimeSpan inicio)
            HoraInicio.Time = inicio;
        if (permiso.HoraFin is TimeSpan fin)
            HoraFin.Time = fin;
        Motivo.SelectedIndex = permiso.Motivo switch
        {
            MotivoAusencia.FallecimientoFamiliar => 1,
            MotivoAusencia.Otros => 2,
            _ => 0,
        };
        OtroMotivo.Text = permiso.OtroMotivo;
        Justificacion.Text = permiso.Justificacion;
        AceptadoJefe.IsChecked = permiso.AceptadoJefe;
        AceptadoAdministracion.IsChecked = permiso.AceptadoAdministracion;
        AceptadoRecursosHumanos.IsChecked = permiso.AceptadoRecursosHumanos;
        AceptadoGerencia.IsChecked = permiso.AceptadoGerencia;
        Aviso.IsOpen = false;
    }

    private void LimpiarFormulario()
    {
        Tipo.SelectedIndex = 0;
        FechaDesde.Date = DateTimeOffset.Now;
        FechaHasta.Date = DateTimeOffset.Now;
        HoraInicio.Time = new TimeSpan(9, 0, 0);
        HoraFin.Time = new TimeSpan(12, 0, 0);
        Motivo.SelectedIndex = 0;
        OtroMotivo.Text = "";
        Justificacion.Text = "";
        AceptadoJefe.IsChecked = false;
        AceptadoAdministracion.IsChecked = false;
        AceptadoRecursosHumanos.IsChecked = false;
        AceptadoGerencia.IsChecked = false;
        Aviso.IsOpen = false;
    }

    private void MostrarAviso(string titulo, string mensaje, InfoBarSeverity severidad)
    {
        Aviso.Title = titulo;
        Aviso.Message = mensaje;
        Aviso.Severity = severidad;
        Aviso.IsOpen = true;
    }
}
