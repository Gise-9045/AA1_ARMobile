Este package facilita la integración de control por voz en un proyecto, 
ya que encapsula toda la lógica necesaria para inicializar el sistema de 
reconocimiento, gestionar permisos, escuchar la voz del usuario, recibir 
resultados parciales y finales, y lanzar eventos configurables desde el 
Inspector al detectar palabras concretas. Su principal aportación es permitir
 incorporar comandos de voz de forma rápida, modular y reutilizable, reduciendo 
 el tiempo de implementación y evitando que cada proyecto tenga que desarrollar 
 desde cero toda la infraestructura de reconocimiento y gestión de eventos.

El package incluye una escena de prueba en la que, al pronunciar la palabra
 “Desactivar”, se oculta la interfaz, y al decir “Activar”, vuelve a mostrarse.
 Además, incorpora un prefab listo para arrastrar directamente a cualquier
 escena, así como el script VoiceController.cs, que puede añadirse manualmente 
 a cualquier GameObject según las necesidades del proyecto.

Desde el editor es posible configurar fácilmente los Keyword Events, indicando
 qué palabra se quiere detectar y qué eventos deben ejecutarse cuando esa
 palabra sea reconocida. De este modo, el sistema resulta flexible y sencillo 
 de adaptar a distintos contextos de juego o aplicación.

El reconocimiento funciona especialmente bien con palabras en inglés, aunque 
también suele detectar correctamente palabras simples en español. Por ello, 
está pensado como una base práctica y accesible para añadir interacción por 
voz sin complicar la estructura general del proyecto.