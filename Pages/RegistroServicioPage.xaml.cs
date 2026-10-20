using System.Diagnostics;
using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using IdermaAdministracion.Servicios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace IdermaAdministracion.Pages;

public sealed partial class RegistroServicioPage : Page
{
    public RegistroServicioPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Cargar();
    }

    private void Cargar()
    {
        var items = OrdenesServicioRepositorio.Listar().Select(o => new OrdenServicioItem(o)).ToList();
        Lista.ItemsSource = items;
        Vacio.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Volver_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private async void Pdf_Click(object sender, RoutedEventArgs e) =>
        await ExportarAsync(sender, "Documento PDF", ".pdf", OrdenServicioPdf.Guardar);

    private async void Excel_Click(object sender, RoutedEventArgs e) =>
        await ExportarAsync(sender, "Libro de Excel", ".xlsx", OrdenServicioExcel.Guardar);

    private void Quitar_Click(object sender, RoutedEventArgs e)
    {
        if (!TryId(sender, out var id))
            return;

        OrdenesServicioRepositorio.Eliminar(id);
        Cargar();
    }

    private async Task ExportarAsync(object sender, string etiqueta, string extension, Action<string, OrdenServicioDocumento> guardar)
    {
        if (!TryId(sender, out var id))
            return;

        var orden = OrdenesServicioRepositorio.Obtener(id);
        var ventana = MainWindow.Instancia;
        if (orden is null || ventana is null)
            return;

        var selector = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"Orden-servicio-{orden.Documento.FechaElaboracion.Replace('/', '-')}",
        };
        selector.FileTypeChoices.Add(etiqueta, new List<string> { extension });
        InitializeWithWindow.Initialize(selector, WindowNative.GetWindowHandle(ventana));
        var archivo = await selector.PickSaveFileAsync();
        if (archivo is null)
            return;

        try
        {
            guardar(archivo.Path, orden.Documento);
            Process.Start(new ProcessStartInfo(archivo.Path) { UseShellExecute = true });
            Aviso.Severity = InfoBarSeverity.Success;
            Aviso.Title = "Archivo listo";
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

    private static bool TryId(object sender, out Guid id)
    {
        id = Guid.Empty;
        return sender is Button boton && boton.Tag is string texto && Guid.TryParse(texto, out id);
    }
}
