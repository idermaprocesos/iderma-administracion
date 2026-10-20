using System.Diagnostics;
using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using IdermaAdministracion.Servicios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace IdermaAdministracion.Pages;

public sealed partial class RegistroCompraAdminPage : Page
{
    public RegistroCompraAdminPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Cargar();
    }

    private void Cargar()
    {
        var items = OrdenesCompraAdminRepositorio.Listar().Select(o => new OrdenCompraAdminItem(o)).ToList();
        Lista.ItemsSource = items;
        Vacio.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Volver_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private async void Pdf_Click(object sender, RoutedEventArgs e) =>
        await ExportarAsync(sender, "Documento PDF", ".pdf", OrdenCompraAdminPdf.Guardar);

    private async void Excel_Click(object sender, RoutedEventArgs e) =>
        await ExportarAsync(sender, "Libro de Excel", ".xlsx", OrdenCompraAdminExcel.Guardar);

    private void Quitar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button boton && boton.Tag is string texto && Guid.TryParse(texto, out var id))
        {
            OrdenesCompraAdminRepositorio.Eliminar(id);
            Cargar();
        }
    }

    private async Task ExportarAsync(object sender, string etiqueta, string extension, Action<string, OrdenCompraAdminDocumento> guardar)
    {
        if (sender is not Button boton || boton.Tag is not string texto || !Guid.TryParse(texto, out var id))
            return;

        var orden = OrdenesCompraAdminRepositorio.Obtener(id);
        var ventana = MainWindow.Instancia;
        if (orden is null || ventana is null)
            return;

        var selector = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = $"Orden-compra-{orden.Documento.FechaElaboracion.Replace('/', '-')}",
        };
        selector.FileTypeChoices.Add(etiqueta, [extension]);
        InitializeWithWindow.Initialize(selector, WindowNative.GetWindowHandle(ventana));
        var archivo = await selector.PickSaveFileAsync();
        if (archivo is null)
            return;

        try
        {
            guardar(archivo.Path, orden.Documento);
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
