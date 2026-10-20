using System.Diagnostics;
using IdermaAdministracion.Datos;
using IdermaAdministracion.Dialogos;
using IdermaAdministracion.Models;
using IdermaAdministracion.Servicios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace IdermaAdministracion.Pages;

public sealed partial class OrganigramaPage : Page
{
    public OrganigramaPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Pintar();
    }

    private void Pintar()
    {
        if (Lienzo is null)
            return;

        Lienzo.Children.Clear();
        OrganigramaDatos.CargarDesdePersonal();
        Lienzo.Children.Add(Rama(OrganigramaDatos.Raiz, Trazo()));
    }

    private const double AnchoVineta = 220;
    private const double Separacion = 16;

    private StackPanel Rama(NodoOrganigrama nodo, Brush trazo)
    {
        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 0,
        };
        panel.Children.Add(Tarjeta(nodo));
        if (Continuacion(nodo, trazo) is FrameworkElement cola)
            panel.Children.Add(cola);
        return panel;
    }

    private FrameworkElement? Continuacion(NodoOrganigrama nodo, Brush trazo)
    {
        if (nodo.Hijos.Count == 0)
            return null;

        var panel = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 0,
        };
        if (nodo.Hijos.Count == 1)
        {
            panel.Children.Add(LineaVertical(16, trazo, nodo.Hijos[0].Asesoria));
            panel.Children.Add(Rama(nodo.Hijos[0], trazo));
            return panel;
        }

        panel.Children.Add(LineaVertical(16, trazo, punteada: false));
        panel.Children.Add(Hermanos(nodo.Hijos, trazo));
        return panel;
    }

    private Grid Hermanos(IReadOnlyList<NodoOrganigrama> hijos, Brush trazo)
    {
        var grilla = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var hijo in hijos)
        {
            var mitad = (AnchoDe(hijo) + Separacion) / 2;
            grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(mitad) });
            grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(mitad) });
        }

        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        if (hijos.Count > 1)
        {
            var barra = new Border
            {
                Height = 2,
                VerticalAlignment = VerticalAlignment.Center,
                Background = trazo,
            };
            Grid.SetColumn(barra, 1);
            Grid.SetColumnSpan(barra, hijos.Count * 2 - 2);
            grilla.Children.Add(barra);
        }

        for (var i = 0; i < hijos.Count; i++)
        {
            var caida = LineaVertical(16, trazo, hijos[i].Asesoria);
            caida.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetRow(caida, 1);
            Grid.SetColumn(caida, i * 2);
            grilla.Children.Add(caida);

            if (hijos[i].Agrupa)
            {
                var jefes = Hermanos(hijos[i].Hijos, trazo);
                jefes.HorizontalAlignment = HorizontalAlignment.Center;
                jefes.VerticalAlignment = VerticalAlignment.Top;
                Grid.SetRow(jefes, 2);
                Grid.SetColumn(jefes, i * 2);
                Grid.SetColumnSpan(jefes, 2);
                grilla.Children.Add(jefes);

                var abajo = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 0,
                };
                abajo.Children.Add(Puente(hijos[i].Hijos, trazo));
                abajo.Children.Add(Hermanos(hijos[i].Compartidos, trazo));
                Grid.SetRow(abajo, 3);
                Grid.SetColumn(abajo, i * 2);
                Grid.SetColumnSpan(abajo, 2);
                grilla.Children.Add(abajo);
                continue;
            }

            var tarjeta = Tarjeta(hijos[i]);
            tarjeta.VerticalAlignment = VerticalAlignment.Stretch;
            tarjeta.Margin = new Thickness(Separacion / 2, 0, Separacion / 2, 0);
            Grid.SetRow(tarjeta, 2);
            Grid.SetColumn(tarjeta, i * 2);
            Grid.SetColumnSpan(tarjeta, 2);
            grilla.Children.Add(tarjeta);

            if (Continuacion(hijos[i], trazo) is not FrameworkElement cola)
                continue;

            cola.VerticalAlignment = VerticalAlignment.Top;
            cola.HorizontalAlignment = HorizontalAlignment.Stretch;
            Grid.SetRow(cola, 3);
            Grid.SetColumn(cola, i * 2);
            Grid.SetColumnSpan(cola, 2);
            grilla.Children.Add(cola);
        }

        return grilla;
    }

    private static double AnchoDe(NodoOrganigrama nodo)
    {
        if (nodo.Agrupa)
            return Math.Max(AnchoFila(nodo.Hijos), AnchoFila(nodo.Compartidos));
        if (nodo.Hijos.Count == 0)
            return AnchoVineta;
        if (nodo.Hijos.Count == 1)
            return Math.Max(AnchoVineta, AnchoDe(nodo.Hijos[0]));
        return Math.Max(AnchoVineta, AnchoFila(nodo.Hijos));
    }

    private static double AnchoFila(IReadOnlyList<NodoOrganigrama> nodos) =>
        nodos.Count == 0 ? 0 : nodos.Sum(AnchoDe) + Separacion * nodos.Count;

    private Grid Puente(IReadOnlyList<NodoOrganigrama> jefes, Brush trazo)
    {
        var grilla = new Grid { HorizontalAlignment = HorizontalAlignment.Center };
        foreach (var jefe in jefes)
        {
            var mitad = (AnchoDe(jefe) + Separacion) / 2;
            grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(mitad) });
            grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(mitad) });
        }

        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var barra = new Border
        {
            Height = 2,
            VerticalAlignment = VerticalAlignment.Bottom,
            Background = trazo,
        };
        Grid.SetColumn(barra, 1);
        Grid.SetColumnSpan(barra, jefes.Count * 2 - 2);
        grilla.Children.Add(barra);

        for (var i = 0; i < jefes.Count; i++)
        {
            var caida = LineaVertical(14, trazo, punteada: false);
            caida.HorizontalAlignment = HorizontalAlignment.Right;
            caida.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(caida, i * 2);
            grilla.Children.Add(caida);
        }

        var centro = LineaVertical(14, trazo, punteada: false);
        centro.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetRow(centro, 1);
        Grid.SetColumn(centro, jefes.Count - 1);
        grilla.Children.Add(centro);
        return grilla;
    }

    private FrameworkElement LineaVertical(double alto, Brush trazo, bool punteada)
    {
        if (!punteada)
        {
            return new Border
            {
                Width = 2,
                Height = alto,
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = trazo,
            };
        }

        return new Line
        {
            X1 = 1,
            X2 = 1,
            Y1 = 0,
            Y2 = alto,
            Stroke = trazo,
            StrokeThickness = 2,
            StrokeDashArray = new DoubleCollection { 1.5, 1.5 },
            Width = 2,
            Height = alto,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
    }

    private Border Tarjeta(NodoOrganigrama nodo)
    {
        var cuerpo = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Top };
        cuerpo.Children.Add(new TextBlock
        {
            Text = nodo.Titulo,
            FontSize = 14,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        });

        if (nodo.Personas.Count > 0)
        {
            var personas = new StackPanel { Spacing = 8 };
            foreach (var persona in nodo.Personas)
            {
                var bloque = new StackPanel { Spacing = 0 };
                bloque.Children.Add(new TextBlock
                {
                    Text = persona.Nombre,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Center,
                });
                if (!string.IsNullOrWhiteSpace(persona.Cargo))
                {
                    bloque.Children.Add(new TextBlock
                    {
                        Text = persona.Cargo,
                        FontSize = 12,
                        Opacity = 0.7,
                        TextWrapping = TextWrapping.Wrap,
                        TextAlignment = TextAlignment.Center,
                    });
                }

                personas.Children.Add(bloque);
            }

            cuerpo.Children.Add(personas);
        }

        var acciones = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            Margin = new Thickness(0, 4, 0, 0),
        };
        acciones.Children.Add(BotonAccion("Agregar persona", async (_, _) => await AgregarAsync(nodo)));
        acciones.Children.Add(BotonAccion("Editar", async (_, _) => await EditarAsync(nodo)));

        var contenido = new Grid();
        contenido.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        contenido.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        contenido.Children.Add(cuerpo);
        Grid.SetRow(acciones, 1);
        contenido.Children.Add(acciones);

        return new Border
        {
            Width = AnchoVineta,
            MinHeight = 148,
            Padding = new Thickness(12),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
            Style = (Style)Application.Current.Resources["TarjetaEstilo"],
            Child = contenido,
        };
    }

    private static Button BotonAccion(string texto, RoutedEventHandler alPulsar)
    {
        var boton = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(8, 6, 8, 6),
            Content = new TextBlock
            {
                Text = texto,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.WrapWholeWords,
            },
        };
        boton.Click += alPulsar;
        return boton;
    }

    private async void ExportarPdf_Click(object sender, RoutedEventArgs e)
    {
        var ventana = MainWindow.Instancia;
        if (ventana is null)
            return;

        var selector = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = "Organigrama Iderma Capilar",
        };
        selector.FileTypeChoices.Add("Documento PDF", new List<string> { ".pdf" });
        InitializeWithWindow.Initialize(selector, WindowNative.GetWindowHandle(ventana));

        var archivo = await selector.PickSaveFileAsync();
        if (archivo is null)
            return;

        try
        {
            OrganigramaPdf.Guardar(archivo.Path);
            Process.Start(new ProcessStartInfo(archivo.Path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            var aviso = new ContentDialog
            {
                Title = "No se pudo crear el PDF",
                Content = ex.Message,
                CloseButtonText = "Cerrar",
                XamlRoot = XamlRoot,
            };
            await aviso.ShowAsync();
        }
    }

    private async Task EditarAsync(NodoOrganigrama nodo)
    {
        var dialogo = new EditorNodoDialog(nodo) { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            return;

        RepositorioEjemplo.ReemplazarArea(nodo.Titulo, dialogo.Personas);
        Pintar();
    }

    private async Task AgregarAsync(NodoOrganigrama nodo)
    {
        var dialogo = new AgregarPersonaDialog(nodo.Titulo) { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            return;

        RepositorioEjemplo.AgregarEnArea(
            dialogo.NombreIngresado,
            dialogo.ApellidoIngresado,
            dialogo.CargoIngresado,
            nodo.Titulo,
            dialogo.Rol);
        Pintar();
    }

    private static Brush Trazo() =>
        Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
        ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
}
