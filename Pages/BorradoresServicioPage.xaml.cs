using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class BorradoresServicioPage : Page
{
    public BorradoresServicioPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Cargar();
    }

    private void Cargar()
    {
        var items = OrdenesServicioRepositorio.Listar()
            .Where(o => string.Equals(o.Documento.Estado, "Borrador", StringComparison.OrdinalIgnoreCase))
            .Select(o => new OrdenServicioItem(o))
            .ToList();
        Lista.ItemsSource = items;
        Vacio.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Volver_Click(object sender, RoutedEventArgs e)
    {
        if (Frame.CanGoBack)
            Frame.GoBack();
    }

    private void Editar_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button boton || boton.Tag is not string texto || !Guid.TryParse(texto, out var id))
            return;

        var orden = OrdenesServicioRepositorio.Obtener(id);
        if (orden is null)
            return;

        Frame.Navigate(typeof(OrdenServicioEditorPage), new OrdenServicioEdicion
        {
            Id = orden.Id,
            FechaRegistro = orden.FechaRegistro,
            AmpliacionAplicada = orden.AmpliacionAplicada,
            Documento = orden.Documento,
            Lineas = orden.Documento.Lineas.Select(OrdenesServicioRepositorio.DesdeGuardada).ToList(),
        });
    }
}

public sealed class OrdenServicioItem
{
    public OrdenServicioItem(OrdenServicioRegistrada orden)
    {
        IdTexto = orden.Id.ToString();
        var doc = orden.Documento;
        Titulo = $"{doc.MesAnio} · {doc.Estado}";
        Resumen = $"{doc.Lineas.Count} contrato(s) · {doc.ElaboradoPor} · {doc.FechaElaboracion} · {doc.Lineas.Sum(l => l.TotalConIgv):C}";
    }

    public string IdTexto { get; }
    public string Titulo { get; }
    public string Resumen { get; }
}
