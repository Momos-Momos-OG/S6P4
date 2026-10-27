using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace S6P4.Models
{
    // ---------- GET https://www.swapi.tech/api/people/?page=1&limit=10 ----------
    public class SwListaResponse
    {
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("total_records")] public int TotalRecords { get; set; }
        [JsonProperty("total_pages")] public int TotalPages { get; set; }
        [JsonProperty("next")] public string Next { get; set; }
        [JsonProperty("previous")] public string Previous { get; set; }
        [JsonProperty("results")] public List<SwRef> Results { get; set; }
    }

    public class SwRef
    {
        [JsonProperty("uid")] public string Uid { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
    }

    // ---------- GET https://www.swapi.tech/api/people/1  (result = objeto) ----------
    public class SwDetalleResponse
    {
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("result")] public PersonaItem Result { get; set; }
    }

    // ---------- GET https://www.swapi.tech/api/people/?name=luke  (result = arreglo) ----------
    public class SwBusquedaResponse
    {
        [JsonProperty("message")] public string Message { get; set; }
        [JsonProperty("result")] public List<PersonaItem> Result { get; set; }
    }

    public class PersonaItem
    {
        [JsonProperty("uid")] public string Uid { get; set; }
        [JsonProperty("description")] public string Description { get; set; }
        [JsonProperty("properties")] public PersonaProps Properties { get; set; }
    }

    public class PersonaProps
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("height")] public string Height { get; set; }
        [JsonProperty("mass")] public string Mass { get; set; }
        [JsonProperty("hair_color")] public string HairColor { get; set; }
        [JsonProperty("skin_color")] public string SkinColor { get; set; }
        [JsonProperty("eye_color")] public string EyeColor { get; set; }
        [JsonProperty("birth_year")] public string BirthYear { get; set; }
        [JsonProperty("gender")] public string Gender { get; set; }
        [JsonProperty("homeworld")] public string Homeworld { get; set; }   // URL del planeta
        [JsonProperty("url")] public string Url { get; set; }

        // ----- Propiedades de ayuda para la vista -----
        [JsonIgnore]
        public string Iniciales
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Name)) return "?";
                var partes = Name.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
                return string.Concat(partes.Take(2).Select(p => char.ToUpperInvariant(p[0])));
            }
        }

        [JsonIgnore]
        public string GeneroClase { get { return StarWarsHelper.GeneroSlug(Gender); } }
    }

    // ---------- GET https://www.swapi.tech/api/planets/1 ----------
    public class SwPlanetaResponse
    {
        [JsonProperty("result")] public PlanetaItem Result { get; set; }
    }

    public class PlanetaItem
    {
        [JsonProperty("uid")] public string Uid { get; set; }
        [JsonProperty("properties")] public PlanetaProps Properties { get; set; }
    }

    public class PlanetaProps
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("climate")] public string Climate { get; set; }
        [JsonProperty("terrain")] public string Terrain { get; set; }
        [JsonProperty("population")] public string Population { get; set; }
        [JsonProperty("diameter")] public string Diameter { get; set; }
        [JsonProperty("gravity")] public string Gravity { get; set; }
        [JsonProperty("rotation_period")] public string RotationPeriod { get; set; }
        [JsonProperty("orbital_period")] public string OrbitalPeriod { get; set; }
        [JsonProperty("surface_water")] public string SurfaceWater { get; set; }
    }

    // ---------- ViewModels ----------
    public class SwListaViewModel
    {
        public List<PersonaItem> Personas { get; set; } = new List<PersonaItem>();
        public string Q { get; set; }
        public int Pagina { get; set; }
        public int TotalPaginas { get; set; }
        public int Total { get; set; }
        public string Error { get; set; }
    }

    public class SwDetalleViewModel
    {
        public PersonaItem Persona { get; set; }
        public PlanetaProps Planeta { get; set; }
        public string Error { get; set; }
    }

    // ---------- Traducciones y utilidades ----------
    public static class StarWarsHelper
    {
        private static readonly Dictionary<string, string> Generos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"male","Masculino"},{"female","Femenino"},{"n/a","No aplica"},{"none","Ninguno"},{"hermaphrodite","Hermafrodita"}
        };

        public static string GeneroEs(string genero)
        {
            string v;
            return genero != null && Generos.TryGetValue(genero, out v) ? v : genero;
        }

        // "n/a" -> "na" (para clases CSS)
        public static string GeneroSlug(string genero)
        {
            if (string.IsNullOrEmpty(genero)) return "na";
            return genero.Trim().ToLowerInvariant().Replace("/", "");
        }

        // Muestra el valor con su unidad, o "—" si la API devuelve "unknown" / "n/a"
        public static string ConUnidad(string valor, string unidad)
        {
            if (string.IsNullOrWhiteSpace(valor) || valor == "unknown" || valor == "n/a") return "—";
            return string.IsNullOrEmpty(unidad) ? valor : valor + " " + unidad;
        }
    }
}