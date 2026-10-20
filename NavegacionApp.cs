using IdermaAdministracion.Pages;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion;

public static class NavegacionApp
{
    public static Frame? Marco { get; set; }
    public static Action<string>? SeleccionarSeccion { get; set; }

    public static void Ir(string seccion) => SeleccionarSeccion?.Invoke(seccion);

    public static void AbrirFicha(string personaId) => Marco?.Navigate(typeof(FichaPage), personaId);

    public static void AbrirPostulacion(string postulacionId) => Marco?.Navigate(typeof(PostulacionPage), postulacionId);
}
