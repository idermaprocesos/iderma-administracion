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

public sealed partial class OrdenCompraAdminEditorPage : Page
{
    private readonly ObservableCollection<LineaOrdenCompraAdmin> _lineas = [];
    private Guid _ordenId = Guid.NewGuid();
    private DateTimeOffset _fechaRegistro = DateTimeOffset.Now;

    public OrdenCompraAdminEditorPage()
    {
        InitializeComponent();
        Lista.ItemsSource = _lineas;
        ComboEstado.ItemsSource = CalculoOrdenCompraAdmin.EstadosDocumento;
        ComboEstado.SelectedIndex = 0;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _lineas.Clear();
        if (e.Parameter is OrdenCompraAdminEdicion edicion)
        {
            _ordenId = edicion.Id == Guid.Empty ? Guid.NewGuid() : edicion.Id;
            _fechaRegistro = edicion.FechaRegistro == default ? DateTimeOffset.Now : edicion.FechaRegistro;
            if (edicion.Documento is { } documento)
                Cargar(documento);
            else
                Nuevo();

            foreach (var linea in edicion.Lineas)
                Agregar(linea);
        }
        else
        {
            Nuevo();
            Agregar(new LineaOrdenCompraAdmin());
        }

        ActualizarTotal();
    }

    private void Nuevo()
    {
        CajaMes.Text = DateTime.Today.ToString("MMMM yyyy", CultureInfo.GetCultureInfo("es-PE"));
        FechaElaboracion.Date = DateTimeOffset.Now;
        ComboEstado.SelectedIndex = 0;
    }

    private void Cargar(OrdenCompraAdminDocumento documento)
    {
        CajaMes.Text = documento.MesAnio;
        CajaProveedor.Text = documento.Proveedor;
        CajaCobertura.Text = documento.PeriodoCobertura;
        CajaElaborado.Text = documento.ElaboradoPor;
        CajaRevisado.Text = documento.RevisadoPor;
        if (DateTime.TryParseExact(documento.FechaElaboracion, "dd/MM/yyyy", CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var fecha))
            FechaElaboracion.Date = new DateTimeOffset(fecha);
        else
            FechaElaboracion.Date = DateTimeOffset.Now;

        var indice = Array.FindIndex(CalculoOrdenCompraAdmin.EstadosDocumento, estado => estado.Equals(documento.Estado, StringComparison.OrdinalIgnoreCase));
        ComboEstado.SelectedIndex = indice >= 0 ? indice : 0;
    }

    private void Agregar(LineaOrdenCompraAdmin linea)
    {
        linea.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(LineaOrdenCompraAdmin.Cantidad) or nameof(LineaOrdenCompraAdmin.PrecioConIgv))
                ActualizarTotal();
        };
        _lineas.Add(linea);
    }

    private void ActualizarTotal() =>
        TotalGeneral.Text = $"Total con IGV: {_lineas.Sum(l => l.ImporteConIgv).ToString("C", CultureInfo.GetCultureInfo("es-PE"))}";

    private void Agregar_Click(object sender, RoutedEventArgs e)
    {
        Agregar(new LineaOrdenCompraAdmin());
        ActualizarTotal();
    }

    private void Quitar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button boton || boton.Tag is not Guid id)
            return;

        var linea = _lineas.FirstOrDefault(l => l.Id == id);
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

    private void Guardar_Click(object sender, RoutedEventArgs e) => Registrar(Capturar());

    private async void Pdf_Click(object sender, RoutedEventArgs e)
    {
        var documento = Capturar();
        if (!Registrar(documento))
            return;
        await ExportarAsync(documento, "Documento PDF", ".pdf", OrdenCompraAdminPdf.Guardar);
    }

    private async void Excel_Click(object sender, RoutedEventArgs e)
    {
        var documento = Capturar();
        if (!Registrar(documento))
            return;
        await ExportarAsync(documento, "Libro de Excel", ".xlsx", OrdenCompraAdminExcel.Guardar);
    }

    private bool Registrar(OrdenCompraAdminDocumento documento)
    {
        if (_lineas.All(l => string.IsNullOrWhiteSpace(l.Descripcion)))
        {
            Aviso.Severity = InfoBarSeverity.Warning;
            Aviso.Title = "Sin líneas";
            Aviso.Message = "Escriba al menos una descripción.";
            Aviso.IsOpen = true;
            return false;
        }

        OrdenesCompraAdminRepositorio.Guardar(new OrdenCompraAdminRegistrada
        {
            Id = _ordenId,
            FechaRegistro = _fechaRegistro,
            Documento = documento,
        });
        Aviso.Severity = InfoBarSeverity.Success;
        Aviso.Title = "Orden de compra guardada";
        Aviso.Message = documento.Estado;
        Aviso.IsOpen = true;
        return true;
    }

    private OrdenCompraAdminDocumento Capturar() => new()
    {
        MesAnio = CajaMes.Text.Trim(),
        FechaElaboracion = (FechaElaboracion.Date?.DateTime ?? DateTime.Today).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
        ElaboradoPor = CajaElaborado.Text.Trim(),
        RevisadoPor = CajaRevisado.Text.Trim(),
        PeriodoCobertura = CajaCobertura.Text.Trim(),
        Proveedor = CajaProveedor.Text.Trim(),
        Estado = ComboEstado.SelectedItem as string ?? "Borrador",
        Lineas = _lineas.Where(l => !string.IsNullOrWhiteSpace(l.Descripcion)).Select(l => l.Exportar()).ToList(),
    };

    private async Task ExportarAsync(OrdenCompraAdminDocumento documento, string etiqueta, string extension, Action<string, OrdenCompraAdminDocumento> guardar)
    {
        var ventana = MainWindow.Instancia;
        if (ventana is null)
            return;

        var selector = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"Orden-compra-{DateTime.Today:yyyyMMdd}",
        };
        selector.FileTypeChoices.Add(etiqueta, [extension]);
        InitializeWithWindow.Initialize(selector, WindowNative.GetWindowHandle(ventana));
        var archivo = await selector.PickSaveFileAsync();
        if (archivo is null)
            return;

        try
        {
            guardar(archivo.Path, documento);
            Process.Start(new ProcessStartInfo(archivo.Path) { UseShellExecute = true });
            Aviso.Severity = InfoBarSeverity.Success;
            Aviso.Title = "Archivo listo para imprimir";
            Aviso.Message = archivo.Name;
            Aviso.IsOpen = true;
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
