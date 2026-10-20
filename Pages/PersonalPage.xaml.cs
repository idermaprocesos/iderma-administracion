using IdermaAdministracion.Datos;
using IdermaAdministracion.Dialogos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class PersonalPage : Page
{
    private RolClinica? _rol;

    public PersonalPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        var rol = e.Parameter as string == "doctores" ? RolClinica.Doctor : RolClinica.Trabajador;
        var cambio = _rol != rol;
        _rol = rol;

        Titulo.Text = rol == RolClinica.Doctor ? "Doctores" : "Trabajadores";
        BotonAgregar.Content = rol == RolClinica.Doctor ? "Agregar doctor" : "Agregar trabajador";
        ActualizarResumen();

        if (cambio)
        {
            BuscarBox.Text = "";
            FiltroEstado.SelectedIndex = 0;
            FiltroPago.SelectedIndex = 0;
            FiltroImagen.SelectedIndex = 0;
        }

        Aplicar();
    }

    private async void Agregar_Click(object sender, RoutedEventArgs e)
    {
        if (_rol is null)
            return;

        var borrador = RepositorioEjemplo.CrearBorrador(_rol.Value);
        var dialogo = new EditorPersonaDialog(borrador, nueva: true) { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() != ContentDialogResult.Primary)
            return;

        BuscarBox.Text = "";
        FiltroEstado.SelectedIndex = 0;
        FiltroPago.SelectedIndex = 0;
        FiltroImagen.SelectedIndex = 0;
        ActualizarResumen();
        Aplicar();
    }

    private void ActualizarResumen()
    {
        if (_rol is null)
            return;

        var baseLista = RepositorioEjemplo.Colaboradores.Where(c => c.Rol == _rol).ToList();
        var activos = baseLista.Count(c => c.EstaActivo);
        var retirados = baseLista.Count - activos;
        Subtitulo.Text = _rol == RolClinica.Doctor
            ? $"{activos} activos · {retirados} retirados. Abre una ficha para ver o editar el contrato."
            : $"{activos} activos · {retirados} retirados. Abre una ficha para ver o editar ingreso, retiro, sueldo, planilla, honorarios e imagen.";
    }

    private void BuscarBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            Aplicar();
    }

    private void Filtro_SelectionChanged(object sender, SelectionChangedEventArgs e) => Aplicar();

    private void Lista_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is Colaborador persona)
            NavegacionApp.AbrirFicha(persona.Id);
    }

    private void Aplicar()
    {
        if (_rol is null || Lista is null)
            return;

        IEnumerable<Colaborador> consulta = RepositorioEjemplo.Colaboradores.Where(c => c.Rol == _rol);
        var texto = BuscarBox.Text?.Trim() ?? "";
        if (texto.Length > 0)
        {
            consulta = consulta.Where(p =>
                p.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Documento.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Contrato.Area.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Contrato.Cargo.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Contrato.Sede.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Especialidad.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                p.Contrato.CodigoPago.Contains(texto, StringComparison.OrdinalIgnoreCase));
        }

        consulta = FiltroEstado.SelectedIndex switch
        {
            1 => consulta.Where(p => p.EstaActivo),
            2 => consulta.Where(p => !p.EstaActivo),
            3 => consulta.Where(p => p.Contrato.AlertaVencimiento.Length > 0),
            _ => consulta,
        };
        consulta = FiltroPago.SelectedIndex switch
        {
            1 => consulta.Where(p => p.Contrato.Modalidad == ModalidadPago.Planilla),
            2 => consulta.Where(p => p.Contrato.Modalidad == ModalidadPago.ReciboPorHonorarios),
            _ => consulta,
        };
        consulta = FiltroImagen.SelectedIndex switch
        {
            1 => consulta.Where(p => p.Contrato.AceptaUsoImagen),
            2 => consulta.Where(p => !p.Contrato.AceptaUsoImagen),
            _ => consulta,
        };

        var lista = consulta
            .OrderBy(p => p.EstaActivo ? 0 : 1)
            .ThenBy(p => p.Apellidos)
            .ToList();

        Lista.ItemsSource = lista;
        Vacio.Visibility = lista.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        MostrarAlerta();
    }

    private void MostrarAlerta()
    {
        if (_rol is null || AvisoVencimiento is null)
            return;

        var urgentes = RepositorioEjemplo.Colaboradores
            .Where(c => c.Rol == _rol && c.Contrato.AlertaVencimiento.Length > 0)
            .OrderBy(c => c.Contrato.DiasParaVencer)
            .ToList();
        if (urgentes.Count == 0)
        {
            AvisoVencimiento.IsOpen = false;
            return;
        }

        var vencidos = urgentes.Count(c => c.Contrato.ContratoVencido);
        AvisoVencimiento.Severity = vencidos > 0 ? InfoBarSeverity.Error : InfoBarSeverity.Warning;
        AvisoVencimiento.Title = vencidos > 0
            ? "Hay contratos vencidos o por vencer"
            : "Hay contratos por vencer";
        AvisoVencimiento.Message = string.Join(" ", urgentes.Select(c =>
            $"{c.NombreCompleto}: {c.Contrato.AlertaVencimiento}."));
        AvisoVencimiento.IsOpen = true;
    }
}
