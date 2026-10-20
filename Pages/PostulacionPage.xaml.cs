using IdermaAdministracion.Controls;
using IdermaAdministracion.Datos;
using IdermaAdministracion.Dialogos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Pages;

public sealed partial class PostulacionPage : Page
{
    private Postulacion? _postulacion;
    private bool _ajustandoProceso;

    public PostulacionPage()
    {
        InitializeComponent();
        FechaPaso.Date = DateTimeOffset.Now;
    }

    protected override void OnNavigatedTo(Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        Mostrar(RepositorioEjemplo.BuscarPostulacion(e.Parameter as string));
    }

    private void Mostrar(Postulacion? postulacion)
    {
        _postulacion = postulacion;
        if (postulacion is null)
        {
            FichaRaiz.Visibility = Visibility.Collapsed;
            Vacio.Visibility = Visibility.Visible;
            return;
        }

        FichaRaiz.Visibility = Visibility.Visible;
        Vacio.Visibility = Visibility.Collapsed;
        Nombre.Text = postulacion.NombreCompleto;
        Puesto.Text = $"{postulacion.RolTexto} · {postulacion.PuestoArea} · {postulacion.Telefono}";
        Nota.Text = postulacion.Nota;
        LineaPasos.Pintar(PasosHost, postulacion);
        PintarProceso(postulacion);
        PintarEstado(postulacion);
        PintarFormulario(postulacion);
        PintarHistorial(postulacion);
        BotonPersonal.Visibility = postulacion.Contratado ? Visibility.Visible : Visibility.Collapsed;
        BotonPersonal.IsEnabled = string.IsNullOrEmpty(postulacion.PersonalId);
        BotonPersonal.Content = string.IsNullOrEmpty(postulacion.PersonalId)
            ? "Pasar a personal"
            : "Ya está en personal";
    }

    private void PintarProceso(Postulacion postulacion)
    {
        _ajustandoProceso = true;
        Proceso.SelectedIndex = postulacion.IndiceProceso;
        _ajustandoProceso = false;
    }

    private void PintarEstado(Postulacion postulacion)
    {
        AvisoEstado.IsOpen = true;
        if (postulacion.Contratado)
        {
            AvisoEstado.Severity = InfoBarSeverity.Success;
            AvisoEstado.Title = "Contratado";
            AvisoEstado.Message = string.IsNullOrEmpty(postulacion.PersonalId)
                ? "Completó el primer día. Puedes pasarlo a trabajadores o doctores."
                : "Completó el primer día y ya figura en el personal.";
            return;
        }

        if (postulacion.TieneCierreNegativo)
        {
            var cierre = postulacion.Pasos.Last(p => !p.Avanza);
            AvisoEstado.Severity = InfoBarSeverity.Warning;
            AvisoEstado.Title = "No continuó";
            AvisoEstado.Message = $"{PasosContratacion.Detalle(cierre.Etapa)}: {cierre.Texto}. Cambia el proceso o corrige el resultado para reabrirla.";
            return;
        }

        AvisoEstado.Severity = InfoBarSeverity.Informational;
        AvisoEstado.Title = postulacion.Pasos.Count == 0 ? "Por contactar" : "En proceso";
        AvisoEstado.Message = postulacion.EtapaActual is EtapaContratacion etapa
            ? $"Paso en curso: {PasosContratacion.Detalle(etapa)}."
            : "Revisa el registro de esta postulación.";
    }

    private void PintarFormulario(Postulacion postulacion)
    {
        AvisoFormulario.IsOpen = false;
        if (postulacion.IndiceProceso >= PasosContratacion.Orden.Count)
        {
            PanelPaso.Visibility = Visibility.Collapsed;
            return;
        }

        var etapa = PasosContratacion.Orden[postulacion.IndiceProceso];
        PanelPaso.Visibility = Visibility.Visible;
        TituloPaso.Text = PasosContratacion.Titulo(etapa);
        DetallePaso.Text = "Corrige o registra el resultado de este paso.";
        var opciones = PasosContratacion.Opciones(etapa).Select(o => o.Texto).ToList();
        OpcionesPaso.ItemsSource = opciones;
        var actual = postulacion.De(etapa);
        OpcionesPaso.SelectedItem = actual is null ? null : actual.Texto;
        FechaPaso.Date = actual is null ? DateTimeOffset.Now : new DateTimeOffset(actual.Cuando);
    }

    private void PintarHistorial(Postulacion postulacion)
    {
        Historial.Children.Clear();
        if (postulacion.Pasos.Count == 0)
        {
            Historial.Children.Add(new TextBlock
            {
                Opacity = 0.75,
                Text = "Todavía no hay resultados registrados.",
            });
            return;
        }

        foreach (var paso in postulacion.Pasos.OrderBy(p => p.Etapa))
        {
            var etapa = paso.Etapa;
            var corregir = new Button { Content = "Corregir este paso", HorizontalAlignment = HorizontalAlignment.Left };
            corregir.Click += (_, _) => IrAlPaso((int)etapa);

            var cuerpo = new StackPanel { Spacing = 8 };
            cuerpo.Children.Add(new TextBlock
            {
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                Text = $"{PasosContratacion.Titulo(paso.Etapa)} · {paso.Texto}",
                TextWrapping = TextWrapping.Wrap,
            });
            cuerpo.Children.Add(new TextBlock
            {
                Opacity = 0.75,
                Text = $"{PasosContratacion.Detalle(paso.Etapa)} · {Formato.FechaLarga(paso.Cuando)}",
            });
            cuerpo.Children.Add(corregir);

            Historial.Children.Add(new Border
            {
                Style = (Style)Application.Current.Resources["TarjetaEstilo"],
                Child = cuerpo,
            });
        }
    }

    private void IrAlPaso(int indice)
    {
        if (Proceso.SelectedIndex == indice)
        {
            PintarFormulario(_postulacion!);
            return;
        }

        Proceso.SelectedIndex = indice;
    }

    private void Proceso_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ajustandoProceso || _postulacion is null || Proceso.SelectedIndex < 0)
            return;

        if (Proceso.SelectedIndex == _postulacion.IndiceProceso)
            return;

        RepositorioEjemplo.UbicarProceso(_postulacion, Proceso.SelectedIndex);
        Mostrar(_postulacion);
    }

    private void Registrar_Click(object sender, RoutedEventArgs e)
    {
        if (_postulacion is null || _postulacion.IndiceProceso >= PasosContratacion.Orden.Count)
            return;

        var etapa = PasosContratacion.Orden[_postulacion.IndiceProceso];
        var elegido = OpcionesPaso.SelectedItem as string;
        var opcion = PasosContratacion.Opciones(etapa).FirstOrDefault(o => o.Texto == elegido);
        if (opcion is null)
        {
            AvisoFormulario.Title = "Falta el resultado";
            AvisoFormulario.Message = "Elige qué pasó en este paso antes de guardarlo.";
            AvisoFormulario.IsOpen = true;
            return;
        }

        if (FechaPaso.Date is not DateTimeOffset fecha)
        {
            AvisoFormulario.Title = "Falta la fecha";
            AvisoFormulario.Message = "Indica la fecha de la llamada, la reunión, la evaluación o el primer día.";
            AvisoFormulario.IsOpen = true;
            return;
        }

        RepositorioEjemplo.GuardarResultado(_postulacion, new RegistroPaso
        {
            Etapa = etapa,
            Codigo = opcion.Codigo,
            Texto = opcion.Texto,
            Avanza = opcion.Avanza,
            Cuando = fecha.DateTime.Date,
        });
        Mostrar(_postulacion);
    }

    private void Nota_LostFocus(object sender, RoutedEventArgs e)
    {
        if (_postulacion is not null)
            _postulacion.Nota = Nota.Text.Trim();
    }

    private async void EditarDatos_Click(object sender, RoutedEventArgs e)
    {
        if (_postulacion is null)
            return;

        Nota_LostFocus(Nota, e);
        var dialogo = new EditorPostulacionDialog(_postulacion) { XamlRoot = XamlRoot };
        if (await dialogo.ShowAsync() == ContentDialogResult.Primary)
            Mostrar(_postulacion);
    }

    private async void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        if (_postulacion is null)
            return;

        var confirmar = new ContentDialog
        {
            XamlRoot = XamlRoot,
            Title = "Eliminar postulación",
            Content = $"Se quita a {_postulacion.NombreCompleto} del listado de postulaciones.",
            PrimaryButtonText = "Eliminar",
            CloseButtonText = "Cancelar",
            DefaultButton = ContentDialogButton.Close,
        };
        if (await confirmar.ShowAsync() != ContentDialogResult.Primary)
            return;

        RepositorioEjemplo.EliminarPostulacion(_postulacion.Id);
        Volver_Click(sender, e);
    }

    private void PasarPersonal_Click(object sender, RoutedEventArgs e)
    {
        if (_postulacion is null || !_postulacion.Contratado || !string.IsNullOrEmpty(_postulacion.PersonalId))
            return;

        var persona = RepositorioEjemplo.AgregarEnArea(
            _postulacion.Nombres,
            _postulacion.Apellidos,
            _postulacion.Puesto,
            _postulacion.Area,
            _postulacion.Rol);
        _postulacion.PersonalId = persona.Id;
        Mostrar(_postulacion);
    }

    private void Volver_Click(object sender, RoutedEventArgs e)
    {
        Nota_LostFocus(Nota, e);
        if (Frame.CanGoBack)
            Frame.GoBack();
        else
            NavegacionApp.Ir("postulaciones");
    }
}
