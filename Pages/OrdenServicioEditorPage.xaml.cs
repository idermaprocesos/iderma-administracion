using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using IdermaAdministracion.Servicios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace IdermaAdministracion.Pages;

public sealed partial class OrdenServicioEditorPage : Page
{
    private readonly ObservableCollection<LineaOrdenServicio> _lineas = [];
    private Guid _ordenId = Guid.NewGuid();
    private DateTimeOffset _fechaRegistro = DateTimeOffset.Now;
    private bool _aplicada;

    public OrdenServicioEditorPage()
    {
        InitializeComponent();
        Lista.ItemsSource = _lineas;
        ComboEstado.ItemsSource = CalculoOrdenServicio.EstadosDocumento;
        ComboEstado.SelectedIndex = 0;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _lineas.Clear();
        if (e.Parameter is OrdenServicioEdicion edicion)
        {
            _ordenId = edicion.Id == Guid.Empty ? Guid.NewGuid() : edicion.Id;
            _fechaRegistro = edicion.FechaRegistro == default ? DateTimeOffset.Now : edicion.FechaRegistro;
            _aplicada = edicion.AmpliacionAplicada;
            if (edicion.Documento is { } documento)
                CargarEncabezado(documento);
            else
                EncabezadoNuevo();

            foreach (var linea in edicion.Lineas)
                Agregar(linea);
        }
        else
        {
            EncabezadoNuevo();
        }

        ActualizarTotal();
    }

    private void EncabezadoNuevo()
    {
        var hoy = DateTime.Today;
        CajaMes.Text = hoy.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("es-PE"));
        FechaElaboracion.Date = DateTimeOffset.Now;
        CajaCobertura.Text = $"Ampliación desde {hoy:dd/MM/yyyy}";
        ComboEstado.SelectedIndex = 0;
    }

    private void CargarEncabezado(OrdenServicioDocumento documento)
    {
        CajaMes.Text = documento.MesAnio;
        CajaCobertura.Text = documento.PeriodoCobertura;
        CajaElaborado.Text = documento.ElaboradoPor;
        CajaRevisado.Text = documento.RevisadoPor;
        if (DateTime.TryParseExact(documento.FechaElaboracion, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
            FechaElaboracion.Date = new DateTimeOffset(fecha);
        else
            FechaElaboracion.Date = DateTimeOffset.Now;

        var indice = Array.FindIndex(
            CalculoOrdenServicio.EstadosDocumento,
            estado => estado.Equals(documento.Estado, StringComparison.OrdinalIgnoreCase));
        ComboEstado.SelectedIndex = indice >= 0 ? indice : 0;
    }

    private void Agregar(LineaOrdenServicio linea)
    {
        if (_lineas.Any(l => l.ColaboradorId == linea.ColaboradorId && linea.ColaboradorId.Length > 0))
            return;

        linea.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(LineaOrdenServicio.MesesAmpliar))
                ActualizarTotal();
        };
        _lineas.Add(linea);
        ResumenLineas.Text = _lineas.Count == 1 ? "1 contrato" : $"{_lineas.Count} contratos";
    }

    private void ActualizarTotal()
    {
        var total = _lineas.Sum(l => l.ImporteConIgv);
        TotalGeneral.Text = $"Total con IGV: {total.ToString("C", CultureInfo.GetCultureInfo("es-PE"))}";
        ResumenLineas.Text = _lineas.Count == 1 ? "1 contrato" : $"{_lineas.Count} contratos";
    }

    private void BuscarPersona_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
            return;

        var texto = sender.Text.Trim();
        sender.ItemsSource = texto.Length < 2
            ? null
            : RepositorioEjemplo.Colaboradores
                .Where(c => c.EstaActivo)
                .Where(c => c.NombreCompleto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || c.Documento.Contains(texto, StringComparison.OrdinalIgnoreCase)
                    || c.Contrato.Cargo.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .Take(8)
                .Select(c => c.NombreCompleto)
                .ToList();
    }

    private void BuscarPersona_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        var nombre = args.SelectedItem as string;
        var persona = RepositorioEjemplo.Colaboradores.FirstOrDefault(c => c.NombreCompleto == nombre);
        if (persona is null)
            return;

        Agregar(OrdenesServicioRepositorio.DesdeColaborador(persona));
        ActualizarTotal();
        sender.Text = "";
    }

    private void Quitar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button boton || boton.Tag is not string id)
            return;

        var linea = _lineas.FirstOrDefault(l => l.ColaboradorId == id);
        if (linea is null)
            return;

        _lineas.Remove(linea);
        ActualizarTotal();
    }

    private void Volver_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private void Guardar_Click(object sender, RoutedEventArgs e) =>
        Registrar(Capturar());

    private async void Pdf_Click(object sender, RoutedEventArgs e)
    {
        var documento = Capturar();
        if (!Registrar(documento))
            return;
        await ExportarAsync(documento, "Documento PDF", ".pdf", OrdenServicioPdf.Guardar);
    }

    private async void Excel_Click(object sender, RoutedEventArgs e)
    {
        var documento = Capturar();
        if (!Registrar(documento))
            return;
        await ExportarAsync(documento, "Libro de Excel", ".xlsx", OrdenServicioExcel.Guardar);
    }

    private bool Registrar(OrdenServicioDocumento documento)
    {
        if (_lineas.Count == 0)
        {
            Aviso.Severity = InfoBarSeverity.Warning;
            Aviso.Title = "Sin contratos";
            Aviso.Message = "Agregue al menos un colaborador.";
            Aviso.IsOpen = true;
            return false;
        }

        var orden = new OrdenServicioRegistrada
        {
            Id = _ordenId,
            FechaRegistro = _fechaRegistro,
            AmpliacionAplicada = _aplicada,
            Documento = documento,
        };

        if (documento.Estado.Equals("Aprobada", StringComparison.OrdinalIgnoreCase) && !_aplicada)
        {
            var n = OrdenesServicioRepositorio.AplicarAmpliacion(documento);
            orden.AmpliacionAplicada = n > 0;
            _aplicada = orden.AmpliacionAplicada;
        }

        OrdenesServicioRepositorio.Guardar(orden);
        Aviso.Severity = InfoBarSeverity.Success;
        Aviso.Title = orden.AmpliacionAplicada && documento.Estado == "Aprobada"
            ? "Orden aprobada y contratos ampliados"
            : "Orden guardada";
        Aviso.Message = $"{documento.Estado} · {documento.Lineas.Count} contrato(s).";
        Aviso.IsOpen = true;
        return true;
    }

    private OrdenServicioDocumento Capturar()
    {
        var fecha = FechaElaboracion.Date?.DateTime ?? DateTime.Today;
        return new OrdenServicioDocumento
        {
            MesAnio = CajaMes.Text.Trim(),
            FechaElaboracion = fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            ElaboradoPor = CajaElaborado.Text.Trim(),
            RevisadoPor = CajaRevisado.Text.Trim(),
            PeriodoCobertura = CajaCobertura.Text.Trim(),
            Estado = ComboEstado.SelectedItem as string ?? "Borrador",
            Lineas = _lineas.Select(l => l.Exportar()).ToList(),
        };
    }

    private async Task ExportarAsync(OrdenServicioDocumento documento, string etiqueta, string extension, Action<string, OrdenServicioDocumento> guardar)
    {
        var ventana = MainWindow.Instancia;
        if (ventana is null)
            return;

        var selector = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"Orden-servicio-{DateTime.Today:yyyyMMdd}",
        };
        selector.FileTypeChoices.Add(etiqueta, new List<string> { extension });
        InitializeWithWindow.Initialize(selector, WindowNative.GetWindowHandle(ventana));
        var archivo = await selector.PickSaveFileAsync();
        if (archivo is null)
            return;

        try
        {
            guardar(archivo.Path, documento);
            Aviso.Severity = InfoBarSeverity.Success;
            Aviso.Title = "Archivo listo para imprimir";
            Aviso.Message = archivo.Name;
            Aviso.IsOpen = true;
            Process.Start(new ProcessStartInfo(archivo.Path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Aviso.Severity = InfoBarSeverity.Error;
            Aviso.Title = "No se pudo crear el archivo";
            Aviso.Message = ex.Message;
            Aviso.IsOpen = true;
        }
    }
}
