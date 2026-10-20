using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Dialogos;

public sealed partial class EditorPostulacionDialog : ContentDialog
{
    private readonly Postulacion _postulacion;
    private readonly bool _nueva;
    private bool _cargando = true;

    public EditorPostulacionDialog(Postulacion postulacion, bool fijarPuesto = false)
    {
        InitializeComponent();
        _postulacion = postulacion;
        _nueva = string.IsNullOrWhiteSpace(postulacion.Nombres);
        Title = _nueva ? "Nueva postulación" : "Editar postulación";
        PrimaryButtonText = _nueva ? "Crear" : "Guardar";
        Area.ItemsSource = RepositorioEjemplo.Areas;
        Area.SelectedItem = RepositorioEjemplo.Areas.Contains(postulacion.Area)
            ? postulacion.Area
            : RepositorioEjemplo.Areas[0];
        Nombres.Text = postulacion.Nombres;
        Apellidos.Text = postulacion.Apellidos;
        Documento.Text = postulacion.Documento;
        Telefono.Text = postulacion.Telefono;
        Correo.Text = postulacion.Correo;
        Puesto.ItemsSource = RepositorioEjemplo.PuestosAbiertos.Select(p => p.Nombre).ToList();
        Puesto.SelectedItem = RepositorioEjemplo.PuestosAbiertos
            .Select(p => p.Nombre)
            .FirstOrDefault(n => string.Equals(n, postulacion.Puesto, StringComparison.OrdinalIgnoreCase));
        Puesto.IsEnabled = !fijarPuesto;
        Rol.SelectedIndex = postulacion.Rol == RolClinica.Doctor ? 1 : 0;
        Puesto.SelectionChanged += Puesto_SelectionChanged;
        _cargando = false;
    }

    private void Puesto_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_cargando || Puesto.SelectedItem is not string nombre)
            return;

        if (RepositorioEjemplo.BuscarPuesto(nombre) is not PuestoTrabajo puesto)
            return;

        Area.SelectedItem = puesto.Area;
        Rol.SelectedIndex = puesto.Rol == RolClinica.Doctor ? 1 : 0;
    }

    private void Guardar_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var nombres = Nombres.Text.Trim();
        var apellidos = Apellidos.Text.Trim();
        var puesto = Puesto.SelectedItem as string ?? "";
        if (nombres.Length == 0 || apellidos.Length == 0 || puesto.Length == 0)
        {
            Aviso.Title = "Faltan datos";
            Aviso.Message = RepositorioEjemplo.PuestosAbiertos.Count == 0
                ? "Crea primero un puesto de trabajo y después registra al postulante."
                : "Escribe nombres, apellidos y elige el puesto.";
            Aviso.IsOpen = true;
            args.Cancel = true;
            return;
        }

        _postulacion.Nombres = nombres;
        _postulacion.Apellidos = apellidos;
        _postulacion.Documento = Documento.Text.Trim();
        _postulacion.Telefono = Telefono.Text.Trim();
        _postulacion.Correo = Correo.Text.Trim();
        _postulacion.Puesto = puesto;
        _postulacion.Area = Area.SelectedItem as string ?? RepositorioEjemplo.Areas[0];
        _postulacion.Rol = Rol.SelectedIndex == 1 ? RolClinica.Doctor : RolClinica.Trabajador;
        RepositorioEjemplo.GuardarPostulacion(_postulacion, _nueva);
    }
}
