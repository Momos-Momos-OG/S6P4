using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using S6P4.Models;

namespace S6P4.Controllers
{
    public class StarWarsController : Controller
    {
        private const int PorPagina = 10;

        // HttpClient único y reutilizable (buena práctica: no crear uno por petición)
        private static readonly HttpClient _client = CrearCliente();

        private static HttpClient CrearCliente()
        {
            // La API usa HTTPS: .NET Framework necesita TLS 1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var cliente = new HttpClient
            {
                BaseAddress = new Uri("https://www.swapi.tech/api/"),
                Timeout = TimeSpan.FromSeconds(30)
            };

            cliente.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd("S6P4/1.0");

            // SWAPI es pública y NO requiere token.
            // Con otra API que sí lo pida, aquí agregarías:
            // cliente.DefaultRequestHeaders.Authorization =
            //     new AuthenticationHeaderValue("Bearer", "TU_TOKEN_AQUI");

            return cliente;
        }

        // GET: /StarWars?q=luke&pagina=1
        public async Task<ActionResult> Index(string q = null, int pagina = 1)
        {
            if (pagina < 1) pagina = 1;
            var vm = new SwListaViewModel { Q = q, Pagina = pagina, TotalPaginas = 1 };

            try
            {
                if (!string.IsNullOrWhiteSpace(q))
                {
                    // Búsqueda por nombre: devuelve directamente las propiedades (sin paginación)
                    var resp = await _client.GetAsync("people/?name=" + Uri.EscapeDataString(q.Trim()));

                    if (resp.IsSuccessStatusCode)
                    {
                        string json = await resp.Content.ReadAsStringAsync();
                        var busqueda = JsonConvert.DeserializeObject<SwBusquedaResponse>(json);
                        vm.Personas = busqueda.Result ?? new List<PersonaItem>();
                        vm.Total = vm.Personas.Count;
                    }
                    // 404 o sin coincidencias: se muestra la lista vacía
                    return View(vm);
                }

                // 1) Lista paginada: solo trae uid, name y url
                string jsonLista = await _client.GetStringAsync("people/?page=" + pagina + "&limit=" + PorPagina);
                var lista = JsonConvert.DeserializeObject<SwListaResponse>(jsonLista);

                vm.Total = lista.TotalRecords;
                vm.TotalPaginas = lista.TotalPages > 0 ? lista.TotalPages : 1;

                // 2) Detalle de cada personaje en paralelo (para género, año de nacimiento, etc.)
                var tareas = (lista.Results ?? new List<SwRef>()).Select(r => ObtenerPersonaAsync(r.Uid));
                var detalles = await Task.WhenAll(tareas);
                vm.Personas = detalles.Where(d => d != null).ToList();
            }
            catch (HttpRequestException ex)
            {
                vm.Error = "No se pudo conectar con SWAPI: " + ex.Message;
            }
            catch (TaskCanceledException)
            {
                vm.Error = "La solicitud tardó demasiado (timeout). Intenta de nuevo.";
            }
            catch (JsonException ex)
            {
                vm.Error = "Error al interpretar el JSON recibido: " + ex.Message;
            }

            return View(vm);
        }

        // GET: /StarWars/Detalle/1
        public async Task<ActionResult> Detalle(string id)
        {
            var vm = new SwDetalleViewModel();

            try
            {
                vm.Persona = await ObtenerPersonaAsync(id);

                if (vm.Persona == null)
                {
                    vm.Error = "No se encontró ningún personaje con el número \"" + id + "\".";
                    return View(vm);
                }

                // El planeta de origen viene como URL absoluta: se consulta con una segunda llamada
                vm.Planeta = await ObtenerPlanetaAsync(vm.Persona.Properties.Homeworld);
            }
            catch (Exception ex)
            {
                vm.Error = "Ocurrió un error al consultar la API: " + ex.Message;
            }

            return View(vm);
        }

        // -------- Métodos privados --------

        private static async Task<PersonaItem> ObtenerPersonaAsync(string uid)
        {
            var resp = await _client.GetAsync("people/" + Uri.EscapeDataString(uid ?? ""));
            if (!resp.IsSuccessStatusCode) return null;

            string json = await resp.Content.ReadAsStringAsync();
            var detalle = JsonConvert.DeserializeObject<SwDetalleResponse>(json);
            return detalle != null && detalle.Result != null ? detalle.Result : null;
        }

        private static async Task<PlanetaProps> ObtenerPlanetaAsync(string urlPlaneta)
        {
            if (string.IsNullOrEmpty(urlPlaneta)) return null;

            try
            {
                string json = await _client.GetStringAsync(urlPlaneta);
                var planeta = JsonConvert.DeserializeObject<SwPlanetaResponse>(json);
                return planeta != null && planeta.Result != null ? planeta.Result.Properties : null;
            }
            catch
            {
                return null; // el planeta es opcional
            }
        }
    }
}