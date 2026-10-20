using IdermaAdministracion.Datos;
using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Dialogos;

public sealed partial class EditorPersonaDialog : ContentDialog
{
    private readonly Colaborador _persona;
    private readonly bool _nueva;

    public EditorPersonaDialog(Colaborador persona, bool nueva)
    {
        InitializeComponent();
        _persona = persona;
        _nueva = nueva;

        var esDoctor = persona.Rol == RolClinica.Doctor;
        Title = nueva
            ? esDoctor ? "Nuevo doctor" : "Nuevo trabajador"
            : esDoctor ? "Editar doctor" : "Editar trabajador";
        BloqueMedico.Visibility = esDoctor ? Visibility.Visible : Visibility.Collapsed;

        CargarOpciones(TipoContrato, ["Plazo indeterminado", "Plazo fijo", "Locación de servicios"], persona.Contrato.TipoContrato);
        CargarOpciones(Sede, ["Clínica", "Oficina administrativa 1904"], persona.Contrato.Sede);
        CargarOpciones(Jornada, ["Tiempo completo", "Medio tiempo", "Por turnos", "Por procedimiento"], persona.Contrato.Jornada);
        CargarOpciones(Modalidad, ["Planilla", "Recibo por honorarios"], persona.Contrato.ModalidadTexto);

        Nombres.Text = persona.Nombres;
        Apellidos.Text = persona.Apellidos;
        Documento.Text = persona.Documento;
        Telefono.Text = persona.Telefono;
        Correo.Text = persona.Correo;
        Colegiatura.Text = persona.Colegiatura;
        Especialidad.Text = persona.Especialidad;
        NumeroContrato.Text = persona.Contrato.NumeroContrato;
        Cargo.Text = persona.Contrato.Cargo;
        CargarOpciones(Area, RepositorioEjemplo.Areas, persona.Contrato.Area);
        HorarioSemana.Asegurar(persona.Contrato);
        PintarHorario();
        FechaIngreso.Date = ComoFecha(persona.Contrato.FechaIngreso);
        Duracion.ItemsSource = new[] { "Sin plazo fijo", "3 meses", "6 meses", "12 meses" };
        Duracion.SelectedIndex = persona.Contrato.PlazoMeses switch
        {
            3 => 1,
            6 => 2,
            12 => 3,
            _ => 0,
        };
        ActualizarAvisoPlazo();
        SeRetiro.IsChecked = persona.Contrato.FechaRetiro is not null;
        FechaRetiro.Date = ComoFecha(persona.Contrato.FechaRetiro);
        MotivoRetiro.Text = persona.Contrato.MotivoRetiro;
        Sueldo.Value = (double)persona.Contrato.SueldoMensual;
        CodigoPago.Text = persona.Contrato.CodigoPago;
        Banco.Text = persona.Contrato.Banco;
        Cuenta.Text = persona.Contrato.NumeroCuenta;
        AceptaImagen.IsOn = persona.Contrato.AceptaUsoImagen;
        Canales.Text = persona.Contrato.CanalesImagen == "—" ? "" : persona.Contrato.CanalesImagen;
        Restricciones.Text = persona.Contrato.RestriccionesImagen == "—" ? "" : persona.Contrato.RestriccionesImagen;
        FechaConsentimiento.Date = ComoFecha(persona.Contrato.FechaConsentimientoImagen);
        VencimientoImagen.Date = ComoFecha(persona.Contrato.VencimientoImagen);
        Observaciones.Text = persona.Contrato.Observaciones;
        ActualizarRetiro();
        ActualizarImagen();
    }

    private void PresetHorario_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button boton)
            return;

        var (entrada, salida) = (boton.Content as string) switch
        {
            "9 a 6" => (9, 18),
            "9 a 7" => (9, 19),
            "8 a 6" => (8, 18),
            "8 a 5" => (8, 17),
            _ => (-1, -1),
        };
        if (entrada < 0)
            return;

        var dias = _persona.Contrato.Dias;
        if (dias.All(d => !d.Labora))
        {
            for (var i = 0; i < dias.Count && i < 5; i++)
                dias[i].Labora = true;
        }

        foreach (var dia in dias.Where(d => d.Labora))
        {
            dia.Ingreso = new TimeSpan(entrada, 0, 0);
            dia.Salida = new TimeSpan(salida, 0, 0);
        }

        PintarHorario();
    }

    private void PintarHorario()
    {
        DiasHorario.Children.Clear();
        DiasHorario.Children.Add(FilaHorario(
            new TextBlock { Text = "Día", Opacity = 0.7, VerticalAlignment = VerticalAlignment.Center },
            EtiquetaHora("Ingreso"),
            EtiquetaHora("Salida"),
            fondo: false));

        foreach (var dia in _persona.Contrato.Dias)
        {
            var labora = new CheckBox
            {
                Content = dia.Nombre,
                IsChecked = dia.Labora,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 0,
            };
            var ingreso = CampoHora(dia.Ingreso, dia.Labora, hora => dia.Ingreso = hora);
            var salida = CampoHora(dia.Salida, dia.Labora, hora => dia.Salida = hora);
            labora.Checked += (_, _) =>
            {
                dia.Labora = true;
                ingreso.Activar(true);
                salida.Activar(true);
            };
            labora.Unchecked += (_, _) =>
            {
                dia.Labora = false;
                ingreso.Activar(false);
                salida.Activar(false);
            };

            DiasHorario.Children.Add(FilaHorario(labora, ingreso.Vista, salida.Vista, fondo: false));
        }
    }

    private static TextBlock EtiquetaHora(string texto) => new()
    {
        Text = texto,
        Opacity = 0.7,
        VerticalAlignment = VerticalAlignment.Center,
    };

    private sealed class RelojEditor
    {
        public required Border Vista { get; init; }
        public required ComboBox Horas { get; init; }
        public required ComboBox Minutos { get; init; }

        public void Activar(bool activo)
        {
            Horas.IsEnabled = activo;
            Minutos.IsEnabled = activo;
            Vista.Opacity = activo ? 1 : 0.4;
        }
    }

    private static RelojEditor CampoHora(TimeSpan hora, bool activo, Action<TimeSpan> alCambiar)
    {
        var horas = ListaHora(24, hora.Hours);
        var minutos = ListaHora(12, hora.Minutes / 5, paso: 5);
        if (hora.Minutes % 5 != 0)
        {
            minutos.Items.Add(hora.Minutes.ToString("00"));
            minutos.SelectedItem = hora.Minutes.ToString("00");
        }

        void Publicar()
        {
            if (horas.SelectedItem is not string textoHora || minutos.SelectedItem is not string textoMinuto)
                return;
            if (int.TryParse(textoHora, out var h) && int.TryParse(textoMinuto, out var m))
                alCambiar(new TimeSpan(h, m, 0));
        }

        horas.SelectionChanged += (_, _) => Publicar();
        minutos.SelectionChanged += (_, _) => Publicar();

        var caja = new Grid
        {
            ColumnSpacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };
        caja.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        caja.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        caja.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var separador = new TextBlock
        {
            Text = ":",
            FontSize = 16,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(separador, 1);
        Grid.SetColumn(minutos, 2);
        caja.Children.Add(horas);
        caja.Children.Add(separador);
        caja.Children.Add(minutos);

        var vista = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(8, 2, 8, 2),
            CornerRadius = new CornerRadius(6),
            BorderThickness = new Thickness(1),
            BorderBrush = Application.Current.Resources["CardStrokeColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush,
            Child = caja,
        };
        var editor = new RelojEditor { Vista = vista, Horas = horas, Minutos = minutos };
        editor.Activar(activo);
        return editor;
    }

    private static ComboBox ListaHora(int cantidad, int seleccionado, int paso = 1)
    {
        var lista = new ComboBox
        {
            Width = 76,
            MinWidth = 0,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
        };
        for (var i = 0; i < cantidad; i++)
            lista.Items.Add((i * paso).ToString("00"));
        lista.SelectedIndex = Math.Clamp(seleccionado, 0, cantidad - 1);
        return lista;
    }

    private static Border FilaHorario(FrameworkElement dia, FrameworkElement ingreso, FrameworkElement salida, bool fondo)
    {
        var fila = new Grid { ColumnSpacing = 28, VerticalAlignment = VerticalAlignment.Center };
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        fila.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
        Grid.SetColumn(ingreso, 1);
        Grid.SetColumn(salida, 2);
        fila.Children.Add(dia);
        fila.Children.Add(ingreso);
        fila.Children.Add(salida);
        return new Border
        {
            Padding = new Thickness(4, 2, 4, 2),
            Child = fila,
        };
    }

    private void Retiro_Changed(object sender, RoutedEventArgs e) => ActualizarRetiro();

    private void ActualizarRetiro()
    {
        if (BloqueRetiro is null)
            return;

        BloqueRetiro.Visibility = SeRetiro.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Imagen_Toggled(object sender, RoutedEventArgs e) => ActualizarImagen();

    private void ActualizarImagen()
    {
        if (BloqueImagen is null)
            return;

        BloqueImagen.Visibility = AceptaImagen.IsOn ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Guardar_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var error = Validar();
        if (error is not null)
        {
            args.Cancel = true;
            ErrorAviso.Message = error;
            ErrorAviso.IsOpen = true;
            return;
        }

        _persona.Nombres = Nombres.Text.Trim();
        _persona.Apellidos = Apellidos.Text.Trim();
        _persona.Documento = Documento.Text.Trim();
        _persona.Telefono = Telefono.Text.Trim();
        _persona.Correo = Correo.Text.Trim();
        _persona.Colegiatura = Colegiatura.Text.Trim();
        _persona.Especialidad = Especialidad.Text.Trim();

        var contrato = _persona.Contrato;
        contrato.NumeroContrato = NumeroContrato.Text.Trim();
        contrato.TipoContrato = Texto(TipoContrato);
        contrato.Cargo = Cargo.Text.Trim();
        contrato.Area = Texto(Area);
        contrato.Sede = Texto(Sede);
        contrato.Jornada = Texto(Jornada);
        contrato.Horario = HorarioSemana.Resumen(contrato.Dias);
        contrato.FechaIngreso = FechaIngreso.Date!.Value.Date;
        contrato.PlazoMeses = Duracion.SelectedIndex switch
        {
            1 => 3,
            2 => 6,
            3 => 12,
            _ => 0,
        };
        contrato.FechaFinContrato = contrato.PlazoMeses == 0
            ? null
            : contrato.FechaIngreso.AddMonths(contrato.PlazoMeses);
        contrato.FechaRetiro = SeRetiro.IsChecked == true ? FechaRetiro.Date?.Date : null;
        contrato.MotivoRetiro = SeRetiro.IsChecked == true ? MotivoRetiro.Text.Trim() : "";
        contrato.SueldoMensual = (decimal)Sueldo.Value;
        contrato.Modalidad = Texto(Modalidad) == "Planilla"
            ? ModalidadPago.Planilla
            : ModalidadPago.ReciboPorHonorarios;
        contrato.CodigoPago = CodigoPago.Text.Trim();
        contrato.Banco = Banco.Text.Trim();
        contrato.NumeroCuenta = Cuenta.Text.Trim();
        contrato.AceptaUsoImagen = AceptaImagen.IsOn;
        contrato.CanalesImagen = Canales.Text.Trim();
        contrato.RestriccionesImagen = Restricciones.Text.Trim();
        contrato.FechaConsentimientoImagen = AceptaImagen.IsOn ? FechaConsentimiento.Date?.Date : null;
        contrato.VencimientoImagen = AceptaImagen.IsOn ? VencimientoImagen.Date?.Date : null;
        contrato.Observaciones = Observaciones.Text.Trim();

        RepositorioEjemplo.GuardarColaborador(_persona, _nueva);
    }

    private string? Validar()
    {
        if (string.IsNullOrWhiteSpace(Nombres.Text) || string.IsNullOrWhiteSpace(Apellidos.Text))
            return "Escribe nombres y apellidos.";
        if (string.IsNullOrWhiteSpace(Cargo.Text) || string.IsNullOrWhiteSpace(Texto(Area)))
            return "El cargo y el área son obligatorios.";
        if (FechaIngreso.Date is null)
            return "Indica la fecha de ingreso.";
        if (_persona.Contrato.Dias.Any(d => d.Labora && d.Salida <= d.Ingreso))
            return "En los días que labora, la salida tiene que ser después del ingreso.";
        if (double.IsNaN(Sueldo.Value) || Sueldo.Value < 0)
            return "El sueldo debe ser un monto válido.";
        if (SeRetiro.IsChecked == true && FechaRetiro.Date is null)
            return "Si se retiró, indica la fecha de retiro.";
        if (SeRetiro.IsChecked == true && FechaRetiro.Date is DateTimeOffset retiro && retiro.Date < FechaIngreso.Date.Value.Date)
            return "La fecha de retiro no puede ser anterior al ingreso.";
        return null;
    }

    private static void CargarOpciones(ComboBox combo, IEnumerable<string> opciones, string actual)
    {
        combo.Items.Clear();
        foreach (var opcion in opciones)
            combo.Items.Add(opcion);

        if (!string.IsNullOrWhiteSpace(actual) && !combo.Items.Contains(actual))
            combo.Items.Insert(0, actual);

        combo.SelectedItem = string.IsNullOrWhiteSpace(actual) ? combo.Items.FirstOrDefault() : actual;
    }

    private void Duracion_SelectionChanged(object sender, SelectionChangedEventArgs e) => ActualizarAvisoPlazo();

    private void FechaIngreso_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) =>
        ActualizarAvisoPlazo();

    private void ActualizarAvisoPlazo()
    {
        if (AvisoPlazo is null || Duracion is null)
            return;

        var meses = Duracion.SelectedIndex switch
        {
            1 => 3,
            2 => 6,
            3 => 12,
            _ => 0,
        };
        if (meses == 0 || FechaIngreso.Date is null)
        {
            AvisoPlazo.Text = meses == 0
                ? "Sin fecha de término. El contrato sigue hasta el retiro."
                : "Indica la fecha de ingreso para calcular el vencimiento.";
            return;
        }

        var fin = FechaIngreso.Date.Value.Date.AddMonths(meses);
        var dias = (fin - DateTime.Today).Days;
        var cuando = Formato.FechaLarga(fin);
        AvisoPlazo.Text = dias < 0
            ? $"Vence el {cuando}. Ya está vencido."
            : dias <= 30
                ? $"Vence el {cuando}. Quedará una alerta porque falta un mes o menos."
                : $"Vence el {cuando}.";
    }

    private static string Texto(ComboBox combo) => combo.SelectedItem as string ?? "";

    private static DateTimeOffset? ComoFecha(DateTime? fecha) =>
        fecha is null ? null : new DateTimeOffset(DateTime.SpecifyKind(fecha.Value, DateTimeKind.Local));
}
