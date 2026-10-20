using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class BorradoresCompraAdminPage : Page
{
    public BorradoresCompraAdminPage()
    {
        InitializeComponent();
        Loaded += (_, _) => Cargar();
    }

    private void Cargar()
    {
        var items = OrdenesCompraAdminRepositorio.Listar()
            .Where(o => string.Equals(o.Documento.Estado, "Borrador", StringComparison.OrdinalIgnoreCase))
            .Select(o => new OrdenCompraAdminItem(o))
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

        var orden = OrdenesCompraAdminRepositorio.Obtener(id);
        if (orden is null)
            return;

        Frame.Navigate(typeof(OrdenCompraAdminEditorPage), new OrdenCompraAdminEdicion
        {
            Id = orden.Id,
            FechaRegistro = orden.FechaRegistro,
            Documento = orden.Documento,
            Lineas = orden.Documento.Lineas.Select(OrdenesCompraAdminRepositorio.DesdeGuardada).ToList(),
        });
    }
}

public sealed class OrdenCompraAdminItem
{
    public OrdenCompraAdminItem(OrdenCompraAdminRegistrada orden)
    {
        IdTexto = orden.Id.ToString();
        var doc = orden.Documento;
        Titulo = string.IsNullOrWhiteSpace(doc.Proveedor) ? $"{doc.MesAnio} · {doc.Estado}" : $"{doc.Proveedor} · {doc.Estado}";
        Resumen = $"{doc.Lineas.Count} línea(s) · {doc.FechaElaboracion} · {doc.Lineas.Sum(l => l.TotalConIgv):C}";
    }

    public string IdTexto { get; }
    public string Titulo { get; }
    public string Resumen { get; }
}
