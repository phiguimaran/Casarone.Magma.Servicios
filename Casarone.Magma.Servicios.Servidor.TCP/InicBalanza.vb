Option Explicit On

Imports Magma.Tools.Extensiones.Common.Servicios.Rutinas
Imports Magma.Tools.Extensiones.Common.Servicios.Entities
Imports Magma.Tools.Extensiones.Common.Servicios.ResolverEntrada
Imports Magma.Tools.Core.Business.Dataservices
Imports Magma.Tools.Core.Common.DataServices
Imports Magma.Tools.Extensiones.Common.Servicios.Interfaces
Imports Magma.Tools.API.Common
Imports Magma.Tools.Core.Business.DataServices.SQL

Public Class InicBalanza
    Implements IServiciosServidor
    Private _entradaresuelta As String
    Private _transaccion As System.Data.IDbTransaction

    '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    'En caso de que el servicio sea invocado ó pueda invocarse durante una Transaction de un FormularioMagma
    ', y el servicio haga consultas ó ejecuciones en BD, 'se utiliza este método, 
    Public Overloads Function Ejecutar(ByVal Entrada As Object, ByVal Transaccion As System.Data.IDbTransaction) As Object Implements IServiciosServidor.Ejecutar
        _transaccion = Transaccion
        Return Ejecutar(Entrada)
    End Function


    ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
    'Rutina de ejecución del servicio
    Public Overloads Function Ejecutar(ByVal Entrada As Object) As Object Implements IServiciosServidor.Ejecutar
        Dim retorno As Integer = 0
        Dim tipobalanza As Integer
        Dim URLbalanza As String
        Dim puertobalanza As Integer = 0
        Dim salida(0, 0) As Object
        Dim codbalanza As Integer
        'rutina que traduce entrada de servicio, de estructurado, a string
        _entradaresuelta = ResolverEntrada(Entrada, TipoFormato.gkintFormatoSQL)

        Dim PrimerParametro As String = _entradaresuelta.Split("|")(0) ' tomo el primer parámetro (solo se espera uno, pero por las dudas....)
        ' por compatibilidad con link (servicio WWWBalanza), si la entrada es una url, 
        ' tengo que buscar el código de balanza que tiene asignado esta url
        ' esto sería para unificar el servicio con el WWWBalanza del link viejo
        ' de esa forma si se llama desde tools ejecuta esto y si se llama desde link ejecuta la vieja ventanita de lectura del peso

        If PrimerParametro.Contains(".") Then  'si es una url deberúa tener al menos un punto
            codbalanza = SQL.SelScalar("select first 1 nvl(cod_balanza,0) from ct_balanzas where urlbalanza = " & PrimerParametro)
            If codbalanza = 0 Then
                Throw New Exception("No se pudo obtener el codigo de la balanza mediante la URL")
            End If
        Else
            codbalanza = Val(PrimerParametro)
        End If
        ' fin por compatibilidad

        tipobalanza = SQL.SelScalar("select nvl(tipo_balanza,-1) from ct_balanzas where cod_balanza = " & codbalanza)

        If tipobalanza = -1 Then ' si no existe la balanza Error
            Throw New Exception("No se encuentra la balanza ingresada")
        ElseIf tipobalanza = 1 Then
            URLbalanza = SQL.SelScalar("select URLBalanza from ct_balanzas where cod_balanza = " & codbalanza)
            retorno = leerPesoWebServer(URLbalanza)
        ElseIf tipobalanza = 2 Then
            URLbalanza = SQL.SelScalar("select URLBalanzaDirect from ct_balanzas where cod_balanza = " & codbalanza)
            puertobalanza = SQL.SelScalar("select puerto_TCP from ct_balanzas where cod_balanza = " & codbalanza)
            retorno = leerPesoBalanza(URLbalanza, puertobalanza)
        Else
            Throw New Exception("Tipo de balanza debe ser 1 o 2")
        End If

        'Lo que retorna el servicio tiene que ser una matriz de (n,n), en caso que retorne datos
        salida(0, 0) = retorno
        Return salida
    End Function


    'Esta es la traducción a string, de lo que el servicio ejecutó, a los efectos de 'monitorear lo ejecutado 
    'todo se graba un un archivo que se levanta desde el cliutis
    Public Function EntradaResuelta() As String Implements IServiciosServidor.EntradaResuelta
        Return _entradaresuelta
    End Function

    Public Function leerPesoBalanza(URLbalanza As String, puerto As Integer) As Integer
        Dim readstring As String = ""
        Dim retstring As String = ""
        Dim client As System.Net.Sockets.TcpClient = Nothing
        Dim stream As System.Net.Sockets.NetworkStream = Nothing
        Dim buffer(1500) As Byte
        Dim inicio As Integer
        Dim j As Integer
        ' estos 3 valores podrían ser definidos en la tabla ct_balanzas para mayor flexibilidad
        'TcpLargoBuffer = 1500
        'TcpInicioDato = 12
        'TcpLargoDato = 7
        Try
            client = New System.Net.Sockets.TcpClient()
            client.Connect(URLbalanza, puerto)
            stream = client.GetStream()
            stream.ReadTimeout = 200000
            stream = client.GetStream()
            stream.Read(buffer, 0, 1500)
            readstring = System.Text.Encoding.ASCII.GetString(buffer)
            inicio = readstring.LastIndexOf(ChrW(13)) - 12
            retstring = readstring.Substring(inicio, 7)
            If IsNumeric(retstring) Then
                j = Val(retstring)
            Else
                j = 0
            End If
        Catch e As Exception
            j = -1
        Finally
            stream.Close()
            client.Close()
        End Try
        Return j
    End Function

    Public Function leerPesoWebServer(URLbalanza As String) As Integer
        Dim inStream As System.IO.StreamReader = Nothing
        Dim webRequest1 As System.Net.WebRequest
        Dim webresponse1 As System.Net.WebResponse
        Dim url As String = "http://" & URLbalanza.Trim & "/cgi-bin/balanza.exe"
        Dim ret As Integer
        Try
            '23/06/10 devolver siempre cero en caso de fallo ó de que el contenido sea cualquier string no numérico
            webRequest1 = System.Net.WebRequest.Create(url)
            webresponse1 = webRequest1.GetResponse()
            inStream = New System.IO.StreamReader(webresponse1.GetResponseStream())
            Dim sal As Object = inStream.ReadToEnd()
            If Not sal Is Nothing Then
                Dim auxsal As String = sal.Trim.ToLower
                'veo si es el formato casarone
                If auxsal.Length < 50 AndAlso auxsal.StartsWith("<html>") AndAlso auxsal.EndsWith("</html>") Then
                    auxsal = auxsal.Replace(" ", "")
                    auxsal = auxsal.Replace("<html>", "")
                    auxsal = auxsal.Replace("</html>", "")
                    auxsal = auxsal.Replace("<body>", "")
                    auxsal = auxsal.Replace("</body>", "")
                    auxsal = auxsal.Replace(Environment.NewLine, "")
                    If IsNumeric(auxsal) Then
                        ret = Val(auxsal)
                    Else
                        Throw New Exception("El dato leido no se pudo interpretar como un peso válido." & vbCrLf & "Intentelo nuevamente.")
                    End If
                Else
                    Throw New Exception("El dato leido no se pudo interpretar." & vbCrLf & "Intentelo nuevamente.")
                End If
            Else
                Throw New Exception("No se pudo leer desde la balanza indicada." & vbCrLf & "Intentelo nuevamente.")
            End If
            Return ret
        Catch ex As Exception
            Throw ex
        Finally
            inStream.Close()
        End Try
    End Function

End Class

'Public Function leerPesoBalanza(URLbalanza As String, puerto As Integer) As Integer
'    Dim client As System.Net.Sockets.TcpClient = Nothing
'    Dim stream As System.Net.Sockets.NetworkStream = Nothing
'    Dim aux As String = ""
'    Dim j As Integer
'    Dim buffer(17) As Byte
'    Try
'        client = New System.Net.Sockets.TcpClient()
'        client.Connect(URLbalanza, puerto)
'        stream = client.GetStream()
'        stream.ReadTimeout = 200000
'        stream = client.GetStream()
'        stream.Read(buffer, 0, 17)
'        client.Close()
'        aux = System.Text.Encoding.ASCII.GetString(buffer).Substring(3, 7)
'        If IsNumeric(aux) Then
'            j = Val(aux)
'        Else
'            j = 0
'        End If
'    Catch e As Exception
'        j = -1
'    Finally
'        stream.Close()
'        client.Close()
'    End Try
'    Return j
'End Function

'Public Function leerPesoBalanza2(URLbalanza As String, puerto As Integer) As Integer
'    Dim client As System.Net.Sockets.TcpClient = Nothing
'    Dim stream As System.Net.Sockets.NetworkStream = Nothing
'    Dim aux As String = ""
'    Dim j As Integer
'    Dim i As Integer
'    Dim available As Integer
'    Try
'        client = New System.Net.Sockets.TcpClient()
'        client.Connect(URLbalanza, puerto)
'        While client.Available <= 50
'        End While
'        available = client.Available

'        Dim buffer(available) As Byte
'        stream = client.GetStream()
'        stream.Read(buffer, 0, available)
'        i = available - 50
'        Do While buffer(i) <> 13
'            i = i + 1
'        Loop
'        aux = System.Text.Encoding.ASCII.GetString(buffer).Substring(i + 4, 10)
'        If IsNumeric(aux) Then
'            j = Val(aux)
'        Else
'            j = 0
'        End If
'    Catch e As Exception
'        j = -1
'    Finally
'        stream.Close()
'        client.Close()
'    End Try
'    Return j
'End Function
