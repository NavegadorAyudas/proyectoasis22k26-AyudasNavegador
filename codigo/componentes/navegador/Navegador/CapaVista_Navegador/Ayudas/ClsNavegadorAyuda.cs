using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
// Inicio cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026
using CapaControlador_Navegador;
// Fin cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026

//-----------------------------------------------------
// - Hecho por: Natali Sofía Montenegro Portillo
// - Carne: 0901-23-10017
namespace CapaVista_Navegador
{
    // Inicio cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026
    // Ayuda dinámica: el botón Ayuda ya no abre siempre la misma ayuda. Abre la página de la aplicación
    // que tiene configurada el Navegador (su IdAplicacion), dentro del .chm de su módulo. La Vista solo
    // le pregunta al Controlador qué mostrar (ClsCtrlAyuda) y lo muestra; no hay rutas que registrar.
    // Fin cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026
    public static class ClsNavegadorAyuda
    {
        private const string _Recurso = "Navegador.chm";

        // Inicio cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026
        public static void NavegadorMetMostrar(Navegador Componente)
        {
            try
            {
                // El nombre del formulario ayuda a encontrar la página si no coincide con la aplicación.
                Form Formulario = Componente.FindForm();
                string NombreFormulario = Formulario == null ? "" : Formulario.GetType().Name;

                ClsCtrlAyuda Controlador = new ClsCtrlAyuda();
                ClsDestinoAyuda Destino = Controlador.NavegadorFuncObtenerDestino(Componente.NavegadorIdAplicacion, NombreFormulario);

                if (Destino.RutaArchivo == "")
                {
                    MessageBox.Show(
                        Destino.Mensaje,
                        "Ayuda",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                if (Destino.Pagina == "")
                {
                    Help.ShowHelp(Componente, Destino.RutaArchivo);
                }
                else
                {
                    Help.ShowHelp(Componente, Destino.RutaArchivo, HelpNavigator.Topic, Destino.Pagina);
                }
            }
            catch (Exception Excepcion)
            {
                MessageBox.Show(
                    "No se pudo abrir la ayuda de esta aplicación.\n\n" + Excepcion.Message,
                    "Ayuda",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
        // Fin cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026

        // Inicio cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026
        // Obsoleto: antes la ayuda era una sola, el archivo Navegador.chm viajaba DENTRO de
        // CapaVista_Navegador (recurso incrustado) y se extraía a una carpeta temporal. Ahora la ayuda
        // la resuelve ClsCtrlAyuda según la aplicación. Se deja sin borrar, junto con el recurso
        // incrustado del .csproj, para coordinar con Natali antes de eliminarlo.
        // Fin cambio - Evelyn Sofía Andrade Luna - 9959-23-1224 - 26/10/2026
        // Devuelve la ruta del .chm extraído del recurso incrustado .
        private static string NavegadorFuncExtraer()
        {
            string Carpeta = Path.Combine(Path.GetTempPath(), "Navegador");
            string Ruta = Path.Combine(Carpeta, _Recurso);

            using (Stream Origen = Assembly.GetExecutingAssembly().GetManifestResourceStream(_Recurso))
            {
                if (Origen == null)
                {
                    throw new FileNotFoundException("La ayuda no está incluida en el componente Navegador.");
                }

                if (!File.Exists(Ruta) || new FileInfo(Ruta).Length != Origen.Length)
                {
                    Directory.CreateDirectory(Carpeta);

                    using (FileStream Destino = File.Create(Ruta))
                    {
                        Origen.CopyTo(Destino);
                    }
                }
            }

            return Ruta;
        }
    }
}
//--------------------------------------------------
// - Final: Natali Sofía Montenegro Portillo
// - Carne: 0901-23-10017
