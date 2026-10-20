using IdermaAdministracion.Datos;
using IdermaAdministracion.Dialogos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class PostulacionesPage : Page
{
    private readonly HashSet<string> _seleccionados = new(StringComparer.Ordinal);
    private List<Postulacion> _visibles = [];

    public PostulacionesPage()
    {
        InitializeComponent();
        var areas = new List<string> { "Todas" };
        areas.AddRange(RepositorioEjemplo.Areas);
        FiltroArea.ItemsSource = areas;
        FiltroArea.SelectedIndex = 0;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        Aplicar();
    }

    private async void NuevoPuesto_Click(object sender, RoutedEventArgs e)
    {
        var dialogo = new EditorPuestoDialog { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary || dialogo.Creado is null)
            return;

        BuscarBox.Text = "";
        FiltroProceso.SelectedIndex = 0;
        FiltroArea.SelectedIndex = 0;
        Aplicar();
    }

    private async void Agregar_Click(object sender, RoutedEventArgs e)
    {
        if (RepositorioEjemplo.PuestosAbiertos.Count == 0)
        {
            AvisoMasivo.Severity = InfoBarSeverity.Warning;
            AvisoMasivo.Title = "Falta un puesto";
            AvisoMasivo.Message = "Crea primero el puesto de trabajo y después agrega postulantes.";
            AvisoMasivo.IsOpen = true;
            return;
        }

        await AbrirNuevaPostulacionAsync(null);
    }

    private async Task AbrirNuevaPostulacionAsync(PuestoTrabajo? puesto)
    {
        var borrador = RepositorioEjemplo.CrearPostulacion();
        if (puesto is not null)
        {
            borrador.Puesto = puesto.Nombre;
            borrador.Area = puesto.Area;
            borrador.Rol = puesto.Rol;
        }
        else if (RepositorioEjemplo.PuestosAbiertos.FirstOrDefault() is PuestoTrabajo primero)
        {
            borrador.Puesto = primero.Nombre;
            borrador.Area = primero.Area;
            borrador.Rol = primero.Rol;
        }

        var dialogo = new EditorPostulacionDialog(borrador, fijarPuesto: puesto is not null) { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            return;

        BuscarBox.Text = "";
        Aplicar();
        NavegacionApp.AbrirPostulacion(borrador.Id);
    }

    private void BuscarBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            Aplicar();
    }

    private void Filtro_SelectionChanged(object sender, SelectionChangedEventArgs e) => Aplicar();

    private void LimpiarSeleccion_Click(object sender, RoutedEventArgs e)
    {
        _seleccionados.Clear();
        Aplicar();
    }

    private void AplicarEstado_Click(object sender, RoutedEventArgs e)
    {
        var indice = EstadoMasivo.SelectedIndex;
        if (indice < 0)
            return;

        var elegidos = _visibles.Where(p => _seleccionados.Contains(p.Id)).ToList();
        if (elegidos.Count == 0)
        {
            AvisoMasivo.Severity = InfoBarSeverity.Warning;
            AvisoMasivo.Title = "Nadie marcado";
            AvisoMasivo.Message = "Marca uno o más postulantes y después pulsa Cambiar estado.";
            AvisoMasivo.IsOpen = true;
            return;
        }

        foreach (var postulacion in elegidos)
            RepositorioEjemplo.UbicarProceso(postulacion, indice);

        var texto = (EstadoMasivo.SelectedItem as ComboBoxItem)?.Content as string ?? "el estado elegido";
        AvisoMasivo.Severity = InfoBarSeverity.Success;
        AvisoMasivo.Title = "Estado actualizado";
        AvisoMasivo.Message = elegidos.Count == 1
            ? $"1 postulante pasó a {texto}."
            : $"{elegidos.Count} postulantes pasaron a {texto}.";
        AvisoMasivo.IsOpen = true;
        Aplicar();
    }

    private void Aplicar()
    {
        if (Grupos is null || FiltroProceso is null)
            return;

        IEnumerable<Postulacion> consulta = RepositorioEjemplo.Postulaciones;
        var texto = BuscarBox.Text?.Trim() ?? "";
        if (texto.Length > 0)
        {
            consulta = consulta.Where(p =>
                p.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Puesto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Area.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Documento.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Nota.Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        consulta = FiltroProceso.SelectedIndex switch
        {
            1 => consulta.Where(p => p.Pasos.Count == 0),
            2 => consulta.Where(p => p.IndiceProceso == 0 && p.Pasos.Count > 0 && !p.Contratado),
            3 => consulta.Where(p => p.IndiceProceso == 1 && !p.Contratado),
            4 => consulta.Where(p => p.IndiceProceso == 2 && !p.Contratado),
            5 => consulta.Where(p => p.IndiceProceso == 3 && !p.Contratado),
            6 => consulta.Where(p => p.TieneCierreNegativo),
            7 => consulta.Where(p => p.Contratado),
            _ => consulta,
        };

        if (FiltroArea.SelectedItem is string area && area != "Todas")
            consulta = consulta.Where(p => p.Area == area);

        var nombresAbiertos = RepositorioEjemplo.PuestosAbiertos
            .Select(p => p.Nombre)
            .ToList();
        consulta = consulta.Where(p => nombresAbiertos.Any(nombre => MismoPuesto(p.Puesto, nombre)));

        _visibles = consulta.ToList();
        var idsVisibles = _visibles.Select(p => p.Id).ToHashSet(StringComparer.Ordinal);
        _seleccionados.RemoveWhere(id => !idsVisibles.Contains(id));

        var areaFiltro = FiltroArea.SelectedItem as string;
        var mostrarVacios = FiltroProceso.SelectedIndex <= 0;
        var puestos = RepositorioEjemplo.PuestosAbiertos
            .Where(p => CoincidePuesto(p, texto, areaFiltro) || _visibles.Any(v => MismoPuesto(v.Puesto, p.Nombre)))
            .Where(p => mostrarVacios || _visibles.Any(v => MismoPuesto(v.Puesto, p.Nombre)))
            .OrderBy(p => OrdenArea(p.Area))
            .ThenBy(p => p.Nombre, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Grupos.Children.Clear();
        string? areaActual = null;
        foreach (var puesto in puestos)
        {
            if (!string.Equals(areaActual, puesto.Area, StringComparison.Ordinal))
            {
                areaActual = puesto.Area;
                Grupos.Children.Add(new TextBlock
                {
                    Text = areaActual,
                    Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
                    Margin = new Thickness(0, 8, 0, 0),
                });
            }

            var personas = _visibles
                .Where(p => MismoPuesto(p.Puesto, puesto.Nombre))
                .OrderBy(p => OrdenPostulante(p))
                .ThenBy(p => p.Apellidos)
                .ThenBy(p => p.Nombres)
                .ToList();
            Grupos.Children.Add(CrearPuesto(puesto, personas));
        }

        Vacio.Visibility = puestos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        var enProceso = _visibles.Count(p => !p.Cerrada);
        var marcas = _seleccionados.Count;
        Subtitulo.Text = marcas == 0
            ? $"{puestos.Count} puestos · {enProceso} en proceso. Crea un puesto y agrega postulantes."
            : $"{marcas} marcados. Elige el nuevo estado y pulsa Cambiar estado.";
    }

    private static bool CoincidePuesto(PuestoTrabajo puesto, string texto, string? area)
    {
        if (area is not null && area != "Todas" && puesto.Area != area)
            return false;

        if (texto.Length == 0)
            return true;

        return puesto.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
               puesto.Area.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
               puesto.Funciones.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
               puesto.Estudios.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
               puesto.Responsabilidades.Contains(texto, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MismoPuesto(string actual, string nombre) =>
        string.Equals(actual.Trim(), nombre.Trim(), StringComparison.OrdinalIgnoreCase);

    private static int OrdenArea(string area)
    {
        var indice = RepositorioEjemplo.Areas.ToList().IndexOf(area);
        return indice < 0 ? int.MaxValue : indice;
    }

    private static int OrdenPostulante(Postulacion persona)
    {
        if (persona.Contratado)
            return 300;
        if (persona.TieneCierreNegativo)
            return 200;
        return persona.IndiceProceso;
    }

    private Border CrearPuesto(PuestoTrabajo puesto, IReadOnlyList<Postulacion> personas)
    {
        var todos = personas.All(p => _seleccionados.Contains(p.Id));
        var marcarPuesto = new CheckBox
        {
            IsChecked = todos && personas.Count > 0,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = personas.Count == 0 ? Visibility.Collapsed : Visibility.Visible,
        };
        marcarPuesto.Checked += (_, _) =>
        {
            foreach (var persona in personas)
                _seleccionados.Add(persona.Id);
            Aplicar();
        };
        marcarPuesto.Unchecked += (_, _) =>
        {
            if (personas.All(p => _seleccionados.Contains(p.Id)))
            {
                foreach (var persona in personas)
                    _seleccionados.Remove(persona.Id);
                Aplicar();
            }
        };

        var encabezado = new Grid { ColumnSpacing = 10 };
        encabezado.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        encabezado.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        encabezado.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(marcarPuesto, 0);
        var titulo = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        titulo.Children.Add(new TextBlock
        {
            Text = puesto.Nombre,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
        });
        titulo.Children.Add(new TextBlock
        {
            Text = personas.Count == 1 ? "1 postulante" : $"{personas.Count} postulantes",
            FontSize = 12,
            Opacity = 0.7,
        });
        Grid.SetColumn(titulo, 1);
        var agregar = new Button
        {
            Content = "Agregar postulante",
            VerticalAlignment = VerticalAlignment.Center,
        };
        agregar.Click += async (_, _) => await AbrirNuevaPostulacionAsync(puesto);
        var mas = new Button
        {
            Content = "Puesto",
            VerticalAlignment = VerticalAlignment.Center,
        };
        var menu = new MenuFlyout();
        var itemDetalle = new MenuFlyoutItem { Text = "Detallar puesto" };
        itemDetalle.Click += async (_, _) => await DetallarPuestoAsync(puesto);
        var itemCerrar = new MenuFlyoutItem { Text = "Cerrar postulación" };
        itemCerrar.Click += async (_, _) => await CerrarPuestoAsync(puesto, personas.Count);
        menu.Items.Add(itemDetalle);
        menu.Items.Add(itemCerrar);
        mas.Flyout = menu;
        var acciones = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        acciones.Children.Add(agregar);
        acciones.Children.Add(mas);
        Grid.SetColumn(acciones, 2);
        encabezado.Children.Add(marcarPuesto);
        encabezado.Children.Add(titulo);
        encabezado.Children.Add(acciones);

        var cuerpo = new StackPanel { Spacing = 12 };
        cuerpo.Children.Add(encabezado);
        if (puesto.TieneDetalle)
        {
            var franja = puesto.FranjaSueldo.Length > 0 ? $" · {puesto.FranjaSueldo}" : "";
            cuerpo.Children.Add(new Expander
            {
                Header = $"Detalle del puesto{franja}",
                Content = CrearDetalle(puesto),
                IsExpanded = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            });
        }

        cuerpo.Children.Add(new TextBlock
        {
            Text = "Postulantes",
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Opacity = 0.8,
        });
        if (personas.Count == 0)
        {
            cuerpo.Children.Add(new TextBlock
            {
                Text = "Todavía no hay postulantes. Usa Agregar postulante para el primero.",
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
            });
        }
        else
        {
            foreach (var persona in personas)
                cuerpo.Children.Add(CrearPostulante(persona));
        }

        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"],
            Child = cuerpo,
        };
    }

    private async Task DetallarPuestoAsync(PuestoTrabajo puesto)
    {
        var dialogo = new EditorPuestoDialog(puesto) { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            return;

        Aplicar();
    }

    private static StackPanel CrearDetalle(PuestoTrabajo puesto)
    {
        var panel = new StackPanel { Spacing = 4 };
        AgregarLinea(panel, "Funciones", puesto.Funciones);
        AgregarLinea(panel, "Estudios", puesto.Estudios);
        AgregarLinea(panel, "Responsabilidades", puesto.Responsabilidades);
        AgregarLinea(panel, "Franja de sueldo", puesto.FranjaSueldo);
        return panel;
    }

    private static void AgregarLinea(StackPanel panel, string titulo, string texto)
    {
        if (texto.Length == 0)
            return;

        panel.Children.Add(new TextBlock
        {
            Text = titulo,
            FontSize = 12,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
        });
        panel.Children.Add(new TextBlock
        {
            Text = texto,
            Opacity = 0.85,
            TextWrapping = TextWrapping.Wrap,
        });
    }

    private async Task CerrarPuestoAsync(PuestoTrabajo puesto, int postulantes)
    {
        var detalle = postulantes == 0
            ? $"Se archivará «{puesto.Nombre}» sin postulantes y dejará de aparecer en la lista."
            : postulantes == 1
                ? $"Se archivará «{puesto.Nombre}» con su postulante y dejará de aparecer en la lista."
                : $"Se archivará «{puesto.Nombre}» con sus {postulantes} postulantes y dejará de aparecer en la lista.";

        var dialogo = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Cerrar postulación",
            Content = detalle,
            PrimaryButtonText = "Cerrar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            return;

        RepositorioEjemplo.CerrarPuesto(puesto);
        AvisoMasivo.Severity = InfoBarSeverity.Success;
        AvisoMasivo.Title = "Postulación cerrada";
        AvisoMasivo.Message = $"«{puesto.Nombre}» quedó archivado.";
        AvisoMasivo.IsOpen = true;
        Aplicar();
    }

    private Grid CrearPostulante(Postulacion persona)
    {
        var marcar = new CheckBox
        {
            IsChecked = _seleccionados.Contains(persona.Id),
            VerticalAlignment = VerticalAlignment.Center,
        };
        marcar.Checked += (_, _) =>
        {
            _seleccionados.Add(persona.Id);
            ActualizarMarcas();
        };
        marcar.Unchecked += (_, _) =>
        {
            _seleccionados.Remove(persona.Id);
            ActualizarMarcas();
        };

        var abrir = new Button { Content = "Abrir", VerticalAlignment = VerticalAlignment.Center };
        abrir.Click += (_, _) => NavegacionApp.AbrirPostulacion(persona.Id);

        var fila = new Grid { ColumnSpacing = 12 };
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var foto = new PersonPicture { Width = 40, Height = 40, DisplayName = persona.NombreCompleto };
        Grid.SetColumn(foto, 1);
        var texto = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 2 };
        texto.Children.Add(new TextBlock { Text = persona.NombreCompleto, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        texto.Children.Add(new TextBlock { Text = persona.ResumenLista, Opacity = 0.75, TextWrapping = TextWrapping.Wrap });
        Grid.SetColumn(texto, 2);
        var estado = new Border
        {
            Padding = new Thickness(8, 2, 8, 2),
            VerticalAlignment = VerticalAlignment.Center,
            Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SubtleFillColorSecondaryBrush"],
            CornerRadius = new CornerRadius(4),
            Child = new TextBlock { FontSize = 12, Text = persona.EstadoTexto },
        };
        Grid.SetColumn(estado, 3);
        Grid.SetColumn(abrir, 4);
        fila.Children.Add(marcar);
        fila.Children.Add(foto);
        fila.Children.Add(texto);
        fila.Children.Add(estado);
        fila.Children.Add(abrir);
        return fila;
    }

    private void ActualizarMarcas()
    {
        var marcas = _seleccionados.Count;
        if (marcas == 0)
            Subtitulo.Text = "Marca postulantes y usa Cambiar estado para moverlos juntos.";
        else
            Subtitulo.Text = marcas == 1
                ? "1 marcado. Elige el nuevo estado y pulsa Cambiar estado."
                : $"{marcas} marcados. Elige el nuevo estado y pulsa Cambiar estado.";
    }
}
