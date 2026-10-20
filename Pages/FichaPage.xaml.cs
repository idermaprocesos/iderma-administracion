using IdermaAdministracion.Datos;
using IdermaAdministracion.Dialogos;
using IdermaAdministracion.Models;
using IdermaAdministracion.Servicios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class FichaPage : Page
{
    private Colaborador? _persona;

    public FichaPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        Mostrar(RepositorioEjemplo.Buscar(e.Parameter as string));
    }

    private async void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (_persona is null)
            return;

        var dialogo = new EditorPersonaDialog(_persona, nueva: false) { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() == ContentDialogResult.Primary)
            Mostrar(RepositorioEjemplo.Buscar(_persona.Id));
    }

    private async void Informe_Click(object sender, RoutedEventArgs e)
    {
        if (_persona is null)
            return;

        var informe = InformeLaboral.Crear(_persona);
        var dialogo = new InformeDialog(informe) { XamlRoot = XamlRoot };
        await dialogo.ShowAsync();
    }

    private void Mostrar(Colaborador? persona)
    {
        _persona = persona;
        if (persona is null)
        {
            FichaRaiz.Visibility = Visibility.Collapsed;
            Vacio.Visibility = Visibility.Visible;
            return;
        }

        FichaRaiz.Visibility = Visibility.Visible;
        Vacio.Visibility = Visibility.Collapsed;
        FichaRaiz.DataContext = null;
        FichaRaiz.DataContext = persona;
        BloqueMedico.Visibility = persona.Rol == RolClinica.Doctor ? Visibility.Visible : Visibility.Collapsed;
        AvisoEstado.Severity = persona.EstaActivo ? InfoBarSeverity.Success : InfoBarSeverity.Warning;
        AvisoEstado.Title = persona.EstaActivo ? "Activo" : "Retirado";
        AvisoEstado.Message = persona.ResumenContrato;
        AvisoEstado.IsOpen = true;
        var alerta = persona.Contrato.AlertaVencimiento;
        AvisoVencimiento.IsOpen = alerta.Length > 0;
        AvisoVencimiento.Severity = persona.Contrato.ContratoVencido ? InfoBarSeverity.Error : InfoBarSeverity.Warning;
        AvisoVencimiento.Title = persona.Contrato.ContratoVencido ? "Contrato vencido" : "Contrato por vencer";
        AvisoVencimiento.Message = alerta.Length == 0 || persona.Contrato.FechaFinContrato is null
            ? ""
            : $"{alerta} ({persona.Contrato.PlazoTexto}, hasta el {Formato.FechaLarga(persona.Contrato.FechaFinContrato.Value)}).";
        PintarFirmas(persona);
    }

    private void PintarFirmas(Colaborador persona)
    {
        DocumentosPorFirmar.Asegurar(persona.Contrato, conEjemplos: false);
        ResumenFirmas.Text = persona.Contrato.FirmasCompletas
            ? $"{persona.Contrato.FirmasTexto}. Todo firmado. Puedes corregir la fecha de cada uno."
            : $"{persona.Contrato.FirmasTexto}. Marca el documento y ajusta la fecha de firma.";

        ListaFirmas.Children.Clear();
        foreach (var documento in persona.Contrato.Documentos)
        {
            var fila = new Grid { ColumnSpacing = 12 };
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });

            var marca = new CheckBox
            {
                Content = documento.Nombre,
                IsChecked = documento.Firmado,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var fecha = new CalendarDatePicker
            {
                Header = "Fecha de firma",
                PlaceholderText = "Elegir fecha",
                Date = ComoFecha(documento.FechaFirma),
                IsEnabled = documento.Firmado,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            marca.Checked += (_, _) => CambiarFirma(persona, documento, fecha, true);
            marca.Unchecked += (_, _) => CambiarFirma(persona, documento, fecha, false);
            fecha.DateChanged += (_, args) =>
            {
                if (!documento.Firmado)
                    return;

                documento.FechaFirma = args.NewDate?.Date;
                if (documento.Codigo == "imagen")
                    persona.Contrato.FechaConsentimientoImagen = documento.FechaFirma;
            };

            Grid.SetColumn(fecha, 1);
            fila.Children.Add(marca);
            fila.Children.Add(fecha);
            ListaFirmas.Children.Add(fila);
        }
    }

    private void CambiarFirma(Colaborador persona, DocumentoFirma documento, CalendarDatePicker fecha, bool firmado)
    {
        documento.Firmado = firmado;
        documento.FechaFirma = firmado ? documento.FechaFirma ?? DateTime.Today : null;
        fecha.IsEnabled = firmado;
        fecha.Date = ComoFecha(documento.FechaFirma);
        if (documento.Codigo == "imagen")
        {
            persona.Contrato.AceptaUsoImagen = firmado;
            persona.Contrato.FechaConsentimientoImagen = firmado ? documento.FechaFirma : null;
            if (!firmado)
                persona.Contrato.VencimientoImagen = null;
            FichaRaiz.DataContext = null;
            FichaRaiz.DataContext = persona;
        }

        ResumenFirmas.Text = persona.Contrato.FirmasCompletas
            ? $"{persona.Contrato.FirmasTexto}. Todo firmado. Puedes corregir la fecha de cada uno."
            : $"{persona.Contrato.FirmasTexto}. Marca el documento y ajusta la fecha de firma.";
    }

    private static DateTimeOffset? ComoFecha(DateTime? fecha) =>
        fecha is null ? null : new DateTimeOffset(DateTime.SpecifyKind(fecha.Value, DateTimeKind.Unspecified));

    private void Volver_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }
}
