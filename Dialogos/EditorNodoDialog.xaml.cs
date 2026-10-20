using IdermaAdministracion.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace IdermaAdministracion.Dialogos;

public sealed partial class EditorNodoDialog : ContentDialog
{
    private readonly List<FilaPersona> _filas = [];

    public IReadOnlyList<PersonaOrganigrama> Personas { get; private set; } = [];

    public EditorNodoDialog(NodoOrganigrama nodo)
    {
        InitializeComponent();
        Title = $"Editar · {nodo.Titulo}";
        if (nodo.Personas.Count == 0)
            AgregarFila(new PersonaOrganigrama());
        else
        {
            foreach (var persona in nodo.Personas)
                AgregarFila(persona);
        }
    }

    private void Agregar_Click(object sender, RoutedEventArgs e) =>
        AgregarFila(new PersonaOrganigrama());

    private void AgregarFila(PersonaOrganigrama persona)
    {
        var nombre = new TextBox
        {
            Header = "Nombre",
            Text = persona.Nombre,
            PlaceholderText = "Nombre y apellidos",
        };
        var cargo = new TextBox
        {
            Header = "Cargo",
            Text = persona.Cargo,
            PlaceholderText = "Cargo en el área",
        };
        var quitar = new Button
        {
            Content = "Quitar",
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 0, 2),
        };

        var grilla = new Grid { ColumnSpacing = 8 };
        grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1.4, GridUnitType.Star) });
        grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grilla.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(cargo, 1);
        Grid.SetColumn(quitar, 2);
        grilla.Children.Add(nombre);
        grilla.Children.Add(cargo);
        grilla.Children.Add(quitar);

        var fila = new FilaPersona(persona.Id, nombre, cargo, grilla);
        quitar.Click += (_, _) =>
        {
            _filas.Remove(fila);
            Filas.Children.Remove(grilla);
        };
        _filas.Add(fila);
        Filas.Children.Add(grilla);
    }

    private void Guardar_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (_filas.Any(f => string.IsNullOrWhiteSpace(f.Nombre.Text) && !string.IsNullOrWhiteSpace(f.Cargo.Text)))
        {
            Aviso.Title = "Falta el nombre";
            Aviso.Message = "Cada cargo necesita el nombre de la persona, o quita esa fila.";
            Aviso.IsOpen = true;
            args.Cancel = true;
            return;
        }

        Personas = _filas
            .Where(f => !string.IsNullOrWhiteSpace(f.Nombre.Text))
            .Select(f => new PersonaOrganigrama
            {
                Id = string.IsNullOrWhiteSpace(f.Id) ? Guid.NewGuid().ToString("N")[..8] : f.Id,
                Nombre = f.Nombre.Text.Trim(),
                Cargo = f.Cargo.Text.Trim(),
            })
            .ToList();
    }

    private sealed record FilaPersona(string Id, TextBox Nombre, TextBox Cargo, Grid Raiz);
}
