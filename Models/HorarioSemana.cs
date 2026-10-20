using System.Globalization;
using System.Text.RegularExpressions;

namespace IdermaAdministracion.Models;

public sealed class HorarioDia
{
    public string Nombre { get; set; } = "";
    public bool Labora { get; set; }
    public TimeSpan Ingreso { get; set; } = new(9, 0, 0);
    public TimeSpan Salida { get; set; } = new(18, 0, 0);
}

public static class HorarioSemana
{
    private static readonly string[] Nombres = ["Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado", "Domingo"];
    private static readonly string[] Claves = ["lun", "mar", "mie", "jue", "vie", "sab", "dom"];

    public static void Asegurar(ContratoLaboral contrato)
    {
        if (contrato.Dias.Count == Nombres.Length)
            return;

        var (ingreso, salida, labora) = Interpretar(contrato.Horario);
        contrato.Dias.Clear();
        for (var i = 0; i < Nombres.Length; i++)
        {
            contrato.Dias.Add(new HorarioDia
            {
                Nombre = Nombres[i],
                Labora = labora[i],
                Ingreso = ingreso,
                Salida = salida,
            });
        }

        contrato.Horario = Resumen(contrato.Dias);
    }

    public static string Resumen(IReadOnlyList<HorarioDia> dias)
    {
        if (dias.Count == 0)
            return "";

        var partes = new List<string>();
        var inicio = 0;
        while (inicio < dias.Count)
        {
            var fin = inicio;
            while (fin + 1 < dias.Count && Mismo(dias[fin], dias[fin + 1]))
                fin++;

            partes.Add(Frase(dias, inicio, fin));
            inicio = fin + 1;
        }

        var texto = string.Join(". ", partes) + ".";
        return char.ToUpper(texto[0], CultureInfo.GetCultureInfo("es-PE")) + texto[1..];
    }

    private static string Frase(IReadOnlyList<HorarioDia> dias, int inicio, int fin)
    {
        var nombres = DescribirRango(dias, inicio, fin);
        var dia = dias[inicio];
        if (!dia.Labora)
            return $"{nombres} no labora";

        return $"{nombres} ingresa a las {Hora(dia.Ingreso)} y sale a las {Hora(dia.Salida)}";
    }

    private static string DescribirRango(IReadOnlyList<HorarioDia> dias, int inicio, int fin)
    {
        if (inicio == fin)
            return "El " + dias[inicio].Nombre.ToLowerInvariant();

        if (fin == inicio + 1)
            return $"El {dias[inicio].Nombre.ToLowerInvariant()} y el {dias[fin].Nombre.ToLowerInvariant()}";

        return $"De {dias[inicio].Nombre.ToLowerInvariant()} a {dias[fin].Nombre.ToLowerInvariant()}";
    }

    private static bool Mismo(HorarioDia a, HorarioDia b) =>
        a.Labora == b.Labora && a.Ingreso == b.Ingreso && a.Salida == b.Salida;

    public static string Hora(TimeSpan hora) =>
        $"{hora.Hours:00}:{hora.Minutes:00}";

    private static (TimeSpan Ingreso, TimeSpan Salida, bool[] Labora) Interpretar(string horario)
    {
        var plano = SinTildes(horario).ToLowerInvariant();
        var ingreso = new TimeSpan(9, 0, 0);
        var salida = new TimeSpan(18, 0, 0);
        var horas = Regex.Match(plano, @"(\d{1,2})[:h](\d{2})\s*a\s*(\d{1,2})[:h](\d{2})");
        if (horas.Success)
        {
            ingreso = HoraSegura(horas.Groups[1].Value, horas.Groups[2].Value, ingreso);
            salida = HoraSegura(horas.Groups[3].Value, horas.Groups[4].Value, salida);
        }
        else if (plano.Contains("8 horas"))
        {
            ingreso = new TimeSpan(8, 0, 0);
            salida = new TimeSpan(16, 0, 0);
        }

        var labora = new bool[7];
        if (plano.Contains("lun a sab") || plano.Contains("lunes a sabado"))
        {
            for (var i = 0; i <= 5; i++)
                labora[i] = true;
        }
        else if (plano.Contains("lun a vie") || plano.Contains("lunes a viernes"))
        {
            for (var i = 0; i <= 4; i++)
                labora[i] = true;
        }
        else
        {
            var alguno = false;
            for (var i = 0; i < Claves.Length; i++)
            {
                if (Regex.IsMatch(plano, $@"\b{Claves[i]}"))
                {
                    labora[i] = true;
                    alguno = true;
                }
            }

            if (!alguno)
            {
                for (var i = 0; i <= 4; i++)
                    labora[i] = true;
            }
        }

        return (ingreso, salida, labora);
    }

    private static TimeSpan HoraSegura(string hora, string minuto, TimeSpan siFalla)
    {
        if (!int.TryParse(hora, out var h) || !int.TryParse(minuto, out var m))
            return siFalla;
        if (h is < 0 or > 23 || m is < 0 or > 59)
            return siFalla;
        return new TimeSpan(h, m, 0);
    }

    private static string SinTildes(string texto)
    {
        var plano = texto.Normalize(System.Text.NormalizationForm.FormD);
        return new string(plano.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }
}
