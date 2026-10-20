using IdermaAdministracion.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace IdermaAdministracion.Controls;

public static class LineaPasos
{
    private static readonly SolidColorBrush Teal = Brocha(42, 212, 190);
    private static readonly SolidColorBrush Gris = Brocha(138, 138, 138);
    private static readonly SolidColorBrush GrisCirculo = Brocha(154, 154, 154);
    private static readonly SolidColorBrush Rojo = Brocha(225, 91, 100);
    private static readonly SolidColorBrush Blanco = new(Colors.White);

    public static void Pintar(Panel host, Postulacion postulacion)
    {
        host.Children.Clear();

        var etapas = PasosContratacion.Orden;
        var grilla = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };

        for (var i = 0; i < etapas.Count; i++)
        {
            grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            if (i < etapas.Count - 1)
                grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grilla.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        for (var i = 0; i < etapas.Count; i++)
        {
            var columna = i * 2;
            var etapa = etapas[i];
            var registro = postulacion.De(etapa);

            var circulo = new Grid
            {
                Width = 28,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            circulo.Children.Add(new Border
            {
                Background = Fondo(registro),
                CornerRadius = new CornerRadius(14),
            });

            if (registro is not null)
            {
                circulo.Children.Add(new FontIcon
                {
                    Glyph = registro.Avanza ? "\uE73E" : "\uE711",
                    FontSize = 12,
                    Foreground = Blanco,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                });
            }

            Grid.SetColumn(circulo, columna);
            grilla.Children.Add(circulo);

            var etiqueta = new TextBlock
            {
                Text = PasosContratacion.Titulo(etapa),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(8, 10, 8, 0),
                FontSize = 13,
                Foreground = Blanco,
            };
            Grid.SetRow(etiqueta, 1);
            Grid.SetColumn(etiqueta, columna);
            grilla.Children.Add(etiqueta);

            if (i >= etapas.Count - 1)
                continue;

            var izquierda = postulacion.De(etapa);
            var linea = new Border
            {
                Height = 4,
                Margin = new Thickness(0, 12, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                CornerRadius = new CornerRadius(2),
                Background = izquierda is { Avanza: true } ? Teal : Gris,
            };
            Grid.SetColumn(linea, columna + 1);
            grilla.Children.Add(linea);
        }

        host.Children.Add(grilla);
    }

    private static SolidColorBrush Fondo(RegistroPaso? registro)
    {
        if (registro is null)
            return GrisCirculo;

        return registro.Avanza ? Teal : Rojo;
    }

    private static SolidColorBrush Brocha(byte r, byte g, byte b) =>
        new(Color.FromArgb(255, r, g, b));
}
