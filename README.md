# Sistema Integral de Gestión de Clientes, Servicios y Reservas - Software FJ

## Descripción del Proyecto
Este repositorio contiene el código fuente correspondiente a la Fase 4 (Componente Práctico Simulado) del curso de Programación (213023) de la Universidad Nacional Abierta y a Distancia (UNAD). 

El proyecto consiste en una aplicación desarrollada en Python para la empresa "Software FJ". El objetivo principal es gestionar las reservas de salas, alquiler de equipos y asesorías especializadas utilizando los pilares de la Programación Orientada a Objetos (POO). Toda la información se gestiona en memoria mediante objetos y listas (sin uso de bases de datos), enfocándose fuertemente en el manejo avanzado de excepciones para garantizar una aplicación estable y tolerante a fallos.

## Características Técnicas
* **Arquitectura Orientada a Objetos:** Implementación de abstracción, herencia, polimorfismo, encapsulación y métodos sobrecargados.
* **Gestión Avanzada de Errores:** Uso de excepciones personalizadas, encadenamiento de excepciones y aplicación de bloques `try/except`, `try/except/else` y `try/except/finally`.
* **Sistema de Logs:** Registro automático de errores y eventos críticos en un archivo externo (`errores_software_fj.log`) para evitar la interrupción del programa.
* **Simulación de Escenarios:** Ejecución preprogramada de 10 operaciones que ponen a prueba la resiliencia del sistema ante datos inválidos, servicios no disponibles y reservas incorrectas.

## Estructura del Código
El sistema se compone de las siguientes entidades principales:
1. `EntidadGeneral`: Clase abstracta que define el comportamiento base.
2. `Cliente`: Gestiona la información del usuario con validación estricta y encapsulamiento.
3. `Servicio`: Clase abstracta de la cual heredan `ReservaSala`, `AlquilerEquipo` y `AsesoriaEspecializada`, cada una con su propio cálculo de costos mediante polimorfismo.
4. `Reserva`: Unifica clientes y servicios, gestionando los estados (Pendiente, Confirmada, Cancelada) con validaciones de excepciones.

## Requisitos
* Python 3.8 o superior.
* No requiere la instalación de librerías externas (solo utiliza los módulos nativos `abc` y `logging`).

## Instrucciones de Ejecución
1. Clona este repositorio en tu equipo local:
   ```bash
   git clone https://github.com/sebastianvelasquezosorio1998/Fase4_Componente-Practico/tree/main
   ```
2. Navega hasta el directorio del proyecto.
3. Ejecuta el archivo principal desde la terminal:
   ```bash
   python main.py
   ```
4. Revisa la salida en la consola, donde se mostrará la simulación de las 10 operaciones.
5. Verifica la creación del archivo `errores_software_fj.log` en la misma carpeta para observar el registro de las excepciones capturadas.

## Autores
Este proyecto fue desarrollado por el estudiante:
* Sebastian Velasquez Osorio*
