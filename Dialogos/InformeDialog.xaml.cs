using System.Diagnostics;
using IdermaAdministracion.Servicios;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace IdermaAdministracion.Dialogos;

public sealed partial class InformeDialog : ContentDialog
{
    private readonly InformeLaboral _informe;

    public InformeDialog(InformeLaboral informe)
    {
        InitializeComponent();
        _informe = informe;
        Title = informe.Titulo;
        foreach (var seccion in informe.Secciones)
        {
            Cuerpo.Children.Add(new TextBlock
            {
                Text = seccion.Titulo,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 16,
            });
            foreach (var parrafo in seccion.Parrafos)
            {
                Cuerpo.Children.Add(new TextBlock
                {
                    Text = parrafo,
                    TextWrapping = TextWrapping.Wrap,
                    TextAlignment = TextAlignment.Justify,
                    Opacity = 0.92,
                });
            }
        }
    }

    private async void Word_Click(object sender, RoutedEventArgs e) =>
        await ExportarAsync("Documento Word", ".docx", InformeWord.Guardar);

    private async void Pdf_Click(object sender, RoutedEventArgs e) =>
        await ExportarAsync("Documento PDF", ".pdf", InformePdf.Guardar);

    private async Task ExportarAsync(string etiqueta, string extension, Action<string, InformeLaboral> guardar)
    {
        var ventana = MainWindow.Instancia;
        if (ventana is null)
            return;

        var selector = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = _informe.NombreArchivo,
        };
        selector.FileTypeChoices.Add(etiqueta, new List<string> { extension });
        InitializeWithWindow.Initialize(selector, WindowNative.GetWindowHandle(ventana));

        var archivo = await selector.PickSaveFileAsync();
        if (archivo is null)
            return;

        try
        {
            guardar(archivo.Path, _informe);
            Aviso.Severity = InfoBarSeverity.Success;
            Aviso.Title = "Informe guardado";
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
