# Importamos la librería abc para poder crear clases y métodos abstractos
from abc import ABC, abstractmethod
# Importamos logging para registrar los errores en un archivo de logs
import logging

# Configuración del archivo de logs para registrar eventos y errores
logging.basicConfig(
    filename='errores_software_fj.log', # Nombre del archivo de logs
    level=logging.ERROR, # Solo registraremos errores o problemas graves
    format='%(asctime)s - %(levelname)s - %(message)s' # Formato con fecha, nivel y mensaje
)

# =========================================================
# 1. EXCEPCIONES PERSONALIZADAS
# =========================================================
# Clase base para excepciones del sistema
class SistemaFJError(Exception):
    pass

# Excepción para errores relacionados con los datos del cliente
class ClienteInvalidoError(SistemaFJError):
    pass

# Excepción para errores relacionados con la disponibilidad o parámetros de servicios
class ServicioInvalidoError(SistemaFJError):
    pass

# Excepción para fallos al momento de procesar reservas
class ReservaError(SistemaFJError):
    pass

# =========================================================
# 2. CLASE ABSTRACTA PARA ENTIDADES GENERALES
# =========================================================
# Clase abstracta principal que heredan las entidades del sistema
class EntidadGeneral(ABC):
    
    # Constructor base para asignarle un ID a cualquier entidad
    def __init__(self, id_entidad):
        self._id_entidad = id_entidad
        
    # Método abstracto que obligará a las hijas a mostrar su detalle
    @abstractmethod
    def obtener_detalles(self):
        pass

# =========================================================
# 3. CLASE CLIENTE (Encapsulación y Validaciones)
# =========================================================
class Cliente(EntidadGeneral):
    
    # Constructor de cliente, inicializa variables privadas
    def __init__(self, id_entidad, nombre, documento):
        super().__init__(id_entidad) # Llama al constructor padre
        self.__nombre = None # Variable encapsulada
        self.__documento = None # Variable encapsulada
        # Usamos los setters para aplicar las validaciones inmediatamente
        self.nombre = nombre 
        self.documento = documento

    # Getter para el nombre
    @property
    def nombre(self):
        return self.__nombre

    # Setter para el nombre con validación estricta
    @nombre.setter
    def nombre(self, valor):
        # Si el valor está vacío, disparamos una excepción
        if not valor or not valor.strip():
            raise ClienteInvalidoError("El nombre del cliente no puede estar vacío.")
        self.__nombre = valor

    # Getter para el documento
    @property
    def documento(self):
        return self.__documento

    # Setter para el documento con validación
    @documento.setter
    def documento(self, valor):
        if not valor or len(str(valor)) < 5:
            raise ClienteInvalidoError("El documento es inválido. Debe tener al menos 5 caracteres.")
        self.__documento = valor

    # Implementación del método abstracto de la clase padre
    def obtener_detalles(self):
        return f"Cliente: {self.nombre} (ID: {self.documento})"

# =========================================================
# 4. CLASE ABSTRACTA SERVICIO Y CLASES DERIVADAS
# =========================================================
# Clase abstracta intermedia para servicios
class Servicio(EntidadGeneral):
    
    def __init__(self, id_entidad, nombre_servicio, precio_base):
        super().__init__(id_entidad)
        self.nombre_servicio = nombre_servicio
        # Validamos que el precio no sea negativo
        if precio_base < 0:
            raise ServicioInvalidoError("El precio base no puede ser negativo.")
        self.precio_base = precio_base

    # Método abstracto para calcular costos. Usamos parámetros opcionales
    # para simular métodos sobrecargados (requerimiento del anexo)
    @abstractmethod
    def calcular_costo(self, impuestos=True, descuento=0):
        pass

# 4.1 Servicio derivado: Reserva de Salas
class ReservaSala(Servicio):
    
    # Implementación de costo sobreescrito simulando sobrecarga
    def calcular_costo(self, impuestos=True, descuento=0):
        try:
            # Validamos que el descuento sea lógico
            if descuento < 0 or descuento > 100:
                raise ValueError("Descuento fuera de rango (0-100).")
            # Calculamos costo con descuento
            costo = self.precio_base * (1 - descuento / 100)
            # Agregamos 19% de IVA si aplica
            if impuestos:
                costo *= 1.19
            return costo
        except Exception as e:
            # Encadenamiento de excepciones si falla el cálculo
            raise ServicioInvalidoError(f"Error al calcular costo de sala") from e

    def obtener_detalles(self):
        return f"Servicio: Reserva de Sala - {self.nombre_servicio}"

# 4.2 Servicio derivado: Alquiler de Equipos
class AlquilerEquipo(Servicio):
    
    # Implementación con diferente lógica de costo
    def calcular_costo(self, impuestos=True, descuento=0, seguro_adicional=False):
        costo = self.precio_base * (1 - descuento / 100)
        # Este servicio específico tiene la variante de cobro de seguro
        if seguro_adicional:
            costo += 50000 
        if impuestos:
            costo *= 1.19
        return costo

    def obtener_detalles(self):
        return f"Servicio: Alquiler de Equipo - {self.nombre_servicio}"

# 4.3 Servicio derivado: Asesoría Especializada
class AsesoriaEspecializada(Servicio):
    
    def calcular_costo(self, impuestos=False, descuento=0):
        # Las asesorías, por defecto, podrían no tener impuestos aplicables en este escenario
        costo = self.precio_base * (1 - descuento / 100)
        if impuestos:
            costo *= 1.19
        return costo

    def obtener_detalles(self):
        return f"Servicio: Asesoría - {self.nombre_servicio}"

# =========================================================
# 5. CLASE RESERVA (Manejo de estados y try/except avanzado)
# =========================================================
class Reserva:
    
    def __init__(self, cliente, servicio, duracion_horas):
        # Validaciones de la creación de la reserva
        if not isinstance(cliente, Cliente):
            raise ReservaError("El objeto proporcionado no es un cliente válido.")
        if not isinstance(servicio, Servicio):
            raise ReservaError("El objeto proporcionado no es un servicio válido.")
            
        self.cliente = cliente
        self.servicio = servicio
        self.duracion_horas = duracion_horas
        self.estado = "Pendiente" # Estados: Pendiente, Confirmada, Cancelada

    def confirmar_reserva(self):
        # Uso de try/except/else/finally requerido por la rúbrica
        try:
            # Simulación de un proceso que podría fallar
            if self.duracion_horas <= 0:
                raise ValueError("La duración debe ser mayor a 0 horas.")
            if self.estado == "Cancelada":
                raise ReservaError("No se puede confirmar una reserva cancelada.")
                
            # Simulamos cálculo de costo al confirmar
            costo = self.servicio.calcular_costo()
            
        except ValueError as e:
            # Manejo de error de valor específico
            logging.error(f"Error de valor en reserva: {e}")
            raise ReservaError("Fallo por parámetros inconsistentes en la duración.") from e
        except ReservaError as e:
            # Manejo de error personalizado
            logging.error(f"Error de lógica de reserva: {e}")
            raise
        except Exception as e:
            # Captura de cualquier otra anomalía
            logging.error(f"Error inesperado al confirmar: {e}")
            raise SistemaFJError("Error fatal del sistema") from e
        else:
            # Se ejecuta solo si el try fue exitoso sin lanzar excepciones
            self.estado = "Confirmada"
            print(f"ÉXITO: Reserva confirmada para {self.cliente.nombre}. Costo final: ${costo:.2f}")
        finally:
            # Se ejecuta SIEMPRE, haya o no haya errores
            print(f"[FINALLY] Finaliza el proceso de confirmación de reserva. Estado actual: {self.estado}")

    def cancelar_reserva(self):
        if self.estado == "Confirmada":
            self.estado = "Cancelada"
            print("La reserva ha sido cancelada exitosamente.")
        else:
            raise ReservaError("Solo se pueden cancelar reservas previamente confirmadas.")

# =========================================================
# 6. SIMULACIÓN DE 10 OPERACIONES (MAIN)
# =========================================================
if __name__ == "__main__":
    print("--- INICIO SIMULACIÓN SOFTWARE FJ ---\n")
    
    # Usamos listas internas para manejar la información sin base de datos
    lista_clientes = []
    lista_reservas = []

    # Operación 1: Registro válido de cliente
    try:
        print("Op 1: Creando cliente válido...")
        cliente1 = Cliente(1, "Sebastian Velasquez", "10123456")
        lista_clientes.append(cliente1)
        print(f"Cliente creado: {cliente1.nombre}")
    except Exception as e:
        print(f"Error Op 1: {e}")

    # Operación 2: Registro inválido de cliente (fallo capturado)
    try:
        print("\nOp 2: Creando cliente inválido (sin nombre)...")
        cliente_error = Cliente(2, "", "98765")
    except ClienteInvalidoError as e:
        logging.error(f"Intento de registro inválido: {e}")
        print(f"Fallo esperado Op 2: {e}")

    # Operación 3: Creación correcta de servicio
    try:
        print("\nOp 3: Creando servicio válido...")
        servicio1 = ReservaSala(101, "Sala VIP", 100000)
        print(f"Servicio creado: {servicio1.nombre_servicio}")
    except Exception as e:
        print(f"Error Op 3: {e}")

    # Operación 4: Creación incorrecta de servicio (precio negativo)
    try:
        print("\nOp 4: Creando servicio inválido (precio negativo)...")
        servicio_error = AlquilerEquipo(102, "Proyector", -50000)
    except ServicioInvalidoError as e:
        logging.error(f"Servicio inválido creado: {e}")
        print(f"Fallo esperado Op 4: {e}")

    # Operación 5: Creación de reserva exitosa
    try:
        print("\nOp 5: Creando reserva válida...")
        reserva1 = Reserva(cliente1, servicio1, 4)
        lista_reservas.append(reserva1)
        print(f"Reserva creada con estado: {reserva1.estado}")
    except Exception as e:
        print(f"Error Op 5: {e}")

    # Operación 6: Confirmación exitosa de reserva (Uso de try/except/else/finally)
    try:
        print("\nOp 6: Confirmando reserva...")
        reserva1.confirmar_reserva()
    except Exception as e:
        print(f"Error Op 6: {e}")

    # Operación 7: Cancelación exitosa de reserva
    try:
        print("\nOp 7: Cancelando reserva confirmada...")
        reserva1.cancelar_reserva()
    except Exception as e:
        print(f"Error Op 7: {e}")

    # Operación 8: Intento de confirmar reserva ya cancelada (Provoca encadenamiento/error)
    try:
        print("\nOp 8: Confirmando reserva cancelada...")
        reserva1.confirmar_reserva()
    except ReservaError as e:
        print(f"Fallo esperado Op 8: {e}")

    # Operación 9: Método sobrecargado (Cálculo con descuento y sin impuestos)
    try:
        print("\nOp 9: Probando método sobrecargado (Descuento del 10%, sin impuestos)...")
        # Hacemos uso del polimorfismo y parámetros opcionales
        asesoria = AsesoriaEspecializada(103, "Asesoría IT", 200000)
        costo_variante = asesoria.calcular_costo(impuestos=False, descuento=10)
        print(f"Costo sobrecargado calculado: ${costo_variante:.2f}")
    except Exception as e:
        print(f"Error Op 9: {e}")

    # Operación 10: Reserva inválida con parámetros faltantes/erróneos
    try:
        print("\nOp 10: Creando reserva con duración inválida (0 horas)...")
        reserva_fallida = Reserva(cliente1, servicio1, 0)
        reserva_fallida.confirmar_reserva() # Esto detonará el ValueError transformado en ReservaError
    except ReservaError as e:
        # Se demuestra el encadenamiento de excepciones (from e)
        print(f"Fallo esperado Op 10: {e}. Causa original: {e.__cause__}")