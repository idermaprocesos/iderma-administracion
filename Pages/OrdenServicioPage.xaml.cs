using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class OrdenServicioPage : Page
{
    private readonly HashSet<string> _marcados = new(StringComparer.Ordinal);
    private List<ContratoServicioItem> _visibles = [];

    public OrdenServicioPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Refrescar();
    }

    private void CajaBusqueda_TextChanged(object sender, TextChangedEventArgs e) => Refrescar();

    private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
            Refrescar();
    }

    private void Refrescar()
    {
        var texto = (CajaBusqueda.Text ?? "").Trim();
        var estado = (FiltroEstado.SelectedItem as ComboBoxItem)?.Content as string ?? "Todos";
        var pago = (FiltroPago.SelectedItem as ComboBoxItem)?.Content as string ?? "Todos";

        _visibles = RepositorioEjemplo.Colaboradores
            .Where(c => c.EstaActivo)
            .Where(c => Coincide(c, texto, estado, pago))
            .OrderBy(c => c.Contrato.FechaFinContrato ?? DateTime.MaxValue)
            .ThenBy(c => c.Apellidos)
            .Select(c => new ContratoServicioItem(c, _marcados.Contains(c.Id)))
            .ToList();

        Lista.ItemsSource = _visibles;
        var n = _marcados.Count;
        ResumenMarcas.Text = n == 0
            ? $"{_visibles.Count} contratos activos"
            : $"{n} marcado(s) · {_visibles.Count} en la lista";
    }

    private static bool Coincide(Colaborador persona, string texto, string estado, string pago)
    {
        if (pago == "Planilla" && persona.Contrato.Modalidad != ModalidadPago.Planilla)
            return false;
        if (pago == "Recibo por honorarios" && persona.Contrato.Modalidad != ModalidadPago.ReciboPorHonorarios)
            return false;
        if (estado == "Por vencer" && !persona.Contrato.ProximoAVencer)
            return false;
        if (estado == "Vencidos" && !persona.Contrato.ContratoVencido)
            return false;

        if (texto.Length == 0)
            return true;

        var bolsa = string.Join(" ",
            persona.NombreCompleto,
            persona.Documento,
            persona.Contrato.Cargo,
            persona.Contrato.Area,
            persona.Contrato.NumeroContrato,
            persona.Contrato.TipoContrato);
        return bolsa.Contains(texto, StringComparison.OrdinalIgnoreCase);
    }

    private void Marca_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox caja || caja.Tag is not string id)
            return;

        if (caja.IsChecked == true)
            _marcados.Add(id);
        else
            _marcados.Remove(id);

        ResumenMarcas.Text = $"{_marcados.Count} marcado(s) · {_visibles.Count} en la lista";
    }

    private void Generar_Click(object sender, RoutedEventArgs e)
    {
        var personas = _marcados
            .Select(RepositorioEjemplo.Buscar)
            .OfType<Colaborador>()
            .Where(c => c.EstaActivo)
            .ToList();
        if (personas.Count == 0)
        {
            Aviso.Severity = InfoBarSeverity.Warning;
            Aviso.Title = "Seleccione contratos";
            Aviso.Message = "Marque al menos un colaborador para ampliar su contrato.";
            Aviso.IsOpen = true;
            return;
        }

        var lineas = personas.Select(p => OrdenesServicioRepositorio.DesdeColaborador(p)).ToList();
        Frame.Navigate(typeof(OrdenServicioEditorPage), new OrdenServicioEdicion
        {
            Lineas = lineas,
        });
    }

    private void Borradores_Click(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(BorradoresServicioPage));

    private void Anteriores_Click(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(RegistroServicioPage));
}

public sealed class ContratoServicioItem
{
    public ContratoServicioItem(Colaborador persona, bool marcado)
    {
        Id = persona.Id;
        Nombre = persona.NombreCompleto;
        Resumen = $"{persona.Contrato.Cargo} · {persona.Contrato.Area} · {persona.Documento}";
        Contrato = string.IsNullOrWhiteSpace(persona.Contrato.NumeroContrato)
            ? persona.Contrato.TipoContrato
            : persona.Contrato.NumeroContrato;
        Vigencia = persona.Contrato.FinContratoTexto;
        Mensual = persona.Contrato.SueldoTexto;
        Marcado = marcado;
    }

    public string Id { get; }
    public string Nombre { get; }
    public string Resumen { get; }
    public string Contrato { get; }
    public string Vigencia { get; }
    public string Mensual { get; }
    public bool Marcado { get; set; }
}
