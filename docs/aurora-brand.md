# Aurora Hotel — identidad visual

Propuesta para la demo, basada en el nombre Hotel Demo Aurora del seed. No cambia
el nombre del proyecto, los datos persistidos ni los contratos de la API.

Aurora es una marca hotelera sobria y cálida. Firma: «Una estancia. Mil detalles.»
El monograma A se construye con líneas arquitectónicas rectas. La marca se usa
en la navegación, favicon, acceso, cabecera, títulos de pestañas y pie de página.

| Elemento | Especificación |
| --- | --- |
| Azul noche | `#15283F`, marca y acciones principales |
| Marfil | `#F5F3EF`, fondo general |
| Latón | `#AD9066`, acentos discretos |
| Blanco | `#FFFFFF`, superficies de trabajo |
| Tipografía editorial | Palatino / Georgia, marca y titulares |
| Tipografía funcional | Segoe UI / system-ui, navegación y formularios |
| Esquinas | 3–4 px; controles y superficies con bordes discretos |
| Movimiento | Entradas breves y hover contenido; se respeta reduced-motion |

La voz usa mensajes concretos relacionados con la operación. No se muestran
cifras de ocupación ni estados de conexión sin obtenerlos del servidor.

## Recursos

- `src/HotelManagement.Api/wwwroot/images/aurora-mark.svg`: monograma vectorial.
- `src/HotelManagement.Api/wwwroot/images/aurora-lobby.png`: imagen conceptual
  generada con la herramienta integrada image_gen. No es una fotografía de un
  establecimiento real. Se reutiliza en portada y login; no depende de terceros.
- Colores, tipografía y componentes: `wwwroot/css/styles.css`.

Prompt utilizado, modo integrado (sin CLI):

> Use case: photorealistic-natural. Asset type: editorial architectural photograph for the Aurora boutique hotel brand, used as a website hero and login image. Primary request: a sophisticated intimate hotel lobby in coastal Lima, Peru, calm late afternoon natural light, dark navy blue upholstered bench, warm ivory limewash walls, walnut joinery, subtle brushed brass details, one carefully composed ceramic vessel, rectangular architectural opening and travertine floor. Style: believable high-end architectural magazine photography, tactile imperfect natural surfaces, restrained human interior design, no CGI aesthetic, no excessive luxury cliches. Composition: landscape 3:2, one coherent uncluttered room, seating visible in the right center, generous space, clean rectangular lines, no arches. Warm daylight and quiet deep shadows. Color palette navy #15283F, ivory #F4F1EB, warm wood and muted brass. No text, no logos, no people, no watermarks.
