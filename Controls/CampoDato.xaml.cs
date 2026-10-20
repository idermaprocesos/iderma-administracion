using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Controls;

public sealed partial class CampoDato : UserControl
{
    public static readonly DependencyProperty EtiquetaProperty =
        DependencyProperty.Register(nameof(Etiqueta), typeof(string), typeof(CampoDato), new PropertyMetadata("", Cambio));

    public static readonly DependencyProperty ValorProperty =
        DependencyProperty.Register(nameof(Valor), typeof(string), typeof(CampoDato), new PropertyMetadata("", Cambio));

    public CampoDato()
    {
        InitializeComponent();
    }

    public string Etiqueta
    {
        get => (string)GetValue(EtiquetaProperty);
        set => SetValue(EtiquetaProperty, value);
    }

    public string Valor
    {
        get => (string)GetValue(ValorProperty);
        set => SetValue(ValorProperty, value);
    }

    private static void Cambio(DependencyObject objeto, DependencyPropertyChangedEventArgs args)
    {
        ((CampoDato)objeto).Actualizar();
    }

    private void Actualizar()
    {
        if (EtiquetaTexto is null || ValorTexto is null)
            return;

        EtiquetaTexto.Text = Etiqueta;
        ValorTexto.Text = Valor;
    }
}
