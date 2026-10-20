using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Dialogos;

public sealed partial class EditorPuestoDialog : ContentDialog
{
    private readonly PuestoTrabajo? _existente;

    public PuestoTrabajo? Creado { get; private set; }

    public EditorPuestoDialog(PuestoTrabajo? puesto = null)
    {
        InitializeComponent();
        _existente = puesto;
        Area.ItemsSource = RepositorioEjemplo.Areas;
        Area.SelectedIndex = 0;
        if (puesto is null)
            return;

        Title = "Detalle del puesto";
        PrimaryButtonText = "Guardar";
        Nombre.Text = puesto.Nombre;
        Area.SelectedItem = RepositorioEjemplo.Areas.Contains(puesto.Area)
            ? puesto.Area
            : RepositorioEjemplo.Areas[0];
        Rol.SelectedIndex = puesto.Rol == RolClinica.Doctor ? 1 : 0;
        Funciones.Text = puesto.Funciones;
        Estudios.Text = puesto.Estudios;
        Responsabilidades.Text = puesto.Responsabilidades;
        if (puesto.SueldoDesde is decimal desde)
            SueldoDesde.Value = (double)desde;
        if (puesto.SueldoHasta is decimal hasta)
            SueldoHasta.Value = (double)hasta;
    }

    private void Guardar_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var nombre = Nombre.Text.Trim();
        if (nombre.Length == 0)
        {
            Aviso.Title = "Falta el puesto";
            Aviso.Message = "Escribe el nombre del puesto de trabajo.";
            Aviso.IsOpen = true;
            args.Cancel = true;
            return;
        }

        if (RepositorioEjemplo.BuscarPuesto(nombre) is { } existente && existente != _existente)
        {
            Aviso.Title = "Ese puesto ya existe";
            Aviso.Message = existente.Archivado
                ? "Ese puesto está archivado. Elige otro nombre para abrir una postulación nueva."
                : "Elige otro nombre o agrega postulantes al puesto que ya está en la lista.";
            Aviso.IsOpen = true;
            args.Cancel = true;
            return;
        }

        var desde = LeerSueldo(SueldoDesde);
        var hasta = LeerSueldo(SueldoHasta);
        if (desde is not null && hasta is not null && hasta < desde)
        {
            Aviso.Title = "Franja de sueldo";
            Aviso.Message = "El sueldo hasta tiene que ser igual o mayor que el sueldo desde.";
            Aviso.IsOpen = true;
            args.Cancel = true;
            return;
        }

        var area = Area.SelectedItem as string ?? RepositorioEjemplo.Areas[0];
        var rol = Rol.SelectedIndex == 1 ? RolClinica.Doctor : RolClinica.Trabajador;
        var nombreAnterior = _existente?.Nombre;
        var puesto = _existente ?? RepositorioEjemplo.RegistrarPuesto(nombre, area, rol);
        puesto.Nombre = nombre;
        puesto.Area = area;
        puesto.Rol = rol;
        puesto.Funciones = Funciones.Text.Trim();
        puesto.Estudios = Estudios.Text.Trim();
        puesto.Responsabilidades = Responsabilidades.Text.Trim();
        puesto.SueldoDesde = desde;
        puesto.SueldoHasta = hasta;
        if (nombreAnterior is not null)
            RepositorioEjemplo.RenombrarPuestoEnPostulaciones(nombreAnterior, puesto);
        Creado = puesto;
    }

    private static decimal? LeerSueldo(NumberBox caja) =>
        double.IsNaN(caja.Value) || caja.Value <= 0 ? null : (decimal)caja.Value;
}
