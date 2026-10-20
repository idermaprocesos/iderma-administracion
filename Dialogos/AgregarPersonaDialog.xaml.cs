using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Dialogos;

public sealed partial class AgregarPersonaDialog : ContentDialog
{
    public string NombreIngresado { get; private set; } = "";
    public string ApellidoIngresado { get; private set; } = "";
    public string CargoIngresado { get; private set; } = "";
    public RolClinica Rol { get; private set; } = RolClinica.Trabajador;

    public AgregarPersonaDialog(string area)
    {
        InitializeComponent();
        Title = $"Agregar persona · {area}";
        AreaTexto.Text = $"Quedará en {area} y también en trabajadores o doctores, con esa área del organigrama.";
        if (OrganigramaDatos.EsMedica(area))
            RolCombo.SelectedIndex = 1;
    }

    private void Guardar_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(Nombres.Text) || string.IsNullOrWhiteSpace(Apellidos.Text))
        {
            Aviso.Title = "Falta el nombre";
            Aviso.Message = "Escribe nombres y apellidos.";
            Aviso.IsOpen = true;
            args.Cancel = true;
            return;
        }

        NombreIngresado = Nombres.Text.Trim();
        ApellidoIngresado = Apellidos.Text.Trim();
        CargoIngresado = Cargo.Text.Trim();
        Rol = RolCombo.SelectedIndex == 1 ? RolClinica.Doctor : RolClinica.Trabajador;
    }
}
