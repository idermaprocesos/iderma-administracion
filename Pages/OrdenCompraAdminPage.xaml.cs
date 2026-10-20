using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class OrdenCompraAdminPage : Page
{
    public OrdenCompraAdminPage() => InitializeComponent();

    private void Nueva_Click(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(OrdenCompraAdminEditorPage), new OrdenCompraAdminEdicion
        {
            Lineas = [new LineaOrdenCompraAdmin()],
        });

    private void Borradores_Click(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(BorradoresCompraAdminPage));

    private void Anteriores_Click(object sender, RoutedEventArgs e) =>
        Frame.Navigate(typeof(RegistroCompraAdminPage));
}
